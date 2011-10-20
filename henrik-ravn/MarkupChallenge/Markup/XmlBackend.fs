module XmlBackend

open Markup

open System.Xml

module private Internal =
    /// Appends a text node to the parent node in the doc document
    let addText (doc:XmlDocument) (parent:XmlNode) text =
        parent.AppendChild(doc.CreateTextNode(text)) |> ignore

    /// Appends an element named name to the parent node in the doc document
    let addElement (doc:XmlDocument) (parent:XmlNode) name = 
        parent.AppendChild(doc.CreateElement(XmlConvert.EncodeLocalName(name)))

    /// Recursively converts Markup nodes to XML nodes 
    let rec addChild doc parent = function
        | Tag(name, children) -> Seq.iter (addChild doc (addElement doc parent name)) children
        | Text text -> addText doc parent text

    /// Converts a root markup node (and all its children recursively) to the XML document in xmlDoc
    let addRoot xmlDoc markupRoot = 
        addChild xmlDoc xmlDoc markupRoot
        xmlDoc

///
/// Creates an XML document tree corresponding to the Markup passed in.
///
/// Returns the XMLDocument instance with the Markup tree.
///
let createXmlDocument = function
    | Tag("body", children) as rootTag -> Internal.addRoot (new XmlDocument()) rootTag
    | Tag(name,_) -> failwithf "Bad root tag '%s', root tag must be 'body'" name
    | Text _ ->  failwith "Bad root node Text, the root must be a 'body' tag"