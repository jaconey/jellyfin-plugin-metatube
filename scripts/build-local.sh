#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
IMAGE="mcr.microsoft.com/dotnet/sdk:9.0"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required but not found" >&2
  exit 1
fi

cd "$ROOT_DIR"

# Build the plugin using the .NET SDK in Docker.
docker run --rm \
  -v "$ROOT_DIR:/src" \
  -w /src \
  "$IMAGE" \
  dotnet build -c Release

echo "Build complete. DLL output:" 
echo "  $ROOT_DIR/Jellyfin.Plugin.MetaTube/bin/Release/net9.0/MetaTube.dll"
