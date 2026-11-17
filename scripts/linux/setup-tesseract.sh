#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
TESSDATA_DIR="$REPO_ROOT/src/Privatekonomi.Web/tessdata"
TESSDATA_URL="https://github.com/tesseract-ocr/tessdata_fast/raw/main"

mkdir -p "$TESSDATA_DIR"
tmp_eng="$(mktemp "$TESSDATA_DIR/.eng.XXXXXX")"
tmp_swe="$(mktemp "$TESSDATA_DIR/.swe.XXXXXX")"
trap 'rm -f "$tmp_eng" "$tmp_swe"' EXIT

printf 'Downloading English OCR language data...\n'
curl --fail --location --silent --show-error --output "$tmp_eng" "$TESSDATA_URL/eng.traineddata"
printf 'Downloading Swedish OCR language data...\n'
curl --fail --location --silent --show-error --output "$tmp_swe" "$TESSDATA_URL/swe.traineddata"
test -s "$tmp_eng"
test -s "$tmp_swe"
mv "$tmp_eng" "$TESSDATA_DIR/eng.traineddata"
mv "$tmp_swe" "$TESSDATA_DIR/swe.traineddata"

printf 'Tesseract language data installed in: %s\n' "$TESSDATA_DIR"
