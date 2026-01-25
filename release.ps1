$ErrorActionPreference = "Stop"
Push-Location $PSScriptRoot

try {
    # extract version from csproj
    $csproj = Get-Content "cli/mill-cli.csproj" -Raw
    $version = [regex]::Match($csproj, '<Version>([^<]+)</Version>').Groups[1].Value
    $tag = "v$version"

    Write-Host "  > preparing release $tag"

    # validate clean state
    $status = git status --porcelain
    if ($status) {
        Write-Host "  x working directory not clean" -ForegroundColor Red
        exit 1
    }

    # check if tag exists
    $existing = git rev-parse $tag 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "  x tag $tag already exists" -ForegroundColor Red
        exit 1
    }

    # create and push tag
    git tag $tag
    Write-Host "  ✓ created tag $tag" -ForegroundColor Green

    git push origin $tag
    Write-Host "  ✓ pushed to origin" -ForegroundColor Green

    Write-Host ""
    Write-Host "  release workflow triggered"
    Write-Host "  https://github.com/mindrevolution/mill/actions"
}
finally {
    Pop-Location
}
