$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot/.."

try {
    # detect architecture
    $arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
    $rid = "win-$arch"

    Write-Host "  • building for $rid"
    dotnet publish cli/mill-cli.csproj -c Release -r $rid -o out --nologo -v q

    # delegate to mill install (handles binary, prompts, PATH warnings)
    & ".\out\mill.exe" install
}
finally {
    Pop-Location
}
