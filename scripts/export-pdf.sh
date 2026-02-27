#!/usr/bin/env bash
# Headless batch export of DWG files to PDF via AutoCAD Core Console.
#
# Usage: ./scripts/export-pdf.sh <dwg-file> [<dwg-file> ...]
#
# Environment variables:
#   AUTOCAD_DIR     AutoCAD 2023 installation directory
#                   (default: /mnt/c/Program Files/Autodesk/AutoCAD 2023)
#   ACCORECONSOLE   Full path to accoreconsole.exe
#                   (default: $AUTOCAD_DIR/accoreconsole.exe)
#
# Exit codes:
#   0  All specified DWG files processed
#   1  Setup error (accoreconsole.exe not found, no arguments supplied)

set -euo pipefail

AUTOCAD_DIR="${AUTOCAD_DIR:-/mnt/c/Program Files/Autodesk/AutoCAD 2023}"
ACCORECONSOLE="${ACCORECONSOLE:-${AUTOCAD_DIR}/accoreconsole.exe}"

if [ ! -f "$ACCORECONSOLE" ]; then
    echo "[ERROR] accoreconsole.exe not found at: $ACCORECONSOLE" >&2
    echo "[ERROR] Set the AUTOCAD_DIR environment variable to override." >&2
    exit 1
fi

if [ "$#" -eq 0 ]; then
    echo "[ERROR] No DWG files specified." >&2
    echo "Usage: $0 <dwg-file> [<dwg-file> ...]" >&2
    exit 1
fi

for dwg_file in "$@"; do
    echo "[INFO] Processing: $dwg_file"

    temp_scr=$(mktemp /tmp/acad_export_XXXXXX.scr)
    echo "EXPORTPDF" > "$temp_scr"

    "$ACCORECONSOLE" /i "$dwg_file" /s "$temp_scr" 2>&1 || true

    rm -f "$temp_scr"
    echo "[INFO] Done: $dwg_file"
done

exit 0
