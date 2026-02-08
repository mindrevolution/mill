#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."

# detect platform
case "$(uname -s)" in
    Darwin) os="osx" ;;
    Linux)  os="linux" ;;
    *)      echo "  ✕ unsupported OS"; exit 1 ;;
esac

case "$(uname -m)" in
    arm64|aarch64) arch="arm64" ;;
    x86_64)        arch="x64" ;;
    *)             echo "  ✕ unsupported architecture"; exit 1 ;;
esac

rid="$os-$arch"

echo "  • publishing cli for $rid (Release + AOT)"
dotnet publish cli/mill.csproj -c Release -r "$rid" -o out --nologo -v q

# install to user bin
install_dir="$HOME/.local/bin"
mkdir -p "$install_dir"
cp out/mill "$install_dir/mill"
chmod +x "$install_dir/mill"

echo "  ✓ installed to $install_dir/mill"

# check PATH
if ! command -v mill &>/dev/null; then
    echo "  ▲ ~/.local/bin not in PATH"
    echo "    export PATH=\"\$HOME/.local/bin:\$PATH\""
fi
