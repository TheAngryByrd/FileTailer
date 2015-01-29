// Learn more about F# at http://fsharp.net
// See the 'F# Tutorial' project for more help.
open System
open System.IO
open FSharp.Control

[<EntryPoint>]
let main argv = 
    
    FileTailer.TailFile @"C:\Users\jimmy_000\Documents\test.txt" 
        |> AsyncSeq.iter Console.WriteLine
        |> Async.Start
 
    Console.ReadLine() |> ignore
    0
