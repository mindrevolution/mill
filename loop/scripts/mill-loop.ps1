param(
  [Parameter(Mandatory = $true)]
  [string]$PlanPath,
  [Parameter(Mandatory = $false)]
  [int]$MaxIterations = 20,
  [Parameter(Mandatory = $false)]
  [string]$CompletionPromise = "MILL_DONE",
  [Parameter(Mandatory = $false)]
  [int]$SleepSeconds = 0
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$loopDir = Join-Path (Get-Location) ".mill"
New-Item -ItemType Directory -Force -Path $loopDir | Out-Null
$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logPath = Join-Path $loopDir ("mill-loop-$timestamp.log")

for ($i = 1; $i -le $MaxIterations; $i++) {
$outputLines = & (Join-Path $scriptDir "mill-iteration.ps1") `
    -PlanPath $PlanPath `
    -CompletionPromise $CompletionPromise `
    -Iteration $i `
    -MaxIterations $MaxIterations

  $joined = ($outputLines -join "`n")
  $joined | Tee-Object -FilePath $logPath -Append

  if ($joined -match [regex]::Escape($CompletionPromise)) {
    Write-Host $CompletionPromise
    exit 0
  }

  if ($SleepSeconds -gt 0) {
    Start-Sleep -Seconds $SleepSeconds
  }
}

Write-Host "NOT_DONE"
exit 1
