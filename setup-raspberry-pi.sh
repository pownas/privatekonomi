#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DOC_PATH="docs/scripts/raspberry-pi/RASPBERRY_PI_QUICKSTART.md"
SCRIPTS_DIR="$SCRIPT_DIR/scripts/raspberry-pi"
ACTION_HELPER="$SCRIPT_DIR/scripts/common/action-dispatch.sh"

if [[ ! -f "$SCRIPT_DIR/$DOC_PATH" || ! -d "$SCRIPTS_DIR" || ! -f "$ACTION_HELPER" ]]; then
    printf '[ERROR] Hittar inte Raspberry Pi-dokumentationen eller skripten. Klona hela repositoryt och kör skriptet från dess rot.\n' >&2
    exit 1
fi

printf '[INFO] Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH"
printf '[INFO] Skriptkatalog: %s\n' "$SCRIPTS_DIR"

source "$ACTION_HELPER"
if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    show_shell_action_help "$SCRIPTS_DIR" "$(basename "$0")"
    printf 'För installeralternativ: bash setup-raspberry-pi.sh install --help\n'
    exit 0
fi

dispatch_shell_action "$SCRIPTS_DIR" "Raspberry Pi" "$(basename "$0")" "$@"
