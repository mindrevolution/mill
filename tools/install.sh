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

echo "  • building for $rid"
dotnet publish cli/mill-cli.csproj -c Release -r "$rid" -o out --nologo -v q

# delegate to mill install (handles binary, prompts, PATH warnings)
./out/mill install
