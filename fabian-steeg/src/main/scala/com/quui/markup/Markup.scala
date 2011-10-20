/**************************************************************************************************
 * Copyright (c) 2010 Fabian Steeg. All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0 which accompanies this
 * distribution, and is available at http://www.eclipse.org/legal/epl-v10.html
 *************************************************************************************************/
package com.quui.markup

import scala.util.matching.Regex
import scala.xml._
import scala.io.Source._

/**
 * A markup processor, see [[http://codequarterly.net/code-challenges/markup/]]
 * @author Fabian Steeg (fsteeg)
 */
object Markup {

  /**
   * Command-line entry point to use the processor to transform markup to XML.
   * @param args The name of the markup text file to parse and output as XML
   */
  def main(args: Array[String]): Unit = println {
    if (args.size != 1) "Pass a single argument: the name of the markup file to process"
    else toXml(parse(fromFile(args(0)).mkString))
  }

  /**
   * Parse markup.
   * @param input The markup text to parse
   * @param sub ''Optional:'' the pattern describing tag names of markup to be parsed as sub-documents 
   * @return A tree representation of the given input
   */
  def parse(input: String, sub: Regex = defaultSubPattern): Body =
    new MarkupParser(sub).parseMarkup(input)

  /**
   * Export parsed markup to XML.
   * @param markup The root element to export to XML
   * @param pretty ''Optional:'' if true, pretty print the XML
   * @return An XML representation of the given element
   */
  def toXml(markup: Element, pretty: Boolean = defaultPrettyOutput): String = {
    val xml = MarkupBackend.toXml(markup)
    if (pretty) new PrettyPrinter(200, 2).format(xml) else xml.toString
  }

  /**
   * Abstract class for elements a markup tree is made of: an element with child elements and a tag.
   * Concrete elements a markup tree is made of can be used with pattern matching to process a 
   * parsed tree. Some elements with non-standard or variable tag names override the tag def.
   */
  sealed abstract class Element(val children: List[Element]) { def tag = getClass.getSimpleName.toLowerCase }
  case class TextElement(text: String) extends Element(Nil)
  case class Body(content: List[Element]) extends Element(content)
  case class BlockQuote(content: List[Element]) extends Element(content)
  case class Ol(content: List[Li]) extends Element(content)
  case class Ul(content: List[Li]) extends Element(content)
  case class Li(content: List[Element]) extends Element(content)
  case class Pre(text: String) extends Element(List(TextElement(text)))
  case class P(content: List[Element]) extends Element(content)
  case class Link(content: List[Element]) extends Element(content)
  case class Key(text: String) extends Element(List(TextElement(text)))
  case class Url(text: String) extends Element(List(TextElement(text)))
  case class LinkDef(link: Link, url: Url) extends Element(List(link, url)) { override def tag = "link_def" }
  case class H(level: Int, content: List[Element]) extends Element(content) { override def tag = super.tag + level }
  case class Tagged(name: String, content: List[Element]) extends Element(content) { override def tag = name }

  /* Configuration defaults: */
  private[markup] val defaultSubPattern = """note""".r
  private[markup] val defaultPrettyOutput = true
}

import com.quui.markup.Markup._
/* The markup backend: transformation of parsed markup structure to XML. */
private[markup] object MarkupBackend {

  /* The simple XML back-end according to the specification: */
  def toXml(elem: Element): Node = elem match {
    case TextElement(text) => Text(text)
    case m@_ => Elem(null, m.tag, Null, xml.TopScope, m.children.map(toXml): _*)
  }

  /* A sample XML back-end that explicitly defines mappings for some elements: */
  def toXmlSample(elem: Element): Node = elem match {
    case TextElement(text) => Text(text)
    case Body(items) => <body>{ items.map(toXmlSample) }</body>
    case Pre(text) => <pre>{ text }</pre>
    case LinkDef(link@_, Url(url)) => <link_def>{ toXmlSample(link) }<url>{ url }</url></link_def>
    // any other cases that should be handled explicitly can go here
    case e@_ => Elem(null, e.tag, Null, xml.TopScope, e.children.map(toXmlSample): _*)
  }
}

