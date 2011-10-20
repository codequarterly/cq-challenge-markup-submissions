import sbt._

class MarkupProject(info: ProjectInfo) extends DefaultProject(info) {
  val junit = "junit" % "junit" % "4.8.1"
  val scalaTest = "org.scalatest" % "scalatest" % "1.2"
}