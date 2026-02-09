$ErrorActionPreference = "Stop"

# detect architecture
$arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
$asset = "mill-win-$arch.exe"
$installDir = "$env:LOCALAPPDATA\Programs\mill"
$target = "$installDir\mill.exe"

Write-Host "mill installer" -ForegroundColor Yellow
Write-Host ""

# Download binary
Write-Host "  > fetching latest release..."
$release = Invoke-RestMethod -Uri "https://api.github.com/repos/mindrevolution/mill/releases/latest"
$url = $release.assets | Where-Object { $_.name -eq $asset } | Select-Object -ExpandProperty browser_download_url

if (-not $url) {
    Write-Host "  x no release found for $asset" -ForegroundColor Red
    exit 1
}

Write-Host "  > downloading $asset..."
New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Invoke-WebRequest -Uri $url -OutFile $target

Write-Host "  + installed to $target" -ForegroundColor Green

# Check PATH
Write-Host ""
$paths = $env:PATH -split ";"
if ($paths -notcontains $installDir) {
    Write-Host "  ! $installDir not in PATH" -ForegroundColor Yellow
    Write-Host "  run once to add:"
    Write-Host "    [Environment]::SetEnvironmentVariable('PATH', `$env:PATH + ';$installDir', 'User')"
} else {
    Write-Host "  + mill is in PATH" -ForegroundColor Green
}

Write-Host ""
Write-Host "CLI installed. For Claude Code integration:" -ForegroundColor Green
Write-Host "  /plugin marketplace add mindrevolution/claude-plugins"
Write-Host "  /plugin install mill@mindrevolution"
