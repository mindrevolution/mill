$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot
try {
    dotnet publish cli/mill-cli.csproj -c Release -o bin
}
finally {
    Pop-Location
}
