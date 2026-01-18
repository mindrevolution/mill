#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
prompt_path="$script_dir/../prompts/feature.md"

user_prompt="${*:-}"
"$script_dir/_run-prompt.sh" "$prompt_path" "$user_prompt"
