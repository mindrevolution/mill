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

# build workbench frontend
echo "  • building workbench"
(cd workbench && pnpm install --silent && pnpm build)

# build CLI
echo "  • building cli for $rid"
dotnet publish cli/mill-cli.csproj -c Release -r "$rid" -o out --nologo -v q

# copy workbench to wwwroot
echo "  • copying wwwroot"
rm -rf out/wwwroot
cp -r workbench/dist out/wwwroot

# delegate to mill install (handles binary, prompts, PATH warnings)
./out/mill install
