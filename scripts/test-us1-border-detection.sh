#!/usr/bin/env bash
# E2E Test: US1 Border Detection
# Verifies that DETECTBORDER finds at least one border in a previously-failing DWG.
#
# Usage: ./scripts/test-us1-border-detection.sh <dwg-file>
# Exit codes:
#   0 - PASS: border count >= 1 detected
#   1 - FAIL or error

set -euo pipefail

AUTOCAD_DIR="${AUTOCAD_DIR:-/mnt/c/Program Files/Autodesk/AutoCAD 2023}"
ACCORECONSOLE="${ACCORECONSOLE:-${AUTOCAD_DIR}/accoreconsole.exe}"

if [ "$#" -ne 1 ]; then
    echo "Usage: $0 <dwg-file>" >&2
    exit 1
fi

DWG_FILE="$1"

if [ ! -f "$ACCORECONSOLE" ]; then
    echo "SKIP: accoreconsole.exe not found at: $ACCORECONSOLE" >&2
    echo "Set AUTOCAD_DIR to override the AutoCAD installation path." >&2
    exit 0
fi

if [ ! -f "$DWG_FILE" ]; then
    echo "ERROR: DWG file not found: $DWG_FILE" >&2
    exit 1
fi

temp_scr=$(mktemp /tmp/test_us1_XXXXXX.scr)
echo "DETECTBORDER" > "$temp_scr"

echo "[INFO] Running DETECTBORDER on: $DWG_FILE"
output=$("$ACCORECONSOLE" /i "$DWG_FILE" /s "$temp_scr" 2>&1 || true)
rm -f "$temp_scr"

echo "$output"

if echo "$output" | grep -q "\[INFO\].*border(s) detected"; then
    border_count=$(echo "$output" | grep -oE '[0-9]+ border\(s\) detected' | grep -oE '^[0-9]+' | head -1)
    if [ -n "$border_count" ] && [ "$border_count" -ge 1 ]; then
        echo ""
        echo "PASS: $border_count border(s) detected in model space."
        exit 0
    fi
fi

if echo "$output" | grep -q "\[WARN\] No border detected"; then
    echo ""
    echo "FAIL: No borders detected — border detection bug still present."
    exit 1
fi

echo ""
echo "FAIL: Expected '[INFO] N border(s) detected' in output."
exit 1
