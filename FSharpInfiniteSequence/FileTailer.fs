module FileTailer 
open System
open System.IO
open FSharp.Control

let private createWatcher path =
    let watcher = new FileSystemWatcher()
    let dirInfo = DirectoryInfo(path)
    watcher.Path <- dirInfo.Parent.FullName
    watcher.Filter <- dirInfo.Name
    watcher.EnableRaisingEvents <- true
    watcher

let private openFileAsReadOnly path = new StreamReader(new FileStream(path,FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete))

let private tailFileInit path = asyncSeq {
    let stream = path |> openFileAsReadOnly |> ref
    let watcher = createWatcher path
    
    watcher.Renamed.Subscribe(fun _ ->   
                                    try
                                        stream := path |> openFileAsReadOnly
                                    with
                                    | ex -> ()) |> ignore //if we couldn't re-open the file, it was probably moved
                          
    watcher.Created.Subscribe(fun _ ->   
                                stream := path |> openFileAsReadOnly) |> ignore

    (!stream).ReadToEndAsync() |> Async.AwaitTask |> ignore

    let rec loop (streamer : ref<StreamReader>) =   asyncSeq{
        do! Async.Sleep 1
        yield (!streamer).ReadLine()
        yield! loop (streamer)
        }
    yield! loop (stream)
}

let private filterOutNullString tailFile =
    tailFile |> AsyncSeq.filter (String.IsNullOrEmpty >> not)

let TailFile = tailFileInit >> filterOutNullString

