#!/bin/bash
NUSPEC_FILE_NAME=$(cat settings.yml | yq .nuspecFileName)

rm *.nuspec
rm *.nupkg
rm -rf build

./nuspec_generator.sh
./update_plugin_csproj_files.sh

# dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/windows/windows.csproj -c Release -o build/windows
# dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/main/main.csproj -c Release -o build/main
# 7z a -tzip $NUSPEC_FILE_NAME.nupkg $NUSPEC_FILE_NAME.nuspec p
# dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- pack -p --file $NUSPEC_FILE_NAME.nupkg --dir yml
