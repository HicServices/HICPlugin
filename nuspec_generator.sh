#!/bin/bash
NUSPEC_FILE_NAME=$(cat settings.yml | yq .nuspecFileName)
ID=$(cat settings.yml | yq .id)
VERSION=$(cat settings.yml | yq .version)
AUTHORS=$(cat settings.yml | yq .authors)
DESCRIPTION=$(cat settings.yml | yq .description)
RDMP_VERSION=$(cat settings.yml | yq .rdmpVersion)

tee ./$NUSPEC_FILE_NAME.nuspec << END
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
    <metadata>
        <id>$ID</id>
        <version>$VERSION</version>
        <authors>$AUTHORS</authors>
        <description>$DESCRIPTION</description>
        <dependencies>
            <dependency id="HIC.RDMP.Plugin" version="$RDMP_VERSION" />
        </dependencies>
    </metadata>
</package>
END