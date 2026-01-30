$ErrorActionPreference = "Stop"
Push-Location "$PSScriptRoot/.."

try {
    # detect architecture
    $arch = if ([System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture -eq [System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" } else { "x64" }
    $rid = "win-$arch"

    # build workbench frontend
    Write-Host "  • building workbench"
    Push-Location workbench
    pnpm install --silent
    pnpm build
    Pop-Location

    # build CLI
    Write-Host "  • building cli for $rid"
    dotnet publish cli/mill-cli.csproj -c Release -r $rid -o out --nologo -v q

    # copy workbench to wwwroot
    Write-Host "  • copying wwwroot"
    if (Test-Path out/wwwroot) { Remove-Item -Recurse -Force out/wwwroot }
    Copy-Item -Recurse workbench/dist out/wwwroot

    # delegate to mill install (handles binary, prompts, PATH warnings)
    & ".\out\mill.exe" install
}
finally {
    Pop-Location
}
