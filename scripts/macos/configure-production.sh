#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
CONFIG_ROOT="${PRIVATEKONOMI_CONFIG_ROOT:-$REPO_ROOT}"
APP_SUPPORT_DIR="$HOME/Library/Application Support/Privatekonomi"
provider="${1:-${PRIVATEKONOMI_STORAGE_PROVIDER:-Sqlite}}"

json_escape() {
    local value="$1"
    value="${value//\\/\\\\}"
    value="${value//\"/\\\"}"
    printf '%s' "$value"
}

mkdir -p "$APP_SUPPORT_DIR" "$CONFIG_ROOT/src/Privatekonomi.Web" "$CONFIG_ROOT/src/Privatekonomi.Api"

case "$provider" in
    Sqlite)
        web_provider="Sqlite"
        api_provider="Sqlite"
        web_connection="$(json_escape "Data Source=$APP_SUPPORT_DIR/privatekonomi.db")"
        api_connection="$web_connection"
        ;;
    JsonFile)
        web_provider="JsonFile"
        api_provider="InMemory"
        web_connection="$(json_escape "$APP_SUPPORT_DIR/json")"
        api_connection=""
        mkdir -p "$APP_SUPPORT_DIR/json"
        ;;
    *)
        printf '[ERROR] Okänd lagringsprovider: %s\n' "$provider" >&2
        exit 1
        ;;
esac

web_config="$CONFIG_ROOT/src/Privatekonomi.Web/appsettings.Production.json"
api_config="$CONFIG_ROOT/src/Privatekonomi.Api/appsettings.Production.json"
web_temp="$web_config.tmp.$$"
api_temp="$api_config.tmp.$$"
trap 'rm -f "$web_temp" "$api_temp"' EXIT

cat > "$web_temp" <<EOF
{
  "Storage": {
    "Provider": "$web_provider",
    "ConnectionString": "$web_connection",
    "SeedTestData": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://0.0.0.0:5274",
  "AllowedHosts": "*"
}
EOF

cat > "$api_temp" <<EOF
{
  "Storage": {
    "Provider": "$api_provider",
    "ConnectionString": "$api_connection",
    "SeedTestData": false
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "Urls": "http://0.0.0.0:5277",
  "AllowedHosts": "*"
}
EOF

chmod 600 "$web_temp" "$api_temp"
mv -f "$web_temp" "$web_config"
mv -f "$api_temp" "$api_config"

printf '[OK] Produktionskonfiguration skapad: %s och %s\n' "$web_config" "$api_config"
printf '[INFO] Lagring: Web=%s, API=%s\n' "$web_provider" "$api_provider"
