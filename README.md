# HICPlugin

## Building


```
cd Plugin/windows
dotnet publish --runtime win-x64 -c Release --self-contained false
cd ../main
dotnet publish -c Release --self-contained false
cd ../..
nuget pack ./HIC.Plugin.nuspec -Properties Configuration=Release -IncludeReferencedProjects -Symbols -Version 3.0.1
```
