#!/usr/bin/env bash
set -euo pipefail

plan_path="${1:-}"
max_iterations="${2:-20}"
completion_promise="${3:-MILL_DONE}"
sleep_seconds="${4:-0}"

if [[ -z "$plan_path" ]]; then
  echo "Usage: ralph-loop.sh <plan-path> [max-iterations] [completion-promise] [sleep-seconds]" >&2
  exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
loop_dir="$(pwd)/.mill"
mkdir -p "$loop_dir"
timestamp="$(date +"%Y%m%d-%H%M%S")"
log_path="$loop_dir/mill-loop-$timestamp.log"

for ((i=1; i<=max_iterations; i++)); do
  output="$("$script_dir/mill-iteration.sh" "$plan_path" "$completion_promise" "$i" "$max_iterations")"
  printf "%s\n" "$output" | tee -a "$log_path"
  if grep -Fq "$completion_promise" <<<"$output"; then
    echo "$completion_promise"
    exit 0
  fi
  if [[ "$sleep_seconds" -gt 0 ]]; then
    sleep "$sleep_seconds"
  fi
done

echo "NOT_DONE"
exit 1