import scala.util.parsing.combinator._
/* The markup parser: combines parsers of markup elements, i.e. Parser[Element] */
private[markup] class MarkupParser(sub: Regex = Markup.defaultSubPattern) extends MarkupLexer {

  def parseMarkup(m: String): Body = checked(parseAll(body, Preprocessor.clean(m)))

  def body: Parser[Body] = rep(h | pre | list | blockquote | linkDef | p) ^^ { Body(_) }

  def h: Parser[H] = rep1("*") ~ " " ~ text ^^ { case h ~ _ ~ p => H(h.size, p) }

  /* A verbatim section is indented with 3 spaces and its content is captured as it is: */
  def pre: Parser[Pre] = block(3) ^^ { Pre(_) }

  /* A blockquote section is indented with 2 spaces and its content is parsed like a body: */
  def blockquote: Parser[BlockQuote] = block(2) ^^ {
    case b => BlockQuote(parseInternal(body, b).children)
  }

  def p: Parser[P] = text ^^ { P(_) }

  def text: Parser[List[Element]] =
    rep1(link | linkSimple | para(textChar) | taggedSubdoc | taggedTextual) <~ rep(newLine) ^^ {
      _ map(_ match {
        case t: String => TextElement(t); case sub: Element => sub
      })
    }

  def list: Parser[Element] = block(2) ^^ {
    case s: String if s.startsWith("#") => Ol(parseInternal(ols, s))
    case s: String if s.startsWith("-") => Ul(parseInternal(uls, s))
    case s@_ => BlockQuote(parseInternal(body, s).children)
  }

  def ols: Parser[List[Li]] = rep1("# " ~> li)
  def uls: Parser[List[Li]] = rep1("- " ~> li)

  def li: Parser[Li] = {
    def li(s: String) = Li(parseInternal(body, s).children)
    line(textChar) ~ rep(newLine) ~ opt(block(2)) ^^ {
      case first ~ _ ~ None => li(first)
      case first ~ Nil ~ Some(rest) => li(first + "\n" + rest) // multi-line para
      case first ~ _ ~ Some(rest) => li(first + "\n\n" + rest) // multi-para item
    }
  }

  def link: Parser[Link] = "[" ~> linkLabel ~ "|" ~ tag <~ "]" ^^ { case t ~ _ ~ k => Link(List(t, Key(k))) }
  def linkSimple: Parser[Link] = "[" ~> linkLabel <~ "]" ^^ { case t => Link(List(t)) }
  def linkLabel: Parser[Element] = taggedTextual | rep1("""[^|\]]""".r) ^^ { case c => TextElement(c.mkString) }
  def linkDef: Parser[LinkDef] = linkSimple ~ rep(" ") ~ "<" ~ rep1("""[^>]""".r) <~ ">" <~ rep(newLine) ^^ {
    case link ~ _ ~ _ ~ url => LinkDef(link, Url(url.mkString))
  }

  def taggedTextual: Parser[Tagged] = tagged(tag, text)
  def taggedSubdoc: Parser[Tagged] = tagged(sub, subdoc)
  def subdoc: Parser[List[Element]] = body ^^ { _.children }
  def tagged(tag: Parser[String], content: Parser[List[Element]]) =
    """\""" ~> tag ~ "{" ~ content <~ "}" <~ opt(newLine) ^^ { case tag ~ _ ~ c => Tagged(tag, c) }

  def parseInternal[T](e: Parser[T], s: String): T = checked(super.parseAll(e, s))
  def checked[T](p: ParseResult[T]): T = p match {
    case Success(result, _) => result
    case no@_ => throw new IllegalArgumentException(no.toString)
  }
}

/* The markup lexer: tokenizes text by combining parsers of strings, i.e. Parser[String] */
private[markup] class MarkupLexer extends JavaTokenParsers with RegexParsers {
  override def skipWhitespace = false

  /* Generic block construct: indented with n spaces, parsed into the un-indented text.
   * Using this, we untab sub-blocks used for different elements and parse them again. */
  def block(n: Int) = rep1((repN(n, " ") ~ line(rawChar)) | newLine) ^^ {
    case text => {
      text.map(_ match {
        case _ ~ content => content; case empty if empty != text.last => empty; case _ => ""
      }).mkString("\n").replaceAll(Preprocessor.patterns("trailingSpaces"), "")
    }
  }

  /* Sections of characters, configurable to use a certain class, e.g. rawChar or textChar. */
  def para(c: Parser[String]) = rep1sep(text(c), newLine) <~ opt(newLine) ^^ { _.mkString(" ") }
  def line(c: Parser[String]) = text(c) <~ (newLine | end) ^^ { _.mkString }
  def text(c: Parser[String]) = rep1(c) ^^ { _.mkString }

  /* Actual character definitions: */
  def rawChar = """.""".r
  def textChar = """[^\\{}\[\n]""".r | """\""" ~> escapedChar
  def escapedChar = requiredEscapes | optionalEscapes
  def requiredEscapes = """\""" | "{" | "}" | "["
  def optionalEscapes = "*" | "-" | "#"
  def tag = rep1("""[\d\w-.]""".r) ^^ { _.mkString }
  def newLine = "\u000D\u000A" | "\u000D" | "\u000A"
  def end = """$""".r
}

/* Some preprocessing logic: patterns to remove and removal method, applied before the text is parsed. */
private[markup] object Preprocessor {
  val patterns = Map("modeLines" -> """^-\*-.+\n{1,2}""", "spaceBetweenNewLines" -> """(?m)^\s+$""",
    "leadingBlankLines" -> """^\n+""", "trailingSpaces" -> """\s+$""")
  def clean(s: String) = (s /: patterns)((s: String, t: (String, String)) => t._2.r.replaceAllIn(s, ""))
}
