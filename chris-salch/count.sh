#!/bin/sh
#echo "Cleaning . . ."
#make clean
#echo

echo "Source Statistics:"
wc `find . -regextype posix-basic -iregex '.*\.\(scm\|h\|c\|y\|l\|pl\|pm\)' | grep -v 'test\/test_eyeball\|CMake\|build/\|\.git'`

echo
echo "Commit Count: " `git log | grep '^commit' | wc -l`
echo
