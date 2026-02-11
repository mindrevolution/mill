#!/usr/bin/env bash
# activate-dev-plugin-location.sh — Set up mill plugin dev environment
#
# Source this script to add a shell alias:
#   source ./activate-dev-plugin-location.sh
#
# Then run:
#   claude-mill          (launches Claude Code with live dev plugin)
#
# The --plugin-dir flag bypasses the plugin cache entirely,
# loading skills directly from your dev repo.

DEV_PLUGIN="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)/plugin"

# Add alias to current session
claude-mill() {
    claude --plugin-dir "$DEV_PLUGIN" "$@"
}
export -f claude-mill

echo -e "\033[32mDev alias ready:\033[0m"
echo "  claude-mill    — launches Claude Code with live dev plugin from $DEV_PLUGIN"
echo ""

# Detect shell config file
if [ -n "$ZSH_VERSION" ]; then
    SHELL_RC="$HOME/.zshrc"
else
    SHELL_RC="$HOME/.bashrc"
fi

# Check if already in profile
if grep -q "claude-mill" "$SHELL_RC" 2>/dev/null; then
    echo "Profile already has claude-mill alias."
    return 0 2>/dev/null || exit 0
fi

read -rp "Add to $SHELL_RC for persistence? (y/n) " response
if [ "$response" = "y" ]; then
    printf '\n# mill dev alias\nclaude-mill() { claude --plugin-dir "%s" "$@"; }\nexport -f claude-mill\n' "$DEV_PLUGIN" >> "$SHELL_RC"
    echo -e "\033[32mAdded to $SHELL_RC\033[0m"
else
    echo "Alias active for this session only. Source this script again next time."
fi
