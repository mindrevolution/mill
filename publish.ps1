$ErrorActionPreference = "Stop"

dotnet publish cli/mill-cli.csproj -c Release -o bin

$binPath = (Resolve-Path "bin").Path
$userPath = [Environment]::GetEnvironmentVariable("Path", "User")

if ($userPath -notlike "*$binPath*") {
    [Environment]::SetEnvironmentVariable("Path", "$userPath;$binPath", "User")
    $env:Path = "$env:Path;$binPath"
    Write-Host "  + added $binPath to PATH"
} else {
    Write-Host "  . $binPath already in PATH"
}
