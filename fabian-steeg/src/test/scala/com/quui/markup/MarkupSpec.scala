/**************************************************************************************************
 * Copyright (c) 2010 Fabian Steeg. All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0 which accompanies this
 * distribution, and is available at http://www.eclipse.org/legal/epl-v10.html
 *************************************************************************************************/
package com.quui.markup

import scala.xml._
import scala.util.parsing.combinator.Parsers
import org.scalatest.Spec
import org.scalatest.matchers.ShouldMatchers
import org.junit.runner.RunWith
import org.scalatest.junit.JUnitRunner
import java.io.File
import scala.io.Source._
import com.quui.markup.Markup._

/**
 * Tests for the markup processor, run as ScalaTest or as JUnit test.
 * @author Fabian Steeg (fsteeg)
 */
@RunWith(classOf[JUnitRunner])
class MarkupSpec extends MarkupParser with Spec with ShouldMatchers {
  val pretty = new PrettyPrinter(200, 2)

  describe("The markup processor") {

    it("can parse a markup file passed as a command-line argument and output its XML representation") {
      Markup.main(Array("terms.txt"))
    }

    it("provides API to parse and export markup input") {
      val parsed = Markup.parse(fromFile("terms.txt").mkString, sub = "note|footnote".r)
      val output = Markup.toXml(parsed, pretty = false)
    }

    val body: Body = parseMarkup("""
* the first header

a first paragraph.

  some famous words

** the second header

and more""")

    it("can parse markup input to an internal tree representation") {
      val tree = Body(List(
        H(1, List(TextElement("the first header"))),
        P(List(TextElement("a first paragraph."))),
        BlockQuote(List(
          P(List(TextElement("some famous words"))))),
        H(2, List(TextElement("the second header"))),
        P(List(TextElement("and more")))))
      expect(tree) { body }
    }

    it("can export the parsed structure to an XML representation") {
      val xml = <body>
                  <h1>the first header</h1>
                  <p>a first paragraph.</p>
                  <blockquote>
                    <p>some famous words</p>
                  </blockquote>
                  <h2>the second header</h2>
                  <p>and more</p>
                </body>
      expect(pretty format xml) { pretty format MarkupBackend.toXml(body) }
    }
  }

  describe("The Markup parser") {

    import MarkupBackend._

    it("uses CR (U+000D), CR/LF, (U+000D U+000A), or LF (U+000A) for line termination") {
      expect(classOf[Success[_]]) { parseAll(newLine, "\u000D").getClass }
      expect(classOf[Success[_]]) { parseAll(newLine, "\u000D\u000A").getClass }
      expect(classOf[Success[_]]) { parseAll(newLine, "\u000A").getClass }
    }

    it("parses 'note' tags as sub-documents in its default configuration") {
      expect(<p><note><p>Text</p></note></p>) { toXml(parseAll(p, """\note{Text}""").get) }
      expect(<p><foot>Text</foot></p>) { toXml(parseAll(p, """\foot{Text}""").get) }
    }

    it("can parse custom tags as sub-documents if specified in a pattern") {
      object CustomParser extends MarkupParser(sub = "note|foot".r) {
        expect(<p><note><p>Text</p></note></p>) { toXml(parseAll(p, """\note{Text}""").get) }
        expect(<p><foot><p>Text</p></foot></p>) { toXml(parseAll(p, """\foot{Text}""").get) }
      }
    }

    it("supports simple links") {
      expect(<link>text</link>) { toXml(checked(parseAll(linkSimple, """[text]"""))) }
    }

    it("supports links with keys") {
      expect(<link>text<key>key</key></link>) { toXml(checked(parseAll(link, """[text|key]"""))) }
    }

    it("supports link defs") {
      expect(<link_def><link>text</link><url>http://www.example.com/text/</url></link_def>) {
        toXml(checked(parseAll(linkDef, """[text] <http://www.example.com/text/>""")))
      }
    }

    it("can parse and transform the test files in the tests/ directory") {
      for (
        txt <- new File("tests").listFiles; if txt.getName.endsWith(".txt");
        xml = new File(txt.getAbsolutePath.replace(".txt", ".xml"))
      ) {
        val parsed = parseMarkup(fromFile(txt, "UTF-8").mkString)
        val correct = XML.loadFile(xml)
        print("[Testing] %s ".format(txt.getName))
        expect(pretty format correct) { pretty format MarkupBackend.toXml(parsed) }
        expect(pretty format correct) { pretty format MarkupBackend.toXmlSample(parsed) }
        println("[OK]")
      }
    }

    it("can parse the text files included in the project") {
      for (file <- new File(".").listFiles; if file.getName.endsWith(".txt")) {
        expect(classOf[Body]) { parseMarkup(fromFile(file).mkString).getClass }
      }
    }
  }
}
