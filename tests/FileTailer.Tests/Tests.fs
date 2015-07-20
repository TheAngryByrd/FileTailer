module FileTailer.Tests

open FileTailer
open NUnit.Framework
open System
open System.IO
open FSharp.Control
open FsUnit
open System.Threading
open System.Diagnostics

let deleteFileOutsideOfProcess file =
   // try
        use p = new Process(StartInfo = ProcessStartInfo("cmd.exe", sprintf "/c del \"%s\""  file, UseShellExecute = false) )        
        p.Start() |> ignore
        p.WaitForExit()
    //with | _ -> ()

let generateFileName ()=
    Guid.NewGuid().ToString("N") |> sprintf "%s.txt"

let appendLinesToFile filename lines =
    File.AppendAllLines(filename, lines)

[<Test>]
let ``Wait to tail non-existent file`` () =
    let fileName = generateFileName ()
    let list = ResizeArray<string>()
    FileTailerModule.getAsyncFileRead' fileName FileTailerModule.DoNotTrack
                |> AsyncSeq.iter(list.Add)
                |> Async.Start
    Thread.Sleep 250
    list.Count |> should equal 0
    [1.. 10]
    |> Seq.map string
    |> appendLinesToFile fileName
    Thread.Sleep 250
    list.Count |> should equal 10

[<Test>]
let ``Should start at end of already existent file`` () =
    let fileName = generateFileName ()
    [1.. 10]
    |> Seq.map string
    |> appendLinesToFile fileName
    File.Exists(fileName) |> should be True

    let list = ResizeArray<string>()
    FileTailerModule.getAsyncFileRead' fileName FileTailerModule.DoNotTrack
                |> AsyncSeq.iter(list.Add)
                |> Async.Start

    Thread.Sleep 250
    list.Count |> should equal 0
    [1.. 10]
    |> Seq.map string
    |> appendLinesToFile fileName

    Thread.Sleep 250
    list.Count |> should equal 10

[<Test>]
let ``Shouldn't follow file deletions by setting DoNotTrack`` () =
    let fileName = generateFileName ()
    

    let list = ResizeArray<string>()
    FileTailerModule.getAsyncFileRead' fileName FileTailerModule.TrackByName
                |> AsyncSeq.iter(list.Add)
                |> Async.Start

    Thread.Sleep 250
    list.Count |> should equal 0
    [1.. 10]
    |> Seq.map string
    |> appendLinesToFile fileName
    let fileInfo = FileInfo(fileName)
    Thread.Sleep 250
    list.Count |> should equal 10

    deleteFileOutsideOfProcess(fileInfo.FullName)
    Thread.Sleep 250
    [1.. 10]
    |> Seq.map string
    |> appendLinesToFile fileName

    Thread.Sleep 250
    list.Count |> should equal 10


[<TearDown>]
let CleanUp() =
    ()

[<TestFixtureTearDown>]
let CleanUp2() =
    try 
        let files =
         DirectoryInfo(Environment.CurrentDirectory).GetFiles("*.txt")
         |> Seq.toList
        files |> Seq.iter(fun x -> File.Delete x.FullName)
    with | ex -> ()