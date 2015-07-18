// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#r "../../lib/FSharp.AsyncExtensions.dll"
#r "../../packages/Mono.Posix/lib/net40/Mono.Posix.dll"
#load "Library.fs"
open FileTailer
open FSharp.Control
open System.IO
open System.Threading

let source = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "Hello3.txt") 

printfn "%s" source


let getFileAsync = FileTailerModule.getAsyncFileRead FileTailerModule.getRetryFilePath

let readFile = getFileAsync source FileTailerModule.TrackOption.TrackByName |> AsyncSeq.map(sprintf "File1 - %s") |> AsyncSeq.iter(printfn "%s")
let readFile2 = getFileAsync source FileTailerModule.TrackOption.DoNotTrack |> AsyncSeq.map(sprintf "File2 - %s") |> AsyncSeq.iter(printfn "%s")
let cToken = new CancellationTokenSource()
Async.Start(readFile,cToken.Token)
Async.Start(readFile2,cToken.Token)

let appendAllLines (path : string) (lines : seq<string>) = File.AppendAllLines(path, lines)


async {
    [1 .. 10] 
    |> Seq.map(string)    
    //|> appendAllLines source
    |> Seq.iter (fun x -> appendAllLines source [x]) 
    } |> Async.Start

cToken.Cancel()