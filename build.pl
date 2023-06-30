#!/bin/perl -w

use strict;

my ($rdmpversion,$ownversion);

open my $rdmp, '<', "RDMP/directory.build.props" or die "rdmp:$!\n";
while(<$rdmp>) {
	$rdmpversion=$1 if /version>([^<]+)</i;
}
open my $assembly, '<', "SharedAssemblyInfo.cs" or die "SharedAssemblyInfo.cs:$1\n";
while(<$assembly>) {
	$ownversion=$1 if /AssemblyInformationalVersion\("([^\"]+)"\)/;
}

my $nuspec=<<"EON";
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
    <metadata>
        <id>Rdmp.HIC.Plugin</id>
        <version>$ownversion</version>
        <authors>Health Informatics Service, University of Dundee</authors>
        <description>Imaging plugin for Research Data Management Platform </description>
        <dependencies>
            <dependency id="HIC.RDMP.Plugin" version="$rdmpversion" />
        </dependencies>
    </metadata>
</package>
EON

open(NUSPEC,'>','plugin.nuspec') || die $!;
print NUSPEC $nuspec;
close(NUSPEC);

system("dotnet publish Plugin/windows/windows.csproj -c Release -o p/windows");
system("dotnet publish Plugin/main/main.csproj -c Release -o p/main");
system("7z a -tzip HIC.Plugin.nupkg plugin.nuspec p");
system("dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- pack -p --file HIC.Plugin.nupkg --dir yaml");
system("dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- cmd listsupportedcommands --dir yaml");
