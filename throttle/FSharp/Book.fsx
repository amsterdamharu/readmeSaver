#load "WebTask.fsx"
open Common
open System
open HtmlAgilityPack
open WebTask


let prettyUp webTask =
  let dom = webTask.htmlDoc
  let main = dom.DocumentNode.SelectSingleNode("//article")
  dom.DocumentNode.SelectSingleNode("/html/body").AppendChild(main) |> ignore
  [ for x in dom.DocumentNode.SelectNodes("/html/body/*[not(self::article)]") -> x]
    |> List.map (fun el -> el.Remove()) |> ignore
  [ for x in dom.DocumentNode.SelectNodes("//svg") -> x]
    |> List.map (fun el -> el.Remove()) |> ignore
  {webTask with text = webTask.htmlDoc.DocumentNode.OuterHtml}

(* try so far *)
let urls = [
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch1.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch2.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch3.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch4.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch5.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/ch6.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/apA.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/apB.md"
  "https://github.com/getify/You-Dont-Know-JS/blob/master/async%20%26%20performance/apC.md"  ]

let imageProcessor =
  tryWebTaskFromTryUri >>
  ([setBinaryContent;(setPath ["wot"]);setFileName]
  |> List.fold 
    (fun acc fn ->
        acc >> wrapTryRun 
          createErrorFn
          fn)
    id)

//setImages tryWebTaskFromTryUri 
let processor = 
  (stringToTryUri >> tryWebTaskFromTryUri) >>
  ([setHtml;setDocObject;(setPath []);setFileName;prettyUp;(setImages imageProcessor)]
  |> List.fold 
    (fun acc fn ->
        acc >> wrapTryRun 
          createErrorFn
          fn)
    id)

let result = urls |> List.map processor

result
  |> List.filter
    (function
      | Value v -> true
      | _ -> false)
  |> List.map
    (
      (function
        | Value v -> v
        | _ -> failwith "impossibel")
      >>
      (fun webTask ->
        let fileName = (webTask.path |> (List.fold (fun acc p -> acc + "-"+p) "")) + webTask.fileName
        System.IO.File.WriteAllText("out\\"+fileName, webTask.text);
        webTask)
      >>
      (fun webTask ->
        webTask.images
          |> List.map
            (function
              | Value image ->
                let fileName = (image.path |> (List.fold (fun acc p -> acc + "-"+p) "")) + image.fileName
                System.IO.File.WriteAllBytes("out\\images\\"+fileName,image.binaryContent)
                Value image
              | x -> x
            ) |> ignore
        webTask)
    )

let allDocuments = new HtmlDocument()
allDocuments.LoadHtml("<html><head></head><body></body></html>")
let allHtml =
  result
    |> List.filter
      (function
      | Value v -> true
      | _ -> false)
    |> List.map
      (fun webTask ->
        match webTask with
        | Value v -> v
        | _ -> failwith "this cannot happen")
    |> List.fold
      (fun (all:HtmlDocument) (webTask:WebTask) -> 
        all.DocumentNode.AppendChild( webTask.htmlDoc.DocumentNode.SelectSingleNode("//body")) |> ignore
        all)
      allDocuments

System.IO.File.WriteAllText("out\\all.html", allHtml.DocumentNode.OuterHtml);
