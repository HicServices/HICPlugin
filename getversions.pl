#!/usr/bin/perl -w

use strict;

open my $rdmp, '<', "RDMP/directory.build.props" or die "rdmp:$!\n";
while(<$rdmp>) {
	print "rdmpversion=$1\n" if /version>([^<]+)</i;
}
open my $assembly, '<', "SharedAssemblyInfo.cs" or die "SharedAssemblyInfo.cs:$1\n";
while(<$assembly>) {
	print "version=$1\n" if /AssemblyInformationalVersion\("([^\"]+)"\)/;
}