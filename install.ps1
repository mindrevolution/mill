$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

try {
    # detect architecture
    $arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
    $rid = "win-$arch"
    $installDir = "$env:LOCALAPPDATA\Programs\mill"
    $target = "$installDir\mill.exe"

    Write-Host "  > building for $rid"
    dotnet publish cli/mill-cli.csproj -c Release -r $rid -o out --nologo -v q

    Write-Host "  > installing to $target"
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Copy-Item "out/mill.exe" $target -Force

    Write-Host "  ✓ installed" -ForegroundColor Green

    # check if in PATH
    $paths = $env:PATH -split ";"
    if ($paths -notcontains $installDir) {
        Write-Host ""
        Write-Host "  ! $installDir not in PATH" -ForegroundColor Yellow
        Write-Host "  run once to add:"
        Write-Host "    [Environment]::SetEnvironmentVariable('PATH', `$env:PATH + ';$installDir', 'User')"
    }
}
finally {
    Pop-Location
}
