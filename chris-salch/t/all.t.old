#!/usr/bin/perl

use strict;
use warnings;
use diagnostics;

no lib '.';
use lib './src/lib'; # make sure to include the 

use Markup::Parser;
use Markup::Tokenizer;
use Markup::Util qw/slurp/;

use Test::Simple tests => 68;

=head1 NAME

test.pl - Test case driver

=head1 SYNOPSIS

=cut

# Get our tests
my @tests=&get_tests('./tests');


# run each test individually
foreach (@tests) {
    
    eval {
	
	# construct a new instance of everything to make sure 
	# various portions of the tests do not interactive with 
	# each other in negative or positive manners
	my $tokenizer=Markup::Tokenizer->new();
	my $parser=Markup::Parser->new(tokenizer => $tokenizer);
	
	my $xml=&slurp("$_.xml");
	my $source=&slurp("$_.txt");
	
	# parse the source tree
	my $tree=$parser->parse($source);
	
	# call our back end handler to convert 
	# to the simple xml format
	my $output=$tree->string('Xml');
	
	# a naive approach to checking output
	ok($output eq $xml, "$_ - Chcking Output");
    };
    
    # if the test case called die, fail it
    ok(!$@, "$_ - Checking for Exception");
    
    # 
    warn $@
	if $@;
}


=head1 INTERNALS


=head2 get_tests

Loads the collection of test case filenames

=cut


sub get_tests {
    my ($path)=@_;

    opendir DIR, $path
	or die "Unable to open test dir: $@";
    
    # all test cases start as txt files
    my @tests=grep {
	m/\.txt$/;
    } readdir DIR;
    
    closedir DIR;

    foreach (@tests) {
	$_=~s/\.txt$//;  # get rid the txt file endings
	$_="$path/$_"; # add in the directory path
    }

    return sort @tests;
}
