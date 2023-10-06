#!/bin/bash
ID=$(cat settings.yml | yq .id)
sed -e "s|<Product>.*</Product>|<Product>$ID</Product>|g" ./Plugin/windows/windows.csproj > _temp.txt
cat _temp.txt > ./Plugin/windows/windows.csproj
sed -e "s|<AssemblyTitle>.*</AssemblyTitle>|<AssemblyTitle>$ID</AssemblyTitle>|g" ./Plugin/windows/windows.csproj > _temp
cat _temp.txt > ./Plugin/windows/windows.csproj
sed -e "s|<Product>.*</Product>|<Product>$ID</Product>|g" ./Plugin/main/main.csproj > _temp.txt
cat _temp.txt > ./Plugin/main/main.csproj
sed -e "s|<AssemblyTitle>.*</AssemblyTitle>|<AssemblyTitle>$ID</AssemblyTitle>|g" ./Plugin/main/main.csproj > _temp.txt
cat _temp.txt > ./Plugin/main/main.csproj
rm _temp.txt