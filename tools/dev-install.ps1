$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot/.."

try {
    # detect architecture
    $arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
    $rid = "win-$arch"

    Write-Host "  • publishing cli for $rid (Release + AOT)"
    dotnet publish cli/mill.csproj -c Release -r $rid -o out --nologo -v q
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

    # install to user programs
    $installDir = "$env:LOCALAPPDATA\Programs\mill"
    New-Item -ItemType Directory -Force -Path $installDir | Out-Null
    Copy-Item -Force "out/mill.exe" "$installDir/mill.exe"

    Write-Host "  ✓ installed to $installDir\mill.exe" -ForegroundColor Green

    # check PATH
    $paths = $env:PATH -split ";"
    if ($paths -notcontains $installDir) {
        Write-Host "  ▲ $installDir not in PATH" -ForegroundColor Yellow
        Write-Host "    [Environment]::SetEnvironmentVariable('PATH', `$env:PATH + ';$installDir', 'User')"
    }
}
finally {
    Pop-Location
}
