#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$repo/.macos-dotnet-test-$$"
mkdir -m 700 "$temp"
trap '/bin/rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
system_path="$PATH"
mkdir -p "$HOME/.dotnet" "$temp/bin"
ln -s "$(command -v awk)" "$temp/bin/awk"
ln -s "$(command -v dirname)" "$temp/bin/dirname"
ln -s "$(command -v grep)" "$temp/bin/grep"
ln -s "$(command -v mkdir)" "$temp/bin/mkdir"
ln -s "$(command -v chmod)" "$temp/bin/chmod"
ln -s "$(command -v mv)" "$temp/bin/mv"
ln -s "$(command -v cat)" "$temp/bin/cat"
ln -s "$(command -v rm)" "$temp/bin/rm"

cat > "$temp/bin/dotnet" <<'EOF'
#!/bin/bash
printf '%s\n' "${PATH_SDKS:-10.0.100} [/system/dotnet/sdk]"
EOF
cat > "$HOME/.dotnet/dotnet" <<'EOF'
#!/bin/bash
printf '%s\n' "${HOME_SDKS:-10.0.100} [$HOME/.dotnet/sdk]"
EOF
chmod +x "$temp/bin/dotnet" "$HOME/.dotnet/dotnet"
export PATH="$temp/bin"
source "$repo/scripts/macos/dotnet.sh"

PATH_SDKS=10.0.100 HOME_SDKS=11.0.100
export PATH_SDKS HOME_SDKS
macos_use_dotnet_11
[[ "${DOTNET_ROOT:-}" == "$HOME/.dotnet" ]]
[[ "${PATH%%:*}" == "$HOME/.dotnet" ]]

unset DOTNET_ROOT
PATH_SDKS=11.0.100 HOME_SDKS=10.0.100
export PATH_SDKS HOME_SDKS
macos_use_dotnet_11
[[ -z "${DOTNET_ROOT:-}" ]]
[[ "${PATH%%:*}" == "$temp/bin" ]]

PATH="$system_path"
export PATH
cat > "$temp/bin/uname" <<'EOF'
#!/bin/bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/route" <<'EOF'
#!/bin/bash
printf 'interface: en0\n'
EOF
cat > "$temp/bin/ipconfig" <<'EOF'
#!/bin/bash
printf '192.168.1.23\n'
EOF
cat > "$temp/bin/bash" <<'EOF'
#!/bin/bash
case "${1:-}" in
    */scripts/macos/setup-backup.sh)
        printf 'BACKUP_SETUP=installed\n'
        ;;
    */scripts/macos/setup-services.sh)
        script_dir="${1%/*}"
        PRIVATEKONOMI_CONFIG_ROOT="${PRIVATEKONOMI_CONFIG_ROOT:-}" \
            /bin/bash "$script_dir/configure-production.sh" "${2:-}"
        printf 'SERVICES_SETUP=installed provider=%s\n' "${2:-}"
        ;;
    */scripts/macos/configure-production.sh)
        exec /bin/bash "$@"
        ;;
    *)
        exec /bin/bash "$@"
        ;;
esac
EOF
cat > "$temp/bin/launchctl" <<'EOF'
#!/bin/bash
exit 0
EOF
chmod +x "$temp/bin/uname" "$temp/bin/route" "$temp/bin/ipconfig" "$temp/bin/launchctl"
chmod +x "$temp/bin/bash"

PATH_SDKS=10.0.100 HOME_SDKS=11.0.100
export PATH_SDKS HOME_SDKS
PATH="$temp/bin"
export PATH
output="$(PRIVATEKONOMI_CONFIG_ROOT="$temp/config" PRIVATEKONOMI_STORAGE_PROVIDER=Sqlite /bin/bash "$repo/scripts/macos/start.sh")"
[[ "$output" == *"Web: http://localhost:5274"* ]]
[[ "$output" == *"http://192.168.1.23:5274"* ]]
[[ "$output" == *"BACKUP_SETUP=installed"* ]]
[[ "$output" == *"SERVICES_SETUP=installed provider=Sqlite"* ]]
[[ "$output" == *"startar automatiskt när du loggar in"* ]]
grep -Fq '"Provider": "Sqlite"' "$temp/config/src/Privatekonomi.Web/appsettings.Production.json"
grep -Fq '"Provider": "Sqlite"' "$temp/config/src/Privatekonomi.Api/appsettings.Production.json"
grep -Fq '"Profile": "Sqlite"' "$temp/config/src/Privatekonomi.Api/appsettings.Production.json"
grep -Fq 'privatekonomi.db' "$temp/config/src/Privatekonomi.Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5274"' "$temp/config/src/Privatekonomi.Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5277"' "$temp/config/src/Privatekonomi.Api/appsettings.Production.json"
output="$(PRIVATEKONOMI_CONFIG_ROOT="$temp/config" PRIVATEKONOMI_STORAGE_PROVIDER=JsonFile /bin/bash "$repo/scripts/macos/start.sh" 2>"$temp/json-warning")"
[[ "$output" == *"Lagring: JsonFile"* ]]
[[ "$output" == *"SERVICES_SETUP=installed provider=JsonFile"* ]]
grep -Fq 'API-data delas inte med Web och försvinner vid omstart' "$temp/json-warning"
grep -Fq '"Provider": "JsonFile"' "$temp/config/src/Privatekonomi.Web/appsettings.Production.json"
grep -Fq '"Provider": "InMemory"' "$temp/config/src/Privatekonomi.Api/appsettings.Production.json"
grep -Fq '"Profile": "InMemory"' "$temp/config/src/Privatekonomi.Api/appsettings.Production.json"
grep -Fq 'Application Support/Privatekonomi/json' "$temp/config/src/Privatekonomi.Web/appsettings.Production.json"

source <(/bin/sed -n '/^select_storage_provider() {/,/^}$/p' "$repo/scripts/macos/install.sh")
log_error() { printf '%s\n' "$1" >&2; }
mkdir -p "$HOME/Library/Application Support/Privatekonomi"
printf 'JsonFile\n' > "$HOME/Library/Application Support/Privatekonomi/storage-provider"
[[ "$(select_storage_provider)" == "JsonFile" ]]
[[ "$(PRIVATEKONOMI_STORAGE_PROVIDER=Sqlite select_storage_provider)" == "Sqlite" ]]
printf 'unsupported\n' > "$HOME/Library/Application Support/Privatekonomi/storage-provider"
if select_storage_provider >"$temp/selection-output" 2>&1; then
    echo "Expected unsupported stored provider to fail."
    exit 1
fi

if PRIVATEKONOMI_STORAGE_PROVIDER=PostgreSQL /bin/bash "$repo/scripts/macos/start.sh" >"$temp/postgres-output" 2>&1; then
    echo "Expected unsupported PostgreSQL selection to fail."
    exit 1
fi
grep -Fq 'saknar PostgreSQL EF Core-provider' "$temp/postgres-output"

echo "macOS startup selects .NET 11, configures persistent storage, and starts launchd services."
