module Common

type TryValue<'a,'b> =
  | Value of 'a
  | Error of 'b
let tryRun createErrorFn param fn =
  try 
    Value (fn param)
  with
    | ex  -> Error (createErrorFn ex.Message param)
let wrapTryRun createErrorFn fn param =
  match param with
  | Value a -> (tryRun createErrorFn a fn)
  | Error e -> param


// let u = Uri("http://www.google.com/somepage")

// let fileName = (
//     Uri(u,"https://www.yahoo.com/path%20with%20spaces/images/some Image.jpg?param1=one&param2=two")
//       .LocalPath
//       .Split [|'/'|]
//     |>Array.rev).[0]

// let path =
//   let u = 
//       Uri(u,"https://www.yahoo.com/path%20with%20spaces/images/someImage.jpg?param1=one&param2=two")
//           .LocalPath
//           .Split [|'/'|]
//       |> Array.filter (fun item -> (item <> ""))
//       |> Array.rev
//   u.[1..]
//       |> Array.rev
