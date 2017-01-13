//module WebTask

type WebTask = {path:List<string>;html:string;last:string}
let createWebTask setPathFn setHtmlFn setLastFn url =
  {
    path = setPathFn url
    html = setHtmlFn url
    last = setLastFn url
  }

let webTaskToString webTask =
  let path = 
    webTask.path 
    |> List.reduce (fun all item->(all+item))

  sprintf "   path:%s \n   html:%s \n   last:%s" 
    path
    webTask.html 
    webTask.last

let createSetPath basePath url =
  basePath :: [url]

let setPath = createSetPath "base"

let setHtml url =
  "this is set html:" + url

let setLast url =
  "this is set last:" + url

let createFn = createWebTask setPath setHtml setLast

let webTasks = 
  ["url 1";"url 2";"url 3"]
  |> List.map createFn

webTasks
  |> List.map webTaskToString
  |> List.iter (printfn "Got a task:\n%s") 

