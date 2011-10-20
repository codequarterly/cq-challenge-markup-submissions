package Markup::Backend::XML;

use strict;

use base 'Markup::Base';

# list of tags that are considered containers and need to have
# a $/ after the opening tag
my %container = map { $_ => 1 } qw/body blockquote ol ul li note/;

=head1 NAME

Markup::Backend::XML - Produces simple XML output from a Markup::Tree instance

=head1 SYNOPSIS

This module takes in a Markup::Tree instance and returns a string which contains
a simple xml representation of the tree.  Tree nodes are walked in a recursive 
manner with each node representing a separate arbitrary element in the xml
document.

=cut

=head1 METHODS

=head2 string

Convert the passed in Markup::Tree structure into a xml string.

=cut

sub string {
    my ($self, $tree, $indent_val)=@_;
    
    my $name=$tree->name;
    my $string='';

    # make sure that $extra_indent is a number
    $indent_val=defined($indent_val)?$indent_val:0;
    
    # we need to add an extra newline for subdocuments
    if($tree->subdocument) {
	$string.=$/;
	$indent_val-=1; # remove the extraneous indent added 
    }


    # construct an indent block 
    my $indent= ' ' x (4 * $indent_val);

    # don't apply indentation to inline nodes
    unless($tree->inline) {
	$string.=$indent;
    }

    # if we have no internals, start with an empty body
    $string.=((@{$tree->body})?"<$name>":"<$name/>");
    
    # put a $/ after opening container tags
    if($container{$name}) {
	$string.=$/;
    }


    # walk all of the nodes in this nodes body
    foreach (@{$tree->body}) {
	
	if(ref $_) { # a complex tag
	    $string.=$self->string($_, $indent_val+1);

	} else { # we have a tag with inline content
	    $string.=$self->_encode_entities($_);
	}
    }
    
    # did we have an empty body tag?
    if($container{$name}) {
	$string.=$indent;
    }

    #closing tag
    $string.=((@{$tree->body})?"</$name>":'');

    # inline and empty tags to not get a $/ after them
    unless($tree->inline
	or (!@{$tree->body} and $container{$name})) {
	$string.=$/;
    }
    
    # properly indent the next line
    if($tree->subdocument) {
	$string.=$indent;
    }

    return $string;
}


=head2 _encode_entities

Used by Markup::Backend::XML to encode content that contains special
characters

=cut

sub _encode_entities {
    my ($self, $content)=@_;

    $content=~s/&/&amp;/g; # has to be done first
    $content=~s/</&lt;/g;
    $content=~s/>/&gt;/g;
    
    return $content;
}

1;
