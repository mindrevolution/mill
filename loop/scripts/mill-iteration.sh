#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
prompt_path="$script_dir/../prompts/mill-iteration.md"

plan_path="${1:-}"
completion_promise="${2:-MILL_DONE}"
iteration="${3:-1}"
max_iterations="${4:-20}"

if [[ -z "$plan_path" ]]; then
  echo "Usage: implement-iteration.sh <plan-path> [completion-promise] [iteration] [max-iterations]" >&2
  exit 2
fi

user_prompt=$(
  cat <<EOF
PLAN_PATH: $plan_path
ITERATION: $iteration
MAX_ITERATIONS: $max_iterations
COMPLETION_PROMISE: $completion_promise
EOF
)

"$script_dir/_run-prompt.sh" "$prompt_path" "$user_prompt"
