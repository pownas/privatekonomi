#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
DOC_PATH="docs/scripts/codespaces/README.md"
INSTALLER_PATH="scripts/codespaces/install.sh"

if [[ ! -f "$SCRIPT_DIR/$DOC_PATH" || ! -f "$SCRIPT_DIR/$INSTALLER_PATH" ]]; then
    printf '[ERROR] Hittar inte Codespaces-dokumentationen eller installeraren. Klona hela repositoryt och kör skriptet från dess rot.\n' >&2
    exit 1
fi
if [[ "$(uname -s)" != "Linux" ]]; then
    printf '[ERROR] Den här startaren är avsedd för Linux/Codespaces. Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH" >&2
    exit 1
fi

printf '[INFO] Dokumentation: %s\n' "$SCRIPT_DIR/$DOC_PATH"
printf '[INFO] Installerare: %s\n' "$SCRIPT_DIR/$INSTALLER_PATH"
printf '[INFO] Installationen lägger till SDK/verktyg, återställer paket och bygger projektet.\n'

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    printf 'Användning: bash setup-codespaces.sh\n'
    exit 0
fi

if [[ ! -r /dev/tty ]]; then
    printf '[ERROR] Kör setup-codespaces.sh i en interaktiv terminal för att godkänna installationen.\n' >&2
    exit 2
fi
printf 'Vill du fortsätta med Linux/Codespaces-installationen? (y/n): '
IFS= read -r answer </dev/tty
case "$answer" in
    [jJ]|[jJ][aA]|[yY]|[yY][eE][sS]) ;;
    *) printf '[INFO] Installationen avbröts.\n'; exit 0 ;;
esac

exec bash "$SCRIPT_DIR/$INSTALLER_PATH" "$@"
