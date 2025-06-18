#!/bin/bash
set -e

# start MinIO (background)
/usr/local/bin/minio server /data --address :9000 --console-address :9001 &
MINIO_PID=$!

# start ASP.NET app
export ASPNETCORE_URLS=http://+:8080
dotnet AccessLensApi.dll &

wait -n
exit $?
