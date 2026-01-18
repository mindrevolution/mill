#!/usr/bin/env bash
set -euo pipefail

prompt_path="${1:-}"
shift || true
user_prompt="${*:-}"

if [[ -z "$prompt_path" ]]; then
  echo "Usage: _run-prompt.sh <prompt-path> [user prompt]" >&2
  exit 2
fi

if [[ ! -f "$prompt_path" ]]; then
  echo "Prompt not found: $prompt_path" >&2
  exit 2
fi

cli="${MILL_CLI:-claude}"
cli_flags="${MILL_CLI_FLAGS:---dangerously-skip-permissions}"
template="$(cat "$prompt_path")"
rendered="${template//\{\{USER_PROMPT\}\}/$user_prompt}"

tmp="$(mktemp)"
printf "%s" "$rendered" > "$tmp"

cat "$tmp" | $cli $cli_flags
rm -f "$tmp"
