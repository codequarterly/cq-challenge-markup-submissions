package Markup::Base;

use strict;
use warnings;
use diagnostics;

use fields qw//;

=head1 NAME

Markup::Base - Base class for Markup components.

=head1 SYNOPSIS

This class provides a simple construct which checks/sets a known list of 
arguments.  A subclass may override B<required_args> to change the list of
valid arguments.

=cut

=head1 METHODS

=head2 new

Calling new creates an instance of the derrived class, attempts to verify
that all required parameters have been defined as arguments, and applies
given default values to remaining parameters.  

=cut

sub new {
    my ($self, @args)=@_;
    
    # Setup the fields hash
    $self=fields::new($self)
	unless ref $self;
    
    # Convert our arguments to a hash
    my %args=@args;

    # Map passed in arguments into their appropriate
    # locations
    foreach (keys %args) {
	$self->{$_}=$args{$_};
    }

    # Make sure that we've set all required arguments
    my @required_args=$self->required_args;
    my @not_set;
    foreach (@required_args) {
	push @not_set, $_
	    unless exists $args{$_};
    }

    if(@not_set) {
	die 'Required argument' . (@not_set>1? 's ' : ' ') . join(', ', @not_set) . ' have not been propperly set.';
    }

    # set default values
    my %default_values=%{$self->default_values};
    foreach (keys %default_values) {
	unless(defined($self->{$_})) {
	    $self->{$_}=$default_values{$_};
	}
    }
    
    return $self;
}


=head2 reset
    
Resets the list of field names passed in to their default values

=cut
sub reset {
    my ($self, @fields)=@_;
    
    my %default_values=%{$self->default_values};

    foreach (@fields) {
	$self->{$_}=$default_values{$_};
    }
}

=head2 required_args

Default implementation of required_args which requires nothing.  Sub-Classes should 
implement their own version whih returns an array of argument names.

=cut

sub required_args {
    return ();
}

=head2 default_values

Default implementation of default values sets not defaults.

=cut

sub default_values {
    return {};
}

=head2 AUTOLOAD

Catch undefined method calls to automagically create accessors for our 
defined fields.

=cut

sub AUTOLOAD : lvalue {
    my ($self, @params)=@_;
    our $AUTOLOAD;

    # we don't want to handle destructors
    return if $AUTOLOAD=~m/::DESTROY/;

    # Extract the function name
    my $name=$AUTOLOAD;
    $name=~s/^.*:://;
    
    # turn off strict so we can 
    {
	no strict 'refs';
	*$AUTOLOAD=sub : lvalue {
	    my ($self)=@_;
	    $self->{$name};
	};
    }

    # Call our newly mented routine
    $self->$name(@params);
}


1;
