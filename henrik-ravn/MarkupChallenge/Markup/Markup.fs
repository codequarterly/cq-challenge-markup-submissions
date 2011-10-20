module Markup

open StringEx

open System
open System.IO
open System.Text
open System.Collections.Generic

///
/// This is the markup datatype. When parsing is done, you will get a tree of 
/// these nodes, rooted in a Tag("body", ....) node
///
/// Each node can be either a Tag node with one or more childNodes, or a Text node
///
/// Check out the prettyPrint routine below for a really simple example of how to walk the tree
///
/// A slightly more involved example is the createXmlDocument function in the XmlBackend.fs file
///
type Markup = Tag of string * Markup seq
            | Text of string


///
/// Predicate that is used to figure out which characters are valid in tag names
///
let isValidTagNameChar c = Char.IsLetterOrDigit c || c = '.' || c = '+' || c = '_'


///
/// Predicate that determines whether a line is the emacs mode line
///
let isModeLine = String.contains "-*- mode: markup; -*-"


///
/// This function is called to determine the section tag (i.e. when indenting 2 columns)
///
let sectionTagName = function
    | '#' -> "ol"
    | '-' -> "ul"
    | _   -> "blockquote"
    

///
/// Pretty-prints a Markup tree to a TextWriter by recursive descent
///
let rec prettyPrint writer indent = function
    // There are two types of nodes:

    | Tag(name, childNodes) ->
        // A Tag node, in which case we write out the tag name, and recursively
        // pretty-print the children indented 2 columns relative to the parent tag
        fprintfn writer "%s%s" (String.ofSpaces indent) name
        childNodes |> Seq.iter (prettyPrint writer (indent+2))

    | Text text ->
        // A Text node, so we just write out the string cropped to terminal width
        text.Split('\n') |> Seq.map (String.crop (74-indent)) |> Seq.iter (fprintfn writer "%s:%s" (String.ofSpaces indent))


