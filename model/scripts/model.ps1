param(
  [Parameter(Mandatory = $false)]
  [string]$UserPrompt = ""
)

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$promptPath = Join-Path $scriptDir "..\\prompts\\model.md"
& (Join-Path $scriptDir "_run-prompt.ps1") -PromptPath $promptPath -UserPrompt $UserPrompt
