// Learn more about F# at http://fsharp.org. See the 'F# Tutorial' project
// for more guidance on F# programming.


#r "../../lib/FSharp.AsyncExtensions.dll"
#load "Library.fs"
open FileTailer
open FSharp.Control
open System.IO
open System.Threading

let source = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "Hello3.txt") 

printfn "%s" source


let getFileAsync = FileTailerModule.getAsyncFileRead FileTailerModule.getRetryFilePath 

let readFile = getFileAsync source |> AsyncSeq.iter(fun s -> printfn "%s" s)
let cToken = new CancellationTokenSource()
Async.Start(readFile,cToken.Token)

let appendAllLines (path : string) (lines : seq<string>) = File.AppendAllLines(path, lines)


async {
    [1 .. 1000000] 
    |> Seq.map(string) 
    |> appendAllLines source

    //|> Seq.iter (fun x -> appendAllLines source [x]) 
    } |> Async.Start

cToken.Cancel()