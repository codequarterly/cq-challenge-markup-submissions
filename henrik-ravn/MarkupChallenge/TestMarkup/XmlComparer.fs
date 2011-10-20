module XmlComparer

open System
open System.Xml

module private Internal =
    let childNodesOf (node:XmlNode) = 
        if not node.HasChildNodes then Seq.empty
        else seq { for n in node.ChildNodes -> n }

    let attributeMapOf (el:XmlElement) =
        if not el.HasAttributes then Map.empty
        else Map.ofSeq (seq { for a in el.Attributes -> (a.Name,a.Value) })

    let rec compareNodes (n1:XmlNode) (n2:XmlNode) = 
        let nodeTypeRes = compare n1.NodeType n2.NodeType
        if nodeTypeRes <> 0 then nodeTypeRes 
        else match n1.NodeType with
             | XmlNodeType.Document -> compareNodes n1.FirstChild n2.FirstChild
             | XmlNodeType.Element -> compareElements (n1 :?> XmlElement) (n2 :?> XmlElement)
             | _ -> compare (n1.Value.Trim()) (n2.Value.Trim())

    and compareElements (left:XmlElement) (right:XmlElement) =
        let nameRes = compare left.Name right.Name
        if nameRes <> 0 then nameRes
        else let attrRes = compare (attributeMapOf left) (attributeMapOf right)
             if attrRes <> 0 then attrRes
             else Seq.compareWith compareNodes (childNodesOf left) (childNodesOf right)


let compareXmlNodes left right = (Internal.compareNodes left right) 

let loadXmlFile (path:string) = 
    let doc = new XmlDocument()
    doc.Load(path)
    doc

let compareXmlFiles leftPath rightPath =
    let doc1, doc2 = new XmlDocument(), new XmlDocument()
    doc1.Load(leftPath:string)
    doc2.Load(rightPath:string)
    compareXmlNodes doc1 doc2

let compareXml leftXml rightXml =
    let doc1, doc2 = new XmlDocument(), new XmlDocument()
    doc1.LoadXml(leftXml)
    doc2.LoadXml(rightXml)
    compareXmlNodes doc1 doc2
