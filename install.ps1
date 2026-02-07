$ErrorActionPreference = "Stop"

# detect architecture
$arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
$asset = "mill-win-$arch.exe"
$installDir = "$env:LOCALAPPDATA\Programs\mill"
$target = "$installDir\mill.exe"
$skillsDir = "$env:USERPROFILE\.claude\skills\mill"

Write-Host "■ mill installer" -ForegroundColor Yellow
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

Write-Host "  ✓ installed to $target" -ForegroundColor Green

# Install skills for Claude Code
Write-Host ""
Write-Host "  > installing skills..."
New-Item -ItemType Directory -Force -Path $skillsDir | Out-Null

$skillsUrl = $release.assets | Where-Object { $_.name -eq "skills.zip" } | Select-Object -ExpandProperty browser_download_url

if ($skillsUrl) {
    $tempZip = "$env:TEMP\mill-skills.zip"
    Invoke-WebRequest -Uri $skillsUrl -OutFile $tempZip
    Expand-Archive -Path $tempZip -DestinationPath $skillsDir -Force
    Remove-Item $tempZip
    Write-Host "  ✓ skills installed to $skillsDir" -ForegroundColor Green
} else {
    Write-Host "  ! skills archive not in release, skipping" -ForegroundColor Yellow
    Write-Host "    clone repo and copy skills/ manually"
}

# Check PATH
Write-Host ""
$paths = $env:PATH -split ";"
if ($paths -notcontains $installDir) {
    Write-Host "  ! $installDir not in PATH" -ForegroundColor Yellow
    Write-Host "  run once to add:"
    Write-Host "    [Environment]::SetEnvironmentVariable('PATH', `$env:PATH + ';$installDir', 'User')"
} else {
    Write-Host "  ✓ mill is in PATH" -ForegroundColor Green
}

Write-Host ""
Write-Host "Usage:"
Write-Host "  mill --help           CLI commands"
Write-Host "  /mill:warmup          Generate project context"
Write-Host "  /mill:shape           Draft specifications"
Write-Host "  /mill:ship            Execute work loops"
