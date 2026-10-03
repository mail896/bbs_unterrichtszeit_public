#!/bin/sh
set -eu
cd "$(dirname "$0")"
dotnet publish Unterrichtszeiten.csproj -c Release -o dist
dotnet publish Unterrichtszeiten.csproj -c Release --self-contained false -p:EnableCompressionInSingleFile=false -o dist-mini
