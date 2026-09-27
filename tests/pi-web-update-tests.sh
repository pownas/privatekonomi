#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
temp=$(mktemp -d)
trap 'rm -rf -- "$temp"' EXIT
mkdir -p "$temp/privatekonomi-update"
touch "$temp/privatekonomi-update/enabled" "$temp/privatekonomi-update/request"

if HOME="$temp" bash "$repo/raspberry-pi-web-update.sh"; then
    echo "Uppdatering utan installerad applikation borde misslyckas." >&2
    exit 1
fi
test "$(cat "$temp/privatekonomi-update/status")" = failed
test ! -e "$temp/privatekonomi-update/request"
grep -q 'misslyckades' "$temp/privatekonomi-update/log"
echo "Felstatus och begäran verifierade."
