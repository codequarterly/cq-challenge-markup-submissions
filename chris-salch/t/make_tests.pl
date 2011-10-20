#!/usr/bin/perl

use strict;
use warnings;
use diagnostics;

no lib '.';

=head1 NAME

make_tests.pl - recreate test cases from test files

=head1 SYNOPSIS

=cut

# Get our tests
my @tests=&get_tests('../tests');


# run each test individually
foreach (@tests) {
    
    open FH, '>', "$_.t";

    print FH <<"EOT";

use strict;
use warnings;
use diagnostics;

no lib '.';
use lib './src/lib';

use Markup::Tester;
&run_test('./tests', \$0);

EOT

    close FH;
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
    }

    return sort @tests;
}
