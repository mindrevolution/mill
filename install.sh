#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")"

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

rid="$os-$arch"
install_dir="$HOME/.local/bin"
target="$install_dir/mill"

echo "  > building for $rid"
dotnet publish cli/mill-cli.csproj -c Release -r "$rid" -o out --nologo -v q

echo "  > installing to $target"
mkdir -p "$install_dir"
cp out/mill "$target"
chmod +x "$target"

echo "  ✓ installed"

# check if in PATH
if ! command -v mill &>/dev/null; then
    echo ""
    echo "  ! ~/.local/bin not in PATH"
    echo "  add to your shell profile:"
    echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
fi
