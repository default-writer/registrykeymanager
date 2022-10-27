& Remove-Item -Force -Recurse -Path publish
& dotnet build -o publish
# & nuget push .\publish\RegistryKeyManager.1.0.2022.1027.nupkg -Source https://api.nuget.org/v3/index.json