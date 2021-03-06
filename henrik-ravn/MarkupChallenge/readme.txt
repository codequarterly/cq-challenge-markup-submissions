** The Markup Challenge 

*** Introduction
This is my response to the Markup Challenge. 

It is written in F#\note{using Visual Studio 2010, and .Net 4, which is also the 
only pre-requisite for using the project}, which I had wanted to try out on a 
reasonably sized project for some time, and this seemed like a good fit. 

At the time of writing it successfully parses all test files, including the 
instructions and markup specification files. The XML generated by the XML 
backend is equivalent\note{They are not identical because of some leading and 
trailing newlines and whitespace in the test files that have no semantic value 
when interpreting XML.} to the XML files in the tests folder. 

The parser also handles mode-lines and the optional link syntax.

I originally submitted within the original deadline of october 15th (actually 
within minutes of it), but this submission is somewhat cleaner than the original,
plus it has been expanded to include rudimentary error handling, along with error 
test cases.

Overall, I had a good time with the challenge, although it was a bit of an anti-climax
that it seemed to lose steam after the deadline passed. I certainly think that F\# was
a good fit for the job, as the code is fairly clean and quite compact compared to what
it would have been like in C\#, for instance.


Henrik Ravn
Copenhagen, October, December 2010


*** Build instructions 

Load the \file{MarkupChallenge.sln} from the root project folder in Visual 
Studio, build and you're ready to go. 

It is also possible to build with msbuild, just cd to the root directory and 
enter \code{msbuild MarkupChallenge.sln}. This will build a Debug version.

*** Folder structure 

In the root folder you'll find the Visual Studio solution file, and the 
following folders: 

  - \dir{Markup} contains the F# part of the code, organized in a project. 
  The project consists of these files:
    - \file{StringEx.fs} - some string utility functions.
    - \file{Markup.fs} - the Markup data structure in all it's glory. 
    - \file{TextParser.fs} - the parsing code. 
    - \file{XmlBackend.fs} - the XML generator backend.
  
  - \dir{MarkupProcessorFS} contains a small driver program, also written 
  in F#, that takes one or more file patterns on the command line and creates 
  XML files with the processed results. It is also possible to pass in some 
  options to the processor, run the program with an empty command line to 
  see usage info. This project just has one file:
    - \file{Program.fs} - the main program.
	
  - \dir{Specs} - the original challenge material, including the test files. 
  I have added error test cases in the \dir{errtests} sub-folder.
  
  - \dir{TestMarkup} contains a test program that runs through the test files 
  in the \dir{Specs/tests} folder and compares them to the expected XML output. 
  The project consists of two files:
	- \file{Program.fs} - the main test application
    - \file{XmlComparer.fs} - utility functions for comparing two XML trees. 

*** Data structure 

The Markup data structure is extremely simple, and is documented in the file 
\file{Markup.fs}. In the same file there is a simple pretty-print function, that 
shows how to walk the data structure recursively. A further example of how to 
walk the data structure, can be found ion the \file{XmlBackend.fs} file.


