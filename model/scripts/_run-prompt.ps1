param(
  [Parameter(Mandatory = $true)]
  [string]$PromptPath,
  [Parameter(Mandatory = $false)]
  [string]$UserPrompt = ""
)

$cli = $env:MILL_CLI
if ([string]::IsNullOrWhiteSpace($cli)) {
  $cli = $env:RALPH_CLI
}
if ([string]::IsNullOrWhiteSpace($cli)) {
  $cli = "claude-code"
}

if (-not (Test-Path -LiteralPath $PromptPath)) {
  throw "Prompt not found: $PromptPath"
}

$template = Get-Content -LiteralPath $PromptPath -Raw
$rendered = $template.Replace("{{USER_PROMPT}}", $UserPrompt)

$tmp = New-TemporaryFile
Set-Content -LiteralPath $tmp -Value $rendered -NoNewline

try {
  Get-Content -LiteralPath $tmp -Raw | & $cli
} finally {
  Remove-Item -LiteralPath $tmp -Force
}
