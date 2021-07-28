# HICPlugin

## Building

You can build this plugin ready for upload to an RDMP instance using:

```bash
cd Plugin/windows
dotnet publish --runtime win-x64 -c Release --self-contained false
cd ../main
dotnet publish -c Release --self-contained false
cd ../..
nuget pack ./HIC.Plugin.nuspec -Properties Configuration=Release -IncludeReferencedProjects -Symbols -Version 3.0.1
```
_Use the version number in SharedAssembly.info in pace of 3.0.1_

Once built you will have a file called `Rdmp.Hic.Plugin.3.0.1.nupkg` 

Upload it to RDMP using

```bash
./rdmp pack -f Z:\Repos\HICPlugin\Rdmp.Hic.Plugin.3.0.1.nupkg
```
_Upload into RDMP. Or use the gui client 'Plugins' node under the Tables(Advanced) toolbar button_
