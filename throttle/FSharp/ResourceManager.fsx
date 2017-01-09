#load "ResourceRequest.fsx"
open Module

type ResourceMessage =
  | Wait of ResourceRequest
  | Running of ResourceRequest
  | ReleaseResource of ResourceRequest

type ResourceManager(numberOfActiveResources) =

  let splitRequests (resourceMessages:ResourceMessage[]) = 
    let continueAndNewList : ResourceMessage [] * ResourceMessage option = 
      match resourceMessages.Length with
      | 0 -> (resourceMessages, None)
      | _ ->
            let noReleased = resourceMessages
                                    |> Array.filter 
                                        (fun req -> match req with
                                                    | ReleaseResource(r) -> false
                                                    | _ -> true)
            //get the last Wait type message, and remove that one from the list
            let toSend = (Array.concat [
                            (resourceMessages
                              |> Array.map 
                              (fun msg -> match msg with
                                          | Wait r -> Some(Wait(r))
                                          | _ -> None)
                            ); 
                            [|None|] 
                          ])
                          |> Array.reduce 
                            (fun lastWait item -> 
                              match item with
                              | Some msg -> Some(msg)
                              | None -> lastWait)
            (
              (**
                first in tuple Removed any ReleaseResource ResourceMessage from resourceRequests
                  and the first Wait ResourceMessage
                second in tuple first Wait ResourceMessage
              *)
              (
                match toSend with
                              | None -> noReleased
                              | Some msg -> (noReleased |> Array.filter (fun m -> m <> msg))
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
        continueAndNewList
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
                        Running(resourceMessage)
                      | x when x >= numberOfActiveResources ->
                        Wait(resourceMessage)
                      | _ -> failwith "This is just dumb, how can x be anything else but smaller or biggerEqual"
              return! loop(Array.append [|r|] resourceMessages)
          | Running(resourceMessage) -> raise (System.ArgumentException("Cannot send a message with a running task."))
          | ReleaseResource(resourceRequest) -> //@todo: remove the message that has been released, not just the last one!!!!
            let continueAndNewList = 
              (splitRequests resourceMessages) |> sendMessage
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
    printfn "Async method returning with arg %d" arg
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