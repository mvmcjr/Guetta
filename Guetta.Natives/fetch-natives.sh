#!/bin/bash
#
# Downloads libdave native binaries for Windows x64 and Linux x64 from GitHub releases.
# Usage: ./fetch-natives.sh [--force] [--version v1.1.1]
#

set -euo pipefail

FORCE=false
VERSION="v1.1.1"

while [[ $# -gt 0 ]]; do
    case "$1" in
        --force|-f) FORCE=true; shift ;;
        --version|-v) VERSION="$2"; shift 2 ;;
        *) echo "Unknown argument: $1"; exit 1 ;;
    esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BASE_URL="https://github.com/discord/libdave/releases/download/${VERSION}/cpp"

declare -A TARGETS
TARGETS["Windows-X64"]="libdave-Windows-X64-boringssl.zip|${SCRIPT_DIR}/runtimes/win-x64/native"
TARGETS["Linux-X64"]="libdave-Linux-X64-boringssl.zip|${SCRIPT_DIR}/runtimes/linux-x64/native"

for target_name in "${!TARGETS[@]}"; do
    IFS='|' read -r zip_name out_dir <<< "${TARGETS[$target_name]}"
    url="${BASE_URL}/${zip_name}"
    temp_zip="${SCRIPT_DIR}/${zip_name}"
    extract_dir="${SCRIPT_DIR}/temp_${target_name}"

    # Skip if already downloaded (unless --force)
    if [ -d "$out_dir" ] && [ "$FORCE" = false ]; then
        file_count=$(find "$out_dir" -maxdepth 1 -type f 2>/dev/null | wc -l)
        if [ "$file_count" -gt 0 ]; then
            echo "[SKIP] ${target_name} - native binaries already exist. Use --force to re-download."
            continue
        fi
    fi

    echo "[DOWNLOAD] ${target_name} from ${url}..."

    # Download
    curl -L -o "$temp_zip" "$url"

    # Extract to temp directory
    rm -rf "$extract_dir"
    mkdir -p "$extract_dir"
    unzip -o "$temp_zip" -d "$extract_dir"

    # Create output directory
    mkdir -p "$out_dir"

    # Copy native binaries from extracted content
    found=false
    while IFS= read -r -d '' file; do
        cp "$file" "$out_dir/"
        echo "  -> Copied $(basename "$file")"
        found=true
    done < <(find "$extract_dir" -type f \( -name "*.dll" -o -name "*.so" -o -name "*.so.*" -o -name "*.lib" -o -name "*.a" -o -name "*.dylib" \) -print0)

    if [ "$found" = false ]; then
        echo "  [WARN] No native binary files found in archive. Copying all files..."
        while IFS= read -r -d '' file; do
            cp "$file" "$out_dir/"
            echo "  -> Copied $(basename "$file")"
        done < <(find "$extract_dir" -type f -print0)
    fi

    # Cleanup
    rm -f "$temp_zip"
    rm -rf "$extract_dir"

    echo "[DONE] ${target_name} binaries placed in ${out_dir}"
done

echo ""
echo "Fetch complete! Run 'dotnet build' to include the native binaries."
