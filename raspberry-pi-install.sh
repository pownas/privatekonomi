#!/usr/bin/env bash
set -Eeuo pipefail

if [[ -n "${BASH_SOURCE[0]:-}" ]]; then
    SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" 2>/dev/null && pwd || true)"
else
    SCRIPT_DIR=""
fi

if [[ -n "$SCRIPT_DIR" && -f "$SCRIPT_DIR/scripts/raspberry-pi/install.sh" ]]; then
    exec bash "$SCRIPT_DIR/scripts/raspberry-pi/install.sh" "$@"
fi

curl --fail --location --silent --show-error https://raw.githubusercontent.com/pownas/Privatekonomi/main/scripts/raspberry-pi/install.sh | bash -s -- "$@"
