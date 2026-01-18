#!/usr/bin/env bash
set -euo pipefail
dotnet publish cli/mill-cli.csproj -c Release -o bin
