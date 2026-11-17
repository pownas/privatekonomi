#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
source "$SCRIPT_DIR/dotnet.sh"

if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
    printf 'Användning: bash scripts/macos/start.sh [Aspire CLI-alternativ]\n'
    printf 'Startar Privatekonomi via Aspire i förgrunden. Avsluta med Ctrl+C.\n'
    exit 0
fi

if [[ "$(uname -s)" != "Darwin" ]]; then
    printf '[ERROR] Det här skriptet är endast avsett för macOS.\n' >&2
    exit 1
fi
if [[ ! -f "$REPO_ROOT/src/Privatekonomi.AppHost/Privatekonomi.AppHost.csproj" ]]; then
    printf '[ERROR] Hittar inte Privatekonomi.AppHost.\n' >&2
    exit 1
fi
if ! macos_use_dotnet_11; then
    printf '[ERROR] .NET 11 SDK saknas. Kör först: bash scripts/macos/install.sh\n' >&2
    exit 1
fi
if ! command -v aspire >/dev/null 2>&1; then
    printf '[ERROR] Aspire CLI saknas. Kör först: bash scripts/macos/install.sh\n' >&2
    exit 1
fi

select_storage_provider() {
    local selection="${PRIVATEKONOMI_STORAGE_PROVIDER:-}"

    if [[ -z "$selection" && -t 0 ]]; then
        printf 'Välj datalagring:\n'
        printf '  1) SQLite (standard)\n'
        printf '  2) JSON-filer\n'
        printf '  3) PostgreSQL (stöds inte ännu)\n'
        read -r -p 'Val [1]: ' selection || selection=""
        selection="${selection:-1}"
    fi

    case "${selection:-Sqlite}" in
        1|[Ss][Qq][Ll][Ii][Tt][Ee])
            PRIVATEKONOMI_STORAGE_PROVIDER="Sqlite"
            ;;
        2|[Jj][Ss][Oo][Nn][Ff][Ii][Ll][Ee]|[Jj][Ss][Oo][Nn])
            PRIVATEKONOMI_STORAGE_PROVIDER="JsonFile"
            ;;
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
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export PRIVATEKONOMI_ENVIRONMENT="${PRIVATEKONOMI_ENVIRONMENT:-Local}"
export PRIVATEKONOMI_STORAGE_PROVIDER
export PRIVATEKONOMI_LAN_ACCESS="${PRIVATEKONOMI_LAN_ACCESS:-true}"
export ASPIRE_DASHBOARD_TELEMETRY_OPTOUT="${ASPIRE_DASHBOARD_TELEMETRY_OPTOUT:-true}"

storage_directory="$HOME/Library/Application Support/Privatekonomi"
mkdir -p "$storage_directory"
case "$PRIVATEKONOMI_STORAGE_PROVIDER" in
    Sqlite)
        export PRIVATEKONOMI_STORAGE_CONNECTION_STRING="Data Source=$storage_directory/privatekonomi.db"
        ;;
    JsonFile)
        export PRIVATEKONOMI_STORAGE_CONNECTION_STRING="$storage_directory/json"
        ;;
esac
export Storage__Provider="$PRIVATEKONOMI_STORAGE_PROVIDER"
export Storage__ConnectionString="$PRIVATEKONOMI_STORAGE_CONNECTION_STRING"

default_interface="$(route -n get default 2>/dev/null | awk '$1 == "interface:" { print $2; exit }' || true)"
lan_ip=""
if [[ -n "$default_interface" ]]; then
    lan_ip="$(ipconfig getifaddr "$default_interface" 2>/dev/null || true)"
fi

printf '[INFO] Startar Privatekonomi med Aspire från %s\n' "$REPO_ROOT"
printf '[INFO] ASPNETCORE_ENVIRONMENT=%s, DOTNET_ENVIRONMENT=%s, Storage=%s\n' \
    "$ASPNETCORE_ENVIRONMENT" "$DOTNET_ENVIRONMENT" "$PRIVATEKONOMI_STORAGE_PROVIDER"
printf '[INFO] Data lagras under: %s\n' "$storage_directory"
printf '[INFO] Web: http://localhost:5274 | API: http://localhost:5277\n'
if [[ -n "$lan_ip" ]]; then
    printf '[INFO] Från hemnätverket: http://%s:5274\n' "$lan_ip"
else
    printf '[WARN] Kunde inte hitta datorns LAN-IP. Kör: ipconfig getifaddr %s\n' "${default_interface:-en0}"
fi
printf '[INFO] Appen körs i förgrunden. Avsluta med Ctrl+C.\n'
exec aspire run "$@"
