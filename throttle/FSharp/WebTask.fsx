module WebTask
#load "common.fsx"
#r "HtmlAgilityPack.dll"
//https://htmlagilitypack.codeplex.com/
//can try that for parsing DOM, can't beleive how broken .net is for not having
//  such a basic thing in it's core and having to use 3rd party shitty documented
//  nusheit packages
open Common
open System
open HtmlAgilityPack
type WebTaskError = {errorDescription:string;url:string}
type WebTask = {
  path:List<string>
  text:string
  binaryContent:byte[]
  fileName:string
  url:string
  urlObject:Uri
  images:List<TryValue<WebTask,WebTaskError>>
  htmlDoc:HtmlDocument  }

let stringToTryUri url =
  tryRun
    (fun msg u -> {errorDescription=msg;url=url})
    url
    (fun url -> Uri(url))

let elementToTryUri attribute (webTask:WebTask) (el:HtmlNode) =
  tryRun
    (fun msg u -> {errorDescription=msg;url="Unknown"})
    el
    (fun el -> 
      match el.GetAttributeValue(attribute,null) with
        | null  ->  
            failwith (sprintf "Element does not have %s attribute" attribute)
        | url   ->
            Uri(webTask.urlObject,url))

let tryWebTaskFromTryUri uri =
  match uri with
  | Value (uri:Uri) ->
      Value {
              path=[]
              text=""
              binaryContent=[||]
              fileName=""
              url=uri.AbsoluteUri
              urlObject=uri
              images=[]
              htmlDoc=null}
  | Error x -> Error x

let webTaskToListOfHtmlNodes xpath wt =
  let nodes = (wt.htmlDoc.DocumentNode.SelectNodes(xpath))
  match nodes with
  | null -> 
    []
  | nodes -> 
    [for n in nodes -> n]

let processElements xpath propName processor webTask =
  webTaskToListOfHtmlNodes xpath webTask
    |> List.map 
          (elementToTryUri propName webTask
            >> processor)
            
let setImages processor webTask =
  { webTask 
      with 
      images =
        processElements "//img" "src" processor webTask}

let setPath basePath webTask =
  let splitPath =
    webTask.urlObject
        .LocalPath
        .Split [|'/'|]
    |> Array.toList
    |> List.filter (fun item -> (item <> ""))
    |> List.rev
  let u = 
    match splitPath with
    | [] -> []
    | _  -> splitPath.Tail
  {webTask with path=List.concat[basePath ; (u |> List.rev)]}


let setDocObject (webTask:WebTask) =
  let htmlDoc = new HtmlDocument()
  htmlDoc.LoadHtml(webTask.text)
  { webTask with htmlDoc=htmlDoc }

let setHtml (webTask:WebTask) =
  { webTask with 
            text = (new System.Net.WebClient())
                  .DownloadString(webTask.urlObject) }
let setBinaryContent (webTask:WebTask) =
  { webTask with
            binaryContent = (new System.Net.WebClient())
              .DownloadData(webTask.urlObject)}

let setFileName webTask =
  let newFileName = 
    (webTask.urlObject
      .LocalPath
      .Split [|'/'|]
    |>Array.filter (fun item -> (item <> ""))
    |>Array.rev).[0]
  { webTask with fileName = newFileName }

let createErrorFn errorMessage (param:WebTask) =
  { errorDescription=errorMessage;url=param.url }
