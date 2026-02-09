#!/usr/bin/env bash
set -euo pipefail

# detect platform
case "$(uname -s)" in
    Darwin) os="osx" ;;
    Linux)  os="linux" ;;
    *)      echo "  x unsupported OS"; exit 1 ;;
esac

case "$(uname -m)" in
    arm64|aarch64) arch="arm64" ;;
    x86_64)        arch="x64" ;;
    *)             echo "  x unsupported architecture"; exit 1 ;;
esac

asset="mill-$os-$arch"
install_dir="$HOME/.local/bin"
target="$install_dir/mill"

echo "mill installer"
echo ""

# Download binary
echo "  > fetching latest release..."
release_url=$(curl -fsSL https://api.github.com/repos/mindrevolution/mill/releases/latest \
    | grep "browser_download_url.*$asset\"" \
    | cut -d '"' -f 4)

if [[ -z "$release_url" ]]; then
    echo "  x no release found for $asset"
    exit 1
fi

echo "  > downloading $asset..."
mkdir -p "$install_dir"
curl -fsSL "$release_url" -o "$target"
chmod +x "$target"

echo "  + installed to $target"

# Check PATH
echo ""
if ! command -v mill &>/dev/null; then
    echo "  ! ~/.local/bin not in PATH"
    echo "  add to your shell profile:"
    echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
else
    echo "  + mill is in PATH"
fi

echo ""
echo "CLI installed. For Claude Code integration:"
echo "  /plugin marketplace add mindrevolution/claude-plugins"
echo "  /plugin install mill@mindrevolution"
