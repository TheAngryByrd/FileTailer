module FileTailer 
open System
open System.IO
open FSharp.Control

//
//type Streaming =
//    | File of StreamReader
//    | InMemory of StreamReader

let private openFileAsReadOnly path = 
    if File.Exists path then
         new StreamReader(new FileStream(path,FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete)) 
    else
         new StreamReader(new MemoryStream()) 

let private tailFileInit path = asyncSeq {
    let stream = path |> openFileAsReadOnly
    stream.ReadToEndAsync() |> Async.AwaitTask |> ignore
    
    let rec read (streamer : StreamReader) =   asyncSeq {
            do! Async.Sleep 1
            if File.Exists(path) |> not  then 
                streamer.Dispose()
                yield! tryLoadFile(path)

            while streamer.EndOfStream |> not do
                yield streamer.ReadLine()
           
            yield! read (streamer)
        }
        and tryLoadFile(path : string) = asyncSeq {
            do! Async.Sleep 1
            if File.Exists(path) |> not  then 
                yield! tryLoadFile(path)
            else 
                let newStream = path |> openFileAsReadOnly
                newStream.BaseStream.Seek(int64 0, SeekOrigin.Begin) |> ignore
                yield! read(newStream)
        } 

    yield! read (stream)
}

let private filterOutNullString tailFile =
    tailFile |> AsyncSeq.filter (String.IsNullOrEmpty >> not)

let TailFile = tailFileInit >> filterOutNullString

