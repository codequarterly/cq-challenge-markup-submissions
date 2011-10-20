package Markup::Util;

use strict;

use base 'Exporter';

=head1 NAME

Markup::Util - A collection of utility methods used throughout the Markup system

=head1 SYNOPSIS

This is a catch all module for functions that are used in several locations.

=cut

# setup a list of functions that may be exported into using modules
our @EXPORT_OK= qw/slurp/;


=head1 FUNCTIONS 

=head2 slurp

Takes in a file handle and slurps its entire contents into a scalar.

=cut

sub slurp {
    my ($file)=@_;

    local $/; # set the end of line marker to undef 

    # If we were passed a file handle, slurp it and return
    # otherwise, we treat our argument as a filename.
    if(ref $file eq 'GLOB') {
	return <$file>;
    }

    open my $fh, '<', $file
	or die "Unable to open file $file due to: $!";
    
    binmode $fh, ":encoding(utf8)"; # set utf8 encoding
    
    # load the entire file into memory
    my $content=<$fh>;

    close $fh; # clean up properly

    return $content;
}


1;
