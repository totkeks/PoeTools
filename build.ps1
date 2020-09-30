dotnet publish -c release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:PublishReadyToRun=false -p:PublishTrimmed=true .\src\CLI
