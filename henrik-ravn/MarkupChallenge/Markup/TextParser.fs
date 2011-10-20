module TextParser

open System
open System.Collections.Generic
open System.Text
open System.Text.RegularExpressions
open Markup
open StringEx

/// Exception type, containing line-number and error message
exception ParseError of int * string

/// raise a parse error
let failParse lineNo message = raise (ParseError(lineNo, message))

/// Encapsulates options to the parser
type ParseOptions = { 
    ParseLinks : bool;    // true, if the link syntax of the spec should be parsed
    DocTags : Set<string> // contains all tag names that are supposed to be parsed as sub-documents
    }

/// Specifies the operations available for parse nodes
type IParseNode = 
    /// Return this parse ndoe as a Markup node
    abstract member ToMarkup : unit -> Markup
    /// Return true, if this node should be included in the Markup, false otherwise
    abstract member Include : unit -> bool

/// Return a ParseNode converted to a Markup node
let parseNodeToMarkup (node:IParseNode) = node.ToMarkup()

/// A text parse node
type TextParseNode(text:string) =
    interface IParseNode with
        member this.ToMarkup() = Text(text)
        member this.Include() = true

/// A tag parse node
type TagParseNode(name:string, indent:int) =
    let textBuffer = new StringBuilder()
    let childNodes = new List<IParseNode>()

    interface IParseNode with
        member this.ToMarkup() = 
            Tag(name, childNodes |> Seq.filter (fun n -> n.Include()) 
                                 |> Seq.map parseNodeToMarkup)
        member this.Include() = 
            this.EmitAndClearText()
            childNodes.Count > 0

    member this.IsMoreIndentedThan n = indent > n
    member this.AddText(text:string) = textBuffer.Append(text) |> ignore
    member this.AddTag(name, delta) = 
        this.EmitAndClearText()
        let newTag = new TagParseNode(name, indent + delta)
        childNodes.Add(newTag)
        newTag
    member this.EmitAndClearText() =
        if textBuffer.Length > 0 then
            childNodes.Add(new TextParseNode(textBuffer.TrimEnd()))
            textBuffer.Clear() |> ignore

/// The different parts of a line
type LinePart = LineText of string | StartInlineTag of string | StartDocTag of string * string | EndTag | Link of string * string

/// The different parse actions
type ParseAction = PushTag of string * int | AddText of string | PopTags of int

///
/// Split a single line into LineParts
///
let splitLineIntoParts options lineNo start (line:string) = 
    let text = new StringBuilder()
    let parts = new List<LinePart>()
    let addText(followedBy) = 
        parts.Add(LineText(text.Consume()))
        Seq.iter parts.Add followedBy
    let mutable parsingTag = false
    let mutable parsingLink = false
    let mutable keyAdded = false
    let mutable pos = start
    while pos < line.Length do
        match line.[pos] with
        | '\\' when not parsingTag -> 
            let next = pos + 1
            if (next < line.Length) && (not (isValidTagNameChar line.[next])) then
                // escape the next char
                text.AddChar line.[next] 
            else
                // begin parsing a tag name
                addText []
                parsingTag <- true
                text.AddChar line.[next]
            pos <- next

        | '{' when parsingTag ->
            let tagName = text.Consume()
            if not (Set.contains tagName options.DocTags) then
                parts.Add(StartInlineTag(tagName))
            else
                if parsingLink then failParse lineNo (sprintf "Illegal document tag '%s' while parsing link" tagName)
                parts.Add(StartDocTag(tagName, line.Substring(pos+1).TrimEnd()))
                pos <- line.Length
            parsingTag <- false

        | '}' when not parsingTag -> addText [EndTag]

        | '[' when options.ParseLinks && not (parsingTag || parsingLink)->
            addText [StartInlineTag("link")]
            parsingLink <- true
            keyAdded <- false

        | '|' when parsingLink ->
            addText [StartInlineTag("key")]
            keyAdded <- true

        | ']' when parsingLink ->
            addText (if keyAdded then [EndTag; EndTag] else [EndTag])
            parsingLink <- false
            
        | c -> 
            if c = '{' then failParse lineNo "'{' must be escaped to be part of text."
            if parsingTag && not (isValidTagNameChar c) then
                failParse lineNo (sprintf "Illegal character '%c' in tag name" c)
            text.AddChar c
        pos <- pos + 1

    if parsingTag then failParse lineNo "Missing '{'."
    if parsingLink then failParse lineNo "Unterminated link. Missing ']'." 
    addText []
    parts

///
/// Convert the incoming lines to ParseActions
///
let splitLinesIntoActions options lines = seq {
    let paraLinkExp = new Regex(@"\[(.*)\]\s*\<(.*)\>", RegexOptions.Compiled)
    let group (m:Match) (n:int) = m.Groups.[n].Value

    let pushTag name = PushTag(name,0)
    let popTags = PopTags Int32.MaxValue
    let paraLink group = [pushTag "link_def"; pushTag "link"; AddText(group 1); popTags; pushTag "url"; AddText(group 2); popTags]
    let lineLink link key = [pushTag "link"; AddText link; pushTag "key"; AddText key;  popTags; popTags]

    let getParaLink text =
        if not options.ParseLinks then None
        else let m = paraLinkExp.Match(text)
             if m.Success then Some(paraLink (group m)) else None

    let isVerbatim = ref false
    let indent = ref 0
    let emitP = ref true
    let prevBlank = ref false
    let lineBuffer = new Queue<string>()
    let subTags = new Stack<bool>();

    let handleLineParts lineNo delta = function
        | Link (link,key) -> lineLink link key
        | LineText text -> if String.isEmpty text then [] else [AddText(text+" ")]
        | StartInlineTag tagName -> 
            subTags.Push(false)
            [pushTag tagName]
        | StartDocTag (tagName,rest) -> 
            subTags.Push(true)
            lineBuffer.Enqueue(String.indent (!indent+delta) rest)
            [pushTag tagName; pushTag "p"]
        | EndTag -> 
            if subTags.Count = 0 then failParse lineNo "Unmatched or unescaped '}' character."
            [popTags] @ (if subTags.Pop() then [popTags] else [])

    let beginLine indent isVerbatim line =
        if String.isEmpty line then true, 0, isVerbatim, line, []
        else let indent' = String.indexOfFirst (not << Char.IsWhiteSpace) line
             let delta = min 3 (indent' - indent)
             if delta < 0 then false, delta, false, line.Substring(indent + delta), [PopTags(indent+delta)]
             elif isVerbatim then false, 0, true, line.Substring(indent), []
             elif delta = 3 then false, 3, true, line.Substring(indent+3), [PushTag("pre",3)]
             else let delta' = if delta = 1 then 0 else delta
                  false, delta', false, line.Substring(indent + delta'), []

    for lineNo,rawLine in lines do
        lineBuffer.Enqueue(rawLine)
        while lineBuffer.Count > 0 do
            let isBlank, delta, isVerbatim', line, initTags = beginLine !indent !isVerbatim (lineBuffer.Dequeue())
            yield! initTags
            isVerbatim := isVerbatim'
            indent := !indent + delta
            if isVerbatim' then yield AddText (line + "\n")
            else if isBlank then
                    if not !prevBlank then yield popTags
                    prevBlank := true
                    emitP := true
                 else prevBlank := false
                      let first = String.firstCharIn line
                      if delta = 2 then yield PushTag(sectionTagName first, 2)
                      let tags, start = match first with
                                        | '*' -> let headerLevel = String.indexOfFirst ((<>) '*') line
                                                 emitP := false
                                                 [pushTag ("h" + string headerLevel)], headerLevel + 1
                                        | '#' 
                                        | '-' -> indent := !indent + 2
                                                 [PushTag("li",2)], 2
                                        | _   -> [], 0
                      yield! tags
                      match getParaLink line with
                      | Some(paraLink') -> yield! paraLink'
                      | None ->
                          if !emitP then yield pushTag "p"
                          emitP := false
                          yield! splitLineIntoParts options lineNo start line
                                 |> Seq.collect (handleLineParts lineNo delta)

    if subTags.Count > 0 then failParse 0 "Some tag unmatched (missing '}'?) before end of document."
    }


///
/// Return a parse tree built from a sequence of parse actions
///
let parseNodesFromActions actions =
    let root = new TagParseNode("body",0)
    let states = new Stack<TagParseNode>([root])
    let pushTag name delta = states.Push(states.Peek().AddTag(name,delta))
    let pop() = states.Pop() |> ignore
    let popTagsUntil n = pop(); while states.Peek().IsMoreIndentedThan n do pop()

    actions 
    |> Seq.iter (function PushTag (name,delta) -> pushTag name delta
                        | AddText text -> states.Peek().AddText(text)
                        | PopTags targetIndent -> popTagsUntil targetIndent)
    root

///
/// Return a Markup tree parsed from a text stream 
///
let parseMarkupFromReader options (reader:System.IO.TextReader) =
    let trimAndExpandTabs = String.trimEnd >> (String.replace "\t" "        ")
    let readLine lineNo = match reader.ReadLine() with
                          | null -> None
                          | line -> Some((lineNo, trimAndExpandTabs line), lineNo+1)
    Seq.unfold readLine 1 
    |> Seq.skipWhile (fun (_,line) -> line.Length = 0 || isModeLine line)
    |> splitLinesIntoActions options
    |> parseNodesFromActions 
    |> parseNodeToMarkup

///
/// Return a Markup tree parsed from a file
///
let parseMarkupFromFile options (filepath:string) = 
    use reader = new System.IO.StreamReader(filepath, Encoding.UTF8)
    parseMarkupFromReader options reader
