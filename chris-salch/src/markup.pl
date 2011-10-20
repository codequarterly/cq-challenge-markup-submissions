#!/usr/bin/env perl

# Mask any libraries in the current directory
use FindBin;
use lib "$FindBin::Bin/lib";
no lib '.';

# Petup a sane execution execution environment
use strict;
use warnings;
use diagnostics;

use Markup::Parser;
use Markup::Tokenizer;
use Markup::Backend::XML;
use Markup::Util qw/slurp/;


# Make sure that we are actually dealing with uft8
binmode STDIN, ':encoding(utf8)';
binmode STDOUT, ':encoding(utf8)';

=head1 NAME

markup.pl - Tool to convert text written using Markup into a simple 
xml document.

=head1 SYNOPSIS

markup.pl [--no-links] [filename] [-o outputfile]

If no filename is provided, markup.pl will expect to receive input via stdin 
and send output to stdout.

=over

cat test.txt | markup.pl

=back

=cut

# parse arguments into reasonable values
my ($filename, $links, $output)=&parse_args(@ARGV);

my $tokenizer=Markup::Tokenizer->new(links => $links);


my $parser=Markup::Parser->new(tokenizer => $tokenizer);
my $backend=Markup::Backend::XML->new();

# Do we have a file on the command line or should we be 
# looking for a stream?
my $source=$filename ? &slurp($filename) : &slurp(\*STDIN);

# parse the source
my $tree=$parser->parse($source);

my $string=$backend->string($tree);

# write to a given file or STDOUT
if($output) {
    open OUTPUT, '>', $output
	or die "Unable to open output file $@";
    
    binmode OUTPUT, ':encoding(utf8)';
    
    print OUTPUT $string;

    close OUTPUT;
    
} else {
    # use STDOUT
    print $string;
}

=head1 INTERNALS

=head2 parse_args

Convert the arguments array into a set of scalars we can use for 
configuration choices.

=cut
sub parse_args {
    my @args=@_;

    my ($filename, $links, $output)=('', 1, '');
   
    
    # if we have any arguments, parse them
    if(@args) {
	
	# did they ask for no links?
	( $links )=grep { $args[$_] eq '--no-links' } 0..$#args;
	
	# clear the --no-links from the array
	if(defined($links)) {
	    $args[$links]='';
	    $links=0;
	}
	
	
	# look for -o 
	my ( $o_index )=grep { $args[$_] eq '-o' } 0..$#args;

	# did we match?
	if(defined($o_index)) {
	    $output=$args[$o_index+1];
	    
	    # clear -o filename from the array
	    $args[$o_index]='';
	    $args[$o_index+1]='';
	}

	# treat the first true value as an input file name
	($filename)=grep { $_ } @args;

    }
    
    return ($filename, $links, $output);
}


