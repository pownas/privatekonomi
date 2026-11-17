#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"

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
if ! command -v dotnet >/dev/null 2>&1 || ! dotnet --list-sdks | awk '$1 ~ /^11\./ { found=1 } END { exit !found }'; then
    printf '[ERROR] .NET 11 SDK saknas. Kör först: bash scripts/macos/install.sh\n' >&2
    exit 1
fi
if ! command -v aspire >/dev/null 2>&1; then
    export PATH="$HOME/.dotnet/tools:$PATH"
fi
if ! command -v aspire >/dev/null 2>&1; then
    printf '[ERROR] Aspire CLI saknas. Kör först: bash scripts/macos/install.sh\n' >&2
    exit 1
fi

cd "$REPO_ROOT"
export ASPNETCORE_ENVIRONMENT="${ASPNETCORE_ENVIRONMENT:-Development}"
export DOTNET_ENVIRONMENT="${DOTNET_ENVIRONMENT:-Development}"
export PRIVATEKONOMI_ENVIRONMENT="${PRIVATEKONOMI_ENVIRONMENT:-Local}"
export PRIVATEKONOMI_STORAGE_PROVIDER="${PRIVATEKONOMI_STORAGE_PROVIDER:-Sqlite}"
export ASPIRE_DASHBOARD_TELEMETRY_OPTOUT="${ASPIRE_DASHBOARD_TELEMETRY_OPTOUT:-true}"

printf '[INFO] Startar Privatekonomi med Aspire från %s\n' "$REPO_ROOT"
printf '[INFO] ASPNETCORE_ENVIRONMENT=%s, DOTNET_ENVIRONMENT=%s, Storage=%s\n' \
    "$ASPNETCORE_ENVIRONMENT" "$DOTNET_ENVIRONMENT" "$PRIVATEKONOMI_STORAGE_PROVIDER"
printf '[INFO] Appen körs i förgrunden. Avsluta med Ctrl+C.\n'
exec aspire run "$@"
