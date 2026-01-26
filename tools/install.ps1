$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot/.."

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

    # add to PATH if not present
    $userPath = [Environment]::GetEnvironmentVariable('PATH', 'User')
    if ($userPath -notlike "*$installDir*") {
        [Environment]::SetEnvironmentVariable('PATH', "$userPath;$installDir", 'User')
        $env:PATH = "$env:PATH;$installDir"
        Write-Host "  ✓ added to PATH" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
