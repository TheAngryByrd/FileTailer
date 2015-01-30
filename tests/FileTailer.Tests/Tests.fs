module FileTailer.Tests

open FileTailer
open NUnit.Framework

[<Test>]
let ``hello returns 42`` () =
  let result = FileTailerModule.TailFile "test.txt"
  Assert.AreEqual(0,0)
