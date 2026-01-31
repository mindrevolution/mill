#!/usr/bin/env bash
set -euo pipefail

# Quick dev iteration for mill workbench.
# Starts API server on port 5218 and frontend dev server with hot reload.
# Press Ctrl+C to stop both servers.

cd "$(dirname "$0")/.."

API_PORT=5218

# Start API server in background
echo "  • starting api (port $API_PORT)"
ASPNETCORE_URLS="http://localhost:$API_PORT" dotnet run --project cli/mill-cli.csproj -- --api-only &
API_PID=$!
trap "kill $API_PID 2>/dev/null" EXIT

# Give API a moment to start
sleep 0.5

# Start frontend dev server (blocks until Ctrl+C)
echo "  • starting frontend"
echo ""
cd workbench
pnpm dev
