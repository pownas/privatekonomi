#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DOC_PATH="docs/scripts/raspberry-pi/RASPBERRY_PI_QUICKSTART.md"
INSTALLER_PATH="scripts/raspberry-pi/install.sh"

if [[ ! -f "$SCRIPT_DIR/$DOC_PATH" || ! -f "$SCRIPT_DIR/$INSTALLER_PATH" ]]; then
    printf '[ERROR] Hittar inte Raspberry Pi-dokumentationen eller installeraren. Klona hela repositoryt och kör skriptet från dess rot.\n' >&2
    exit 1
fi

printf '[INFO] Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH"
printf '[INFO] Installerare: %s\n' "$SCRIPT_DIR/$INSTALLER_PATH"
printf '[INFO] Installationen kan ändra systempaket, tjänster och nätverksinställningar.\n'

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    printf 'Användning: bash setup-raspberry-pi.sh [installerarflaggor]\n'
    exec bash "$SCRIPT_DIR/$INSTALLER_PATH" "$@"
fi

if [[ ! -r /dev/tty ]]; then
    printf '[ERROR] Kör setup-raspberry-pi.sh i en interaktiv terminal för att godkänna installationen.\n' >&2
    exit 2
fi
printf 'Vill du fortsätta med Raspberry Pi-installationen? [j/N] '
IFS= read -r answer </dev/tty
case "$answer" in
    [jJ]|[jJ][aA]|[yY]|[yY][eE][sS]) ;;
    *) printf '[INFO] Installationen avbröts.\n'; exit 0 ;;
esac

exec bash "$SCRIPT_DIR/$INSTALLER_PATH" "$@"
