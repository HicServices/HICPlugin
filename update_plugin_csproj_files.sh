#!/bin/bash
ID=$(cat settings.yml | yq .id)
sed -e "s|<Product>.*</Product>|<Product>$ID</Product>|g" ./Plugin/windows/windows.csproj > ./Plugin/windows/windows.csproj
sed -e "s|<AssemblyTitle>.*</AssemblyTitle>|<AssemblyTitle>$ID</AssemblyTitle>|g" ./Plugin/windows/windows.csproj > ./Plugin/windows/windows.csproj
sed -e "s|<Product>.*</Product>|<Product>$ID</Product>|g" ./Plugin/main/main.csproj > ./Plugin/main/main.csproj
sed -e "s|<AssemblyTitle>.*</AssemblyTitle>|<AssemblyTitle>$ID</AssemblyTitle>|g" ./Plugin/main/main.csproj > ./Plugin/main/main.csproj