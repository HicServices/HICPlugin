#!/usr/bin/perl -w

use strict;

open my $csproj, '<', "DrsPlugin/DrsPlugin.csproj" or die "csproj:$!\n";
while(<$csproj>) {
	print "rdmpversion=$1\n" if /HIC.RDMP.Plugin/ && /version="([^\"]+)"/i;
}
open my $assembly, '<', "SharedAssemblyInfo.cs" or die "SharedAssemblyInfo.cs:$1\n";
while(<$assembly>) {
	print "version=$1\n" if /AssemblyInformationalVersion\("([^\"]+)"\)/;
}