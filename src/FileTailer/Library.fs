namespace FileTailer

module FileTailerModule =
    open System
    open System.IO
    open FSharp.Control

//    let private openFileAsReadOnly path = 
//        if File.Exists path then
//             new StreamReader(new FileStream(path,FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete)) 
//        else
//             new StreamReader(new MemoryStream()) 
//
//    let private tailFileInit path (getFile : string -> StreamReader) = asyncSeq {
//        let stream = path |> getFile
//        stream.ReadToEnd() |> ignore
//    
//        let rec read (streamer : StreamReader) =   asyncSeq {
//                do! Async.Sleep 1
//                if File.Exists(path) |> not  then 
//                    streamer.Dispose()
//                    yield! tryLoadFile(path)
//                while not streamer.EndOfStream  do
//                    yield streamer.ReadLine()           
//                yield! read (streamer)
//            }
//            and tryLoadFile(path) = asyncSeq {
//                do! Async.Sleep 1
//                if File.Exists(path) |> not  then 
//                    yield! tryLoadFile(path)
//                else 
//                    let newStream = path |> getFile
//                    newStream.BaseStream.Seek(int64 0, SeekOrigin.Begin) |> ignore
//                    yield! read(newStream)
//            } 
//
//        yield! read (stream)
//    }
//
//    let private tailFileInit' path = 
//        tailFileInit path openFileAsReadOnly
//
//    let private filterOutNullString tailFile =
//        tailFile |> AsyncSeq.filter (String.IsNullOrEmpty >> not)
//
//    let TailFile = tailFileInit' >> filterOutNullString

    let getFile filePath = async{
            return new StreamReader(new FileStream(filePath,FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete)) 
        }
    let getRetryFilePath filePath = async {
        while not <| File.Exists filePath do
            do! Async.Sleep 100
        return! getFile filePath
    }
    
    let getAsyncFileRead howToGetFile filename = asyncSeq {
        let! file = howToGetFile filename     
        
        let rec readLoop (stream : StreamReader) = asyncSeq {
            
           let! line = (stream.ReadLineAsync() |> Async.AwaitTask)
           if String.IsNullOrEmpty line then 
                yield! rest (stream)
           else 
                yield line
                yield! readLoop(stream)
           }
        and rest (stream : StreamReader) = asyncSeq {
           do! Async.Sleep 1
           yield! readLoop stream
           } 
        yield! readLoop(file)
    }
      
            