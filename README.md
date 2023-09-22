# HICPlugin

## Building
Before Building, ensure the version numbe ris correct within the hicplugin.nuspec and sharedAssembly.info file
You will also need 7zip or an equivalent installed.

You can build this plugin ready for upload to an RDMP instance using:

```bash
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/windows/windows.csproj -c Release -o p/windows
dotnet publish -p:DebugType=embedded -p:GenerateDocumentation=false Plugin/main/main.csproj -c Release -o p/main
7z a -tzip Rdmp.Hic.Plugin.6.1.0.nupkg hicplugin.nuspec p
dotnet run --project RDMP/Tools/rdmp/rdmp.csproj -c Release -- pack -p --file Rdmp.Hic.Plugin.6.1.0.nupkg --dir yaml
```

Once built you will have a file called `Rdmp.Hic.Plugin.6.1.0.nupkg` 

Upload it to RDMP using

```bash
./rdmp pack -p -f Z:\Repos\HICPlugin\Rdmp.Hic.Plugin.6.1.0.nupkg
```
_Upload into RDMP. Or use the gui client 'Plugins' node under the Tables(Advanced) toolbar button_
