package Markup::Tree;

use strict;

use fields qw/indent text name body node verbatim inline subdocument/;

use base 'Markup::Base';

=head1 NAME

Markup::Tree - Stores the parse tree 

=head1 SYNOPSIS

Markup::Tree nodes serve a dual purpose as state variables and the basis of 
a simple AST.  When dealing with a Markup::Tree object, outside of the parser,
only the name, verbatim, inline, subdocument and body members have any 
significance. 

=cut

=head1 METHODS

=head2 append_text

Append text to the end of the node we are currently working on.

=cut

sub append_text {
    my ($self, $text)=@_;
    
    if($text ne '') {
	push @{$self->text}, $text;
    }
}

=head2 append_node

Append a new internal node to the body of whatever node we are currently working
on.  If no value is given, it assumes that text contains the value of the
node to be appended.

=cut

sub append_node {
    my ($self, $node)=@_;

    # do we have a simple or complex node
    if($node) {
       	# add a complex node
	push @{$self->body}, $node;

    } else {
	
	# do we have an inline or verbatim node?
	if($self->inline
	    or $self->verbatim) {
	    $self->body = $self->text;

	} else {
	    
	    # only add a new node if there is some content
	    # to put in the node, avoid empty <p/> tags
	    if(@{$self->text}) {
		# add a simple node
		push @{$self->body}, Markup::Tree->new(
		    name => $self->node,
		    body => $self->text);
	    }
	}
    }
    

    # put us back into the default parsing state
    $self->reset(qw/node text/);
}

=head2 default_values

Setup sane defaults.

=cut

sub default_values {

    return {
	indent => 0,
	text => [],
	body => [],
	node => 'p',
	name => 'body',
	verbatim => '',
	subdocument => '',
    };
}

=head1 FIELDS
    
Meanings and uses for some of the public accessible fields.

=head2 name 

Name for the enclosing block defined by this tree node.

=head2 indent

Indicates the current level of indentation in.

=head2 verbatim

If this is set to true it indiciates that the content of this node 
should be treated as pure text only.  (There are no child nodes.)

=head2 inline

If this is set to true, the indicated tag is treated as an inline tag 
in the output.

=head2 subdocument

If set to true while inline is true, this node should be treated as a
subdocument node.

=cut

1;
