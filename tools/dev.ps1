<#
.SYNOPSIS
    Quick dev iteration for mill workbench.

.DESCRIPTION
    Builds CLI, starts API server on port 5218, and runs frontend dev server.
    API logs are shown in console. Press Ctrl+C to stop.

.EXAMPLE
    .\tools\dev.ps1
#>

$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot/.."

$apiPort = 5218
$rid = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "win-arm64" } else { "win-x64" }
$exe = "cli/bin/Debug/net10.0/$rid/mill.exe"

try {
    # Build CLI
    Write-Host "  • building cli"
    dotnet build cli/mill-cli.csproj -c Debug --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # Start API server in background (shares console)
    Write-Host "  • starting api (port $apiPort)"
    $env:ASPNETCORE_URLS = "http://localhost:$apiPort"
    $apiProcess = Start-Process -FilePath $exe -ArgumentList "--api-only" -PassThru -NoNewWindow

    # Give API a moment to start
    Start-Sleep -Milliseconds 1500

    # Start frontend dev server (blocks until Ctrl+C)
    Write-Host "  • starting frontend"
    Write-Host ""
    Push-Location workbench
    try {
        pnpm dev
    }
    finally {
        Pop-Location
        Stop-Process -Id $apiProcess.Id -ErrorAction SilentlyContinue
    }
}
finally {
    Pop-Location
}
