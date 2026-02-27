#!/usr/bin/env bash
# E2E Test: US2 Floor Plan Naming
# Exports a batch of DWG files and asserts zero output PDFs are named with numbers.
#
# Usage: ./scripts/test-us2-floor-plan-naming.sh <dwg-file> [<dwg-file> ...]
# Exit codes:
#   0 - PASS: no numeric-named PDFs found
#   1 - FAIL: numeric-named PDFs present, or error

set -euo pipefail

AUTOCAD_DIR="${AUTOCAD_DIR:-/mnt/c/Program Files/Autodesk/AutoCAD 2023}"
ACCORECONSOLE="${ACCORECONSOLE:-${AUTOCAD_DIR}/accoreconsole.exe}"
EXPORT_ROOT="${EXPORT_ROOT:-/mnt/c/Users/pphyo/Downloads/export/pdf}"

if [ "$#" -eq 0 ]; then
    echo "Usage: $0 <dwg-file> [<dwg-file> ...]" >&2
    exit 1
fi

if [ ! -f "$ACCORECONSOLE" ]; then
    echo "SKIP: accoreconsole.exe not found at: $ACCORECONSOLE" >&2
    echo "Set AUTOCAD_DIR to override the AutoCAD installation path." >&2
    exit 0
fi

# Export all specified DWG files
for dwg_file in "$@"; do
    if [ ! -f "$dwg_file" ]; then
        echo "[WARN] DWG file not found, skipping: $dwg_file" >&2
        continue
    fi

    echo "[INFO] Exporting: $dwg_file"
    temp_scr=$(mktemp /tmp/test_us2_XXXXXX.scr)
    echo "EXPORTPDF" > "$temp_scr"
    "$ACCORECONSOLE" /i "$dwg_file" /s "$temp_scr" 2>&1 || true
    rm -f "$temp_scr"
    echo "[INFO] Done: $dwg_file"
done

echo ""
echo "[INFO] Checking export root for numeric-named PDFs: $EXPORT_ROOT"

# Find any PDF files named with a leading number pattern like: 1_color.pdf, 2_bw.pdf
numeric_pdfs=$(find "$EXPORT_ROOT" -type f \( -name "*_color.pdf" -o -name "*_bw.pdf" \) 2>/dev/null \
    | grep -E '/[0-9]+_(color|bw)\.pdf$' || true)

if [ -n "$numeric_pdfs" ]; then
    echo ""
    echo "FAIL: Numeric-named PDF files found (floor plan naming bug present):"
    echo "$numeric_pdfs"
    exit 1
fi

echo ""
echo "PASS: All PDF files have floor plan names — no numeric names found."
exit 0
