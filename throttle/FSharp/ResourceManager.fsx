#load "ResourceRequest.fsx"
open Module

type ResourceMessage =
  | Wait of ResourceRequest
  | Run of ResourceRequest
  | ReleaseResource of ResourceRequest

type ResourceManager(numberOfActiveResources) =
  let removeReleasedResource (resourceMessages:ResourceMessage[]) (message:ResourceMessage) =
    let ret = 
      let resourceRequest =
        match message with
        | ReleaseResource(m) -> m
        | _ -> failwith "Why would you call removeReleasedResource with no ResourceMessage of type ReleaseResource"
      resourceMessages
      |> Array.filter (
        fun messageItem -> 
          match messageItem with
          | Run(rr) -> rr <> resourceRequest
          | _ -> true
      )
    ret

  let getLastWaitingMessage resourceMessages =
    (Array.concat [
      (resourceMessages
        |> Array.map 
          (fun msg -> match msg with
                      | Wait r -> Some(msg)
                      | _ -> None)
        ); 
        [|None|] 
    ])
    |> Array.reduce 
      (fun lastWait item -> 
        match item with
        | Some msg -> Some(msg)
        | None -> lastWait)

  let splitRequests (resourceMessages:ResourceMessage[]) (releaseMessage:ResourceMessage option)= 
    let continueAndNewList : ResourceMessage [] * ResourceMessage option = 
      match resourceMessages.Length with
      | 0 -> (resourceMessages, None)
      | _ ->
            //get the last Wait type message, and remove that one from the list
            let toSend = getLastWaitingMessage resourceMessages
            (
              (**
              //@todo
                When sending:
                  Without Release messge:
                    * (done) get the last Waiting message and send the message on this object
                      then change the Waiting message type to Running message type
                  With Release mesage:
                    Same as without release 
                    * (done) remove the release message from the list
                  *)
              (
                match releaseMessage with
                | None -> resourceMessages
                | Some msg -> 
                  match msg with
                  | ReleaseResource(resourceRequest) -> (removeReleasedResource resourceMessages msg)
                  | _ -> failwith "Should not come here, releaseMessage is not none but also not ReleaseResourceMessage"
              )
              ,toSend
            )
    continueAndNewList
  let sendMessage (continueAndNewList:ResourceMessage [] * ResourceMessage option) = 
    match (snd continueAndNewList) with
    | Some resourceRequest -> 
        let msg = match resourceRequest with
                  | Wait m -> m
                  | _ -> failwith "A continue can only be sent to a waiting message."
        do msg.Continue()
        //map the list here to update the Wait of resourceRequest to a Run of resourceRequest
        let updatetedList = (
          (fst continueAndNewList)
            |> Array.map (
              fun messageItem ->
                match messageItem with
                | Wait(m) ->
                  if m = msg then
                    Run(m)
                  else
                    messageItem
                | _ -> messageItem
            )
        )
        (updatetedList, (snd continueAndNewList))

    | None -> continueAndNewList
    
    
  let message =
    MailboxProcessor.Start(fun inbox ->
      let rec loop (resourceMessages:ResourceMessage[]) =
        async { let! msg = inbox.Receive()
          match msg with
          | Wait(resourceMessage) ->
              let r = match (resourceMessages.Length) with
                      | x when x < numberOfActiveResources ->
                        do (resourceMessages, Some(Wait(resourceMessage))) |> sendMessage |> ignore
                        Run(resourceMessage)
                      | x when x >= numberOfActiveResources ->
                        Wait(resourceMessage)
                      | _ -> failwith "This is just dumb, how can x be anything else but smaller or biggerEqual"
              return! loop(Array.append [|r|] resourceMessages)
          | Run(resourceMessage) -> raise (System.ArgumentException("Cannot send a message with a running task."))
          | ReleaseResource(resourceRequest) ->
            let continueAndNewList = 
              ((splitRequests resourceMessages) (Some (ReleaseResource resourceRequest))) |> sendMessage
            return! loop(fst continueAndNewList) }
      loop [||])
  member this.RequestResource = message.Post
  member this.ReleaseResource = message.Post

let createThrottle activeTasks =
  let resourceManager = ResourceManager(activeTasks)
  fun functionToExecute ->
    fun value ->
      async{
        let resourceRequest = ResourceRequest()
        resourceManager.RequestResource(Wait(resourceRequest))
        do! resourceRequest.Wait()
        printfn "manager says it is fine to continue with value %d" value
        let! calculatedReturnValue = (functionToExecute value)
        resourceManager.ReleaseResource(ReleaseResource(resourceRequest))
        return calculatedReturnValue
      }

(** test to see how this works *)
printfn ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>"
let testFn arg = 
  printfn "In testFn, val is:%d" arg
  arg

let asAsync fn arg = 
  async{
    printfn "Async method starting with arg %d" arg
    let result = (fn arg)
    do! Async.Sleep ((16-arg)*20)
    printfn "<<<<<< Async method returning with arg %d" arg
    return result
  }
let throttledFn :((int -> Async<int>) -> int -> Async<int>) = (createThrottle 2)
// let sometest = throttleOne

let result = [1 .. 15]
            |> List.map (throttledFn (asAsync testFn))
            |> Async.Parallel 
            |> Async.RunSynchronously
printfn "DONE"
printfn ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>" 