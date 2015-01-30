namespace System
open System.Reflection

[<assembly: AssemblyTitleAttribute("FileTailer")>]
[<assembly: AssemblyProductAttribute("FileTailer")>]
[<assembly: AssemblyDescriptionAttribute("A short tailer in F# that tails a file event after deletes or rotates.  Typically used for logs.")>]
[<assembly: AssemblyVersionAttribute("1.0")>]
[<assembly: AssemblyFileVersionAttribute("1.0")>]
do ()

module internal AssemblyVersionInformation =
    let [<Literal>] Version = "1.0"
