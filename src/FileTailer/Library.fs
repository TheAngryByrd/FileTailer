namespace FileTailer

module BCLExtensions = 
    let inline private NullOrEmptyToOption value = 
        if System.String.IsNullOrEmpty value then None
        else Some value
    
    type System.IO.StreamReader with
        member this.AsyncReadLine() = async { let! line = this.ReadLineAsync() |> Async.AwaitTask
                                              return NullOrEmptyToOption line }

module FileTailerModule = 
    open System
    open System.IO
    open FSharp.Control
    open Mono.Unix.Native
    open System.Runtime.InteropServices
    open BCLExtensions
    
    let (|Windows|Unix|) _ = 
        if Path.DirectorySeparatorChar = '\\' then Windows
        else Unix
    
    type FileStats = 
        | WindowsFile of uint64
        | UnixFile of Stat
    
    #nowarn "9" "51"
    
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type BY_HANDLE_FILE_INFORMATION = 
        val mutable FileAttributes : uint32
        val mutable CreationTime : System.Runtime.InteropServices.ComTypes.FILETIME
        val mutable LastAccessTime : System.Runtime.InteropServices.ComTypes.FILETIME
        val mutable LastWriteTime : System.Runtime.InteropServices.ComTypes.FILETIME
        val mutable VolumeSerialNumber : uint32
        val mutable FileSizeHigh : uint32
        val mutable FileSizeLow : uint32
        val mutable NumberOfLinks : uint32
        val mutable dwAllocationGranularity : uint32
        val mutable FileIndexHigh : uint32
        val mutable FileIndexLow : uint32
    
    [<DllImport("kernel32.dll", SetLastError = true)>]
    extern bool GetFileInformationByHandle(IntPtr hFile, BY_HANDLE_FILE_INFORMATION * lpFileInformation)
    
    let getWindowsFileStat filePath = 
        let mutable objectFileInfo = new BY_HANDLE_FILE_INFORMATION()
        let fs = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite ||| FileShare.Delete)
        GetFileInformationByHandle(fs.SafeFileHandle.DangerousGetHandle(), &&objectFileInfo) |> ignore
        let combinedFileIndex = objectFileInfo.FileIndexHigh.ToString() + objectFileInfo.FileIndexLow.ToString()
        combinedFileIndex
        |> UInt64.Parse
        |> WindowsFile
    
    //https://stackoverflow.com/questions/10129623/unique-file-identifier
    let isSameFileWindows (oldFileStats : uint64) (newFileStat : uint64) = oldFileStats = newFileStat
    
    let getUnixFileStat filePath = 
        Syscall.stat (filePath)
        |> snd
        |> UnixFile
    
    //https://www.gnu.org/software/libc/manual/html_node/Attribute-Meanings.html
    let isSameFilePosix (oldFileStats : Stat) (newFileStat : Stat) = 
        newFileStat.st_ino = oldFileStats.st_ino && newFileStat.st_dev = oldFileStats.st_dev
    
    let isSameFile oldFileStats newFileStats = 
        match (oldFileStats, newFileStats) with
        | (WindowsFile oldWStat, WindowsFile newWStat) -> isSameFileWindows oldWStat newWStat
        | (UnixFile oldUStat, UnixFile newUStat) -> isSameFilePosix oldUStat newUStat
        | _ -> false
    let isNotSameFile x =  isSameFile x >> not
    let getFileStat filePath = 
        match () with
        | Windows -> getWindowsFileStat filePath
        | Unix -> getUnixFileStat filePath
    
    let getFile filePath fromRotate = 
        async { 
            let streamReader = 
                new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, 
                                                FileShare.ReadWrite ||| FileShare.Delete))
            if not fromRotate then streamReader.BaseStream.Seek(0 |> int64, SeekOrigin.End) |> ignore
            return streamReader
        }
    
    let getRetryFilePath filePath fromRotate = 
        async { 
            let dontskipSome = fromRotate || (not <| File.Exists filePath)
            while not <| File.Exists filePath do
                do! Async.Sleep 100
            return! getFile filePath dontskipSome
        }
    
    let inline private TryWithDefault defaultVal f = 
        try 
            f()
        with | ex -> defaultVal

    type TrackOption = TrackByName | DoNotTrack
    
    let getAsyncFileRead howToGetFile filename trackFile = 
        asyncSeq { 
           
            let rec readLoop (stream : StreamReader) (oldFileStat : FileStats) = 
                asyncSeq { 
                    match trackFile with
                    | TrackByName when (fun () -> 
                                            filename
                                            |> getFileStat
                                            |> isNotSameFile oldFileStat)
                                            |> TryWithDefault false ->

                        yield! openFile filename true
                    | _ -> 
                        let! line = stream.AsyncReadLine()
                        match line with
                        | Some v -> 
                            yield v
                        | None -> 
                            do! Async.Sleep 1
                        yield! readLoop stream oldFileStat
                }
            and openFile filename fromRotate = 
                asyncSeq { 
                    let! newfile = howToGetFile filename fromRotate
                    let newfileStat = getFileStat filename
                    yield! readLoop newfile newfileStat
                }

            yield! openFile filename false
        }
    let getAsyncFileRead' = getAsyncFileRead getRetryFilePath
