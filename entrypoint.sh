#!/bin/sh
set -e

/usr/local/bin/minio server /data --address :9000 --console-address :9001 &
dotnet AccessLensApi.dll
