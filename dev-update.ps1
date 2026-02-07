$ErrorActionPreference = "Stop"

$root = $PSScriptRoot
$publishDir = "$root\cli\bin\Release\net10.0\win-x64\publish"
$skillsSource = "$root\skills"
$commandsDest = "$env:USERPROFILE\.claude\commands\mill"

Write-Host "mill dev-update" -ForegroundColor Yellow
Write-Host ""

# Build and publish
Write-Host "  > building..."
Push-Location "$root\cli"
dotnet publish -c Release -r win-x64 --nologo -v q
if ($LASTEXITCODE -ne 0) { Pop-Location; exit 1 }
Pop-Location
Write-Host "  + built" -ForegroundColor Green

# Check PATH
$paths = $env:PATH -split ";"
if ($paths -notcontains $publishDir) {
    Write-Host "  > adding to PATH..."
    $env:PATH = "$publishDir;$env:PATH"
    [Environment]::SetEnvironmentVariable("PATH", "$publishDir;" + [Environment]::GetEnvironmentVariable("PATH", "User"), "User")
    Write-Host "  + added to PATH" -ForegroundColor Green
} else {
    Write-Host "  + PATH ok" -ForegroundColor Green
}

# Copy commands
Write-Host "  > installing commands..."
if (Test-Path $commandsDest) {
    Remove-Item -Recurse -Force $commandsDest
}
New-Item -ItemType Directory -Force -Path $commandsDest | Out-Null
Copy-Item "$skillsSource\*.md" $commandsDest
Write-Host "  + commands installed" -ForegroundColor Green

Write-Host ""
Write-Host "Ready:" -ForegroundColor Green
Write-Host "  mill --help"
Write-Host "  /mill:warmup"
