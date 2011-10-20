open Markup
open TextParser
open StringEx
open XmlBackend

open System
open System.Collections.Generic
open System.IO
open System.Xml


/// Parses a Markup file and processes it with a given processor
let processFile options proc path =
    let markup = parseMarkupFromFile options path
    proc markup path
    1

/// Return a sequence of all the files on a sequence of file patterns
let enumFiles patterns = seq {
    for pattern in patterns do 
        yield! Directory.EnumerateFiles(".\\", pattern)
    }

/// Return a Markup processor based on the name given.
let getProcessor procName =
    let makeDestName sourceName ext =
        Path.Combine(Path.GetDirectoryName(sourceName), Path.GetFileNameWithoutExtension(sourceName)+ext)

    if procName = "xml" then
        (fun markup sourceName -> 
            let xml = createXmlDocument markup
            let destName = makeDestName sourceName ".xml"
            xml.Save(destName))
    else
        failwithf "Unknown processor name '%s'" procName


/// return the args array split into two sequences, the first for options, 
/// the second for filenames
let splitOptionsFromFilenames (args:string array) = 
    let keyOrEmpty map key =
        if Map.containsKey key map then map.[key] else Seq.empty
    let groups = args |> Seq.groupBy (fun a -> a.StartsWith("-")) |> Map.ofSeq
    keyOrEmpty groups true, keyOrEmpty groups false 


/// Return a Markup processing function and a sequence of file patterns
let parseOptions(options,filePatterns) =
    let mutable enableLinkSyntax = true
    let mutable processorName = "xml"
    let docTags = new HashSet<string>()

    if Seq.isEmpty filePatterns then
        failwith "No files given to process"

    for opt in options do
        if opt = "-nolinks" then enableLinkSyntax <- false
        elif opt.StartsWith("-proc:") then 
            if opt.Length = "-proc:".Length then
                failwith "Processor name option found without name"
            processorName <- opt.Substring("-proc:".Length).ToLower()
        elif opt.StartsWith("-subdoc:") then 
            if opt.Length = "-subdoc:".Length then
                failwith "Subdoc option found without tag name"
            docTags.Add(opt.Substring("-subdoc:".Length)) |> ignore
        else failwithf "Unknown option '%s'" opt
    if docTags.Count = 0 then
        docTags.Add("note") |> ignore

    processFile { ParseLinks = enableLinkSyntax; DocTags = Set(docTags) } (getProcessor processorName), filePatterns


let usage =
    "USAGE: MarkupProcessor [options] filePattern ...\n" +
    "   where options can be any of:\n" +
    "   -nolinks ........ disables parsing of links.\n" +
    "   -proc:name ...... use the processor 'name'. Default is XML\n" +
    "   -subdoc:tag1 .... adds tag1 as a sub-document tagname.\n" +
    "                     If no subdocs given on the command line,\n" +
    "                     the tagname 'note' is automatically added.\n\n" +
    "   Each filePattern can be a filename or a standard shell pattern.\n"

[<EntryPoint>]
let main args =
    printfn "MarkupProcessor 1.0\n"

    try
        let fileProcessor, filePatterns = parseOptions (splitOptionsFromFilenames args)

        let sw = System.Diagnostics.Stopwatch.StartNew()
        let count = filePatterns 
                    |> enumFiles
                    |> Seq.map fileProcessor
                    |> Seq.sum
        sw.Stop()
        printfn "Done. %d file(s) processed. Elapsed time %A" count sw.Elapsed
        0
    with ex -> printfn "Error: %s\n" ex.Message
               printfn "%s" usage
               1

