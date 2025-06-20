#!/bin/sh
set -e

echo "[ENTRYPOINT] Starting AccessLens API"
exec dotnet AccessLensApi.dll
