#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
source "$SCRIPT_DIR/dotnet.sh"

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    printf 'Användning: bash scripts/macos/start.sh\n'
    printf 'Startar eller startar om Web och API som inloggningstjänster på macOS.\n'
    exit 0
fi

if [[ "$(uname -s)" != "Darwin" ]]; then
    printf '[ERROR] Det här skriptet är endast avsett för macOS.\n' >&2
    exit 1
fi
if [[ ! -f "$REPO_ROOT/src/Privatekonomi.Web/Privatekonomi.Web.csproj" ]]; then
    printf '[ERROR] Hittar inte Privatekonomi.Web.\n' >&2
    exit 1
fi
if ! macos_use_dotnet_11 || ! command -v launchctl >/dev/null 2>&1; then
    printf '[ERROR] .NET 11 SDK och launchctl krävs. Kör först: bash scripts/macos/install.sh\n' >&2
    exit 1
fi

select_storage_provider() {
    local selection="${PRIVATEKONOMI_STORAGE_PROVIDER:-}"
    local provider_file="$HOME/Library/Application Support/Privatekonomi/storage-provider"

    if [[ -z "$selection" && -f "$provider_file" ]]; then
        IFS= read -r selection < "$provider_file" || true
    fi
    if [[ -z "$selection" && -t 0 ]]; then
        printf 'Välj datalagring:\n'
        printf '  1) SQLite (standard)\n'
        printf '  2) JSON-filer\n'
        printf '  3) PostgreSQL (stöds inte ännu)\n'
        read -r -p 'Val [1]: ' selection || selection=""
        selection="${selection:-1}"
    fi

    case "${selection:-Sqlite}" in
        1|[Ss][Qq][Ll][Ii][Tt][Ee]) PRIVATEKONOMI_STORAGE_PROVIDER="Sqlite" ;;
        2|[Jj][Ss][Oo][Nn][Ff][Ii][Ll][Ee]|[Jj][Ss][Oo][Nn]) PRIVATEKONOMI_STORAGE_PROVIDER="JsonFile" ;;
        3|[Pp][Oo][Ss][Tt][Gg][Rr][Ee][Ss]*|[Pp][Oo][Ss][Tt][Gg][Rr][Ee][Ss][Qq][Ll])
            printf '[ERROR] PostgreSQL kan inte väljas ännu: projektet saknar PostgreSQL EF Core-provider.\n' >&2
            return 1
            ;;
        *)
            printf '[ERROR] Okänt lagringsval: %s. Välj SQLite eller JsonFile.\n' "$selection" >&2
            return 1
            ;;
    esac
}

if ! select_storage_provider; then
    exit 1
fi

cd "$REPO_ROOT"
export PRIVATEKONOMI_STORAGE_PROVIDER
export PRIVATEKONOMI_CONFIG_ROOT="${PRIVATEKONOMI_CONFIG_ROOT:-$REPO_ROOT}"
storage_directory="$HOME/Library/Application Support/Privatekonomi"
mkdir -p "$storage_directory"
case "$PRIVATEKONOMI_STORAGE_PROVIDER" in
    Sqlite) export PRIVATEKONOMI_STORAGE_CONNECTION_STRING="Data Source=$storage_directory/privatekonomi.db" ;;
    JsonFile) export PRIVATEKONOMI_STORAGE_CONNECTION_STRING="$storage_directory/json" ;;
esac
export Storage__Provider="$PRIVATEKONOMI_STORAGE_PROVIDER"
export Storage__ConnectionString="$PRIVATEKONOMI_STORAGE_CONNECTION_STRING"

if ! bash "$SCRIPT_DIR/setup-backup.sh"; then
    printf '[ERROR] Kunde inte installera eller aktivera den dagliga backupen.\n' >&2
    exit 1
fi
if ! PRIVATEKONOMI_CONFIG_ROOT="$PRIVATEKONOMI_CONFIG_ROOT" bash "$SCRIPT_DIR/setup-services.sh" "$PRIVATEKONOMI_STORAGE_PROVIDER"; then
    printf '[ERROR] Web/API-tjänsterna kunde inte startas. Läs loggarna under ~/Library/Logs/Privatekonomi/.\n' >&2
    exit 1
fi

default_interface="$(route -n get default 2>/dev/null | awk '$1 == "interface:" { print $2; exit }' || true)"
lan_ip=""
if [[ -n "$default_interface" ]]; then
    lan_ip="$(ipconfig getifaddr "$default_interface" 2>/dev/null || true)"
fi
printf '[OK] Web och API har startats i bakgrunden och startar automatiskt när du loggar in på datorn.\n'
printf '[INFO] Lagring: %s (%s)\n' "$PRIVATEKONOMI_STORAGE_PROVIDER" "$storage_directory"
printf '[INFO] Web: http://localhost:5274 | API: http://localhost:5277\n'
if [[ -n "$lan_ip" ]]; then
    printf '[INFO] Hemnätverk: http://%s:5274\n' "$lan_ip"
else
    printf '[WARN] Kunde inte hitta datorns LAN-IP. Kontrollera macOS nätverksinställningar.\n'
fi
printf '[INFO] Loggar: %s\n' "$HOME/Library/Logs/Privatekonomi"
