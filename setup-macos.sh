#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DOC_PATH="docs/scripts/macos/installation.md"
INSTALLER_PATH="scripts/macos/install.sh"

if [[ ! -f "$SCRIPT_DIR/$DOC_PATH" || ! -f "$SCRIPT_DIR/$INSTALLER_PATH" ]]; then
    printf '[ERROR] Hittar inte macOS-dokumentationen eller installeraren. Klona hela repositoryt och kör skriptet från dess rot.\n' >&2
    exit 1
fi
if [[ "$(uname -s)" != "Darwin" ]]; then
    printf '[ERROR] Den här startaren är endast för macOS. Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH" >&2
    exit 1
fi

printf '[INFO] Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH"
printf '[INFO] Installerare: %s\n' "$SCRIPT_DIR/$INSTALLER_PATH"
printf '[INFO] Installationen lägger till .NET/Aspire-verktyg och ett HTTPS-utvecklarcertifikat.\n'

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    printf 'Användning: bash setup-macos.sh\n'
    exit 0
fi

if [[ ! -r /dev/tty ]]; then
    printf '[ERROR] Kör setup-macos.sh i en interaktiv terminal för att godkänna installationen.\n' >&2
    exit 2
fi
printf 'Vill du fortsätta med macOS-installationen? (y/n): '
IFS= read -r answer </dev/tty
case "$answer" in
    [jJ]|[jJ][aA]|[yY]|[yY][eE][sS]) ;;
    *) printf '[INFO] Installationen avbröts.\n'; exit 0 ;;
esac

exec bash "$SCRIPT_DIR/$INSTALLER_PATH" "$@"
