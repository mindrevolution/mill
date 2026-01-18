param(
  [Parameter(Mandatory = $true)]
  [string]$PlanPath,
  [Parameter(Mandatory = $false)]
  [string]$CompletionPromise = "MILL_DONE",
  [Parameter(Mandatory = $false)]
  [int]$Iteration = 1,
  [Parameter(Mandatory = $false)]
  [int]$MaxIterations = 20
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$promptPath = Join-Path $scriptDir "..\\prompts\\mill-iteration.md"

$userPrompt = @"
PLAN_PATH: $PlanPath
ITERATION: $Iteration
MAX_ITERATIONS: $MaxIterations
COMPLETION_PROMISE: $CompletionPromise
"@

& (Join-Path $scriptDir "_run-prompt.ps1") -PromptPath $promptPath -UserPrompt $userPrompt
