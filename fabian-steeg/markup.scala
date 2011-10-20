#!/bin/sh
exec scala -classpath markup.jar $0 $@
!#

// We redirect to default processing and output:
com.quui.markup.Markup.main(args)

// We could customize how we process here, e.g.:
/*
import scala.io.Source._
import com.quui.markup.Markup._
println(
  if(args.size != 1) "Pass a single argument: the name of the markup file to process"
  else toXml(parse(fromFile(args(0), "UTF-8").mkString, sub = "note|subnote".r), pretty = false)
)
*/