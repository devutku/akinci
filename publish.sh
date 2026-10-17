#!/usr/bin/env bash
set -e

echo "========================================================"
echo "  AKINCI - Single-File Publish Script (Linux / macOS)"
echo "========================================================"

RID="${1:-linux-x64}"
OUTPUT_DIR="$(pwd)/dist/$RID"
rm -rf "$OUTPUT_DIR"
mkdir -p "$OUTPUT_DIR"

echo "[1/3] Publishing Akinci.UI ($RID)..."
dotnet publish src/Akinci.UI/Akinci.UI.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true -o "$OUTPUT_DIR/Akinci.UI"

echo "[2/3] Publishing Akinci.Cli ($RID)..."
dotnet publish src/Akinci.Cli/Akinci.Cli.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true -o "$OUTPUT_DIR/Akinci.Cli"

echo "[3/3] Publishing Akinci.NativeHost ($RID)..."
dotnet publish src/Akinci.NativeHost/Akinci.NativeHost.csproj -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true -o "$OUTPUT_DIR/Akinci.NativeHost"

echo "========================================================"
echo "  SUCCESS! Published single-file binaries to $OUTPUT_DIR"
echo "========================================================"
