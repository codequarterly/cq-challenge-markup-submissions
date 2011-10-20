module TestMarkup

open Markup
open TextParser
open XmlBackend
open XmlComparer

open System
open System.IO


let testFolder = @"..\..\..\Specs\tests"
let errorFolder = @"..\..\..\Specs\errtests"
let testLogName = @"..\..\..\testlog.txt"

let options = { ParseLinks = true; DocTags = Set(["note"]) }

let parser = (parseMarkupFromFile options)

let filePattern = "*.txt" // set this to something more specific to test a single file
let runTests = true       // set this to false to pretty print Markup instead of testing
let useLogFile = false    // set this to true to write errors (or pretty-print output) 
                          // to a log file instead of stdout

/// Utility function that formats a duration for output
let timeString (duration:TimeSpan) =
    if duration.TotalSeconds < 1.0 then 
        sprintf "%d ms" duration.Milliseconds
    else 
        sprintf "%0.1f s" duration.TotalSeconds


let fileNames path compareExt = 
    let baseName = Path.GetFileNameWithoutExtension(path)
    let comparePath = Path.Combine(Path.GetDirectoryName(path), baseName + compareExt)
    baseName, comparePath


let errorLine lineNo message = sprintf "Parse error in line %d. %s" lineNo message


/// Pretty-printing the output of a single file (useful for debugging)
let parseAndPrettyPrint log folder pattern =
    Directory.EnumerateFiles(folder, pattern) 
        |> Seq.map parser
        |> Seq.iter (prettyPrint log 0)


/// Testing a single file for producing the correct output
let testFile path =
    let baseName, xmlPath = fileNames path ".xml"
    try
        let expected = loadXmlFile xmlPath
        let actual = createXmlDocument (parser path)
        let result = compareXmlNodes actual expected
        baseName, result, if result = 0 then String.Empty else "output was incorrect."
    with 
    | ParseError(lineNo, message) ->
        baseName, 1, errorLine lineNo message
    | e -> baseName, 2, sprintf "Unknown error %A" e


/// Testing a single file for producing the correct error ouput
let testError path =
    let baseName, logPath = fileNames path ".log"
    let expected = File.ReadAllText(logPath).Trim()
    try
        let _ = createXmlDocument (parser path)
        baseName, 1, (sprintf "No error found, expected '%s'" expected)
    with 
    | ParseError(lineNo, message) ->
        let actual = errorLine lineNo message
        let result = if actual = expected then 0 else 2
        baseName, result, if result = 0 then String.Empty else (sprintf "Wrong error found, expected\n'%s', got\n'%s'" expected actual)
    | e -> baseName, 3, sprintf "Unknown error %A" e


/// Testing a range of files
let testAndCompare log folder pattern testFunc =
    Directory.EnumerateFiles(folder, pattern) 
        |> Seq.map testFunc 
        |> Seq.filter (fun (_,res,_) -> res <> 0) 
        |> Seq.iter (fun (n,_,m) -> fprintfn log "%s failed: %s" n m)    


let run() = 
    use log = if useLogFile then new StreamWriter(testLogName) :> TextWriter else stdout
    if runTests then
        testAndCompare log testFolder filePattern testFile
        testAndCompare log errorFolder filePattern testError
    else
        parseAndPrettyPrint log testFolder filePattern

let sw = System.Diagnostics.Stopwatch.StartNew()
run()
sw.Stop()

printfn "Done. Elapsed time: %s" (timeString sw.Elapsed)

