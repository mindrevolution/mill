# Set up mill plugin dev environment
#
# Adds a PowerShell alias so you can run:
#   claude-mill          (launches Claude Code with live dev plugin)
#
# The --plugin-dir flag bypasses the plugin cache entirely,
# loading skills directly from your dev repo.

$DevPlugin = "$PSScriptRoot\plugin"

# Add alias to current session
function global:claude-mill {
    claude --plugin-dir $DevPlugin @args
}

Write-Host "Dev alias ready:" -ForegroundColor Green
Write-Host "  claude-mill    — launches Claude Code with live dev plugin from $DevPlugin"
Write-Host ""

# Offer to add to PowerShell profile for persistence
$ProfilePath = $PROFILE.CurrentUserAllHosts
$AliasLine = "function claude-mill { claude --plugin-dir `"$DevPlugin`" @args }"

if (Test-Path $ProfilePath) {
    $ProfileContent = Get-Content $ProfilePath -Raw
    if ($ProfileContent -match "claude-mill") {
        Write-Host "Profile already has claude-mill alias." -ForegroundColor Gray
        return
    }
}

$response = Read-Host "Add to PowerShell profile for persistence? (y/n)"
if ($response -eq "y") {
    Add-Content -Path $ProfilePath -Value "`n# mill dev alias`n$AliasLine"
    Write-Host "Added to $ProfilePath" -ForegroundColor Green
} else {
    Write-Host "Alias active for this session only. Run dev-link.ps1 again next time."
}
