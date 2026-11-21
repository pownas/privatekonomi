#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
temp="$repo/.pi-install-config-test-$$"
mkdir -m 700 "$temp"
trap 'rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
mkdir -p "$HOME" "$temp/bin" "$temp/scripts/raspberry-pi" "$temp/scripts/common"
tr -d '\r' < "$repo/scripts/raspberry-pi/install.sh" | sed '1s/^\xEF\xBB\xBF//; /^# Main execution$/,$d' > "$temp/scripts/raspberry-pi/install.sh"
cp "$repo/scripts/common/run-with-progress.sh" "$temp/scripts/common/"
source "$temp/scripts/raspberry-pi/install.sh"

[ "$DOTNET_CLI_USE_MSBUILD_SERVER" = false ]
[ "$MSBUILDDISABLENODEREUSE" = 1 ]

INSTALL_DIR="$HOME/privatekonomi"
DATA_DIR="$HOME/privatekonomi-data"
BACKUP_DIR="$HOME/privatekonomi-backups"
mkdir -p "$INSTALL_DIR/src/Privatekonomi.Web" "$INSTALL_DIR/src/Privatekonomi.Api"

cat > "$temp/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
if [ "${1:-}" = "build" ]; then
    printf '%s\n' "$@" > "${DOTNET_ARGS_FILE:-/dev/null}"
    printf 'simulated dotnet build diagnostic\n' >&2
    exit "${DOTNET_BUILD_EXIT_CODE:-0}"
fi
while [ "$#" -gt 0 ]; do
    if [ "$1" = "-o" ]; then
        output="$2"
        shift 2
    else
        shift
    fi
done
mkdir -p "$output"
case "$output" in
    */Web) touch "$output/Privatekonomi.Web" ;;
    */Api) touch "$output/Privatekonomi.Api" ;;
esac
EOF
cat > "$temp/bin/git" <<'EOF'
#!/usr/bin/env bash
exit 1
EOF
chmod +x "$temp/bin/"*
export PATH="$temp/bin:$PATH"
SKIP_PUBLISH=false
BUILD_LOG_FILE="$temp/publish-build.log"

printf '1\n' | configure_storage >/dev/null
publish_application >/dev/null

grep -Fq '"Provider": "Sqlite"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5274"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5277"' "$INSTALL_DIR/publish/Api/appsettings.Production.json"

printf 'stale\n' > "$INSTALL_DIR/publish/Web/appsettings.Production.json"
printf 'stale\n' > "$INSTALL_DIR/publish/Api/appsettings.Production.json"
printf 'existing data\n' > "$DATA_DIR/transactions.json"
for project in Web Api; do
    printf '{"Storage": {"Provider": "JsonFile", "ConnectionString": "existing"}}\n' > "$INSTALL_DIR/src/Privatekonomi.$project/appsettings.Production.json"
    cp "$INSTALL_DIR/src/Privatekonomi.$project/appsettings.Production.json" "$temp/$project-source-before"
    cp "$INSTALL_DIR/publish/$project/appsettings.Production.json" "$temp/$project-publish-before"
done
for choice in 2 ''; do
    if printf '%s\n' "$choice" | configure_storage >"$temp/json-output"; then
        echo "Expected unsafe JsonFile selection to fail."
        exit 1
    fi
    grep -Fq 'Befintlig konfiguration och data lämnas oförändrade' "$temp/json-output"
    for project in Web Api; do
        cmp "$temp/$project-source-before" "$INSTALL_DIR/src/Privatekonomi.$project/appsettings.Production.json"
        cmp "$temp/$project-publish-before" "$INSTALL_DIR/publish/$project/appsettings.Production.json"
    done
    [ "$(cat "$DATA_DIR/transactions.json")" = 'existing data' ]
done

if printf 'invalid\n' | configure_storage >"$temp/invalid-output"; then
    echo "Expected invalid storage selection to fail."
    exit 1
fi
grep -Fq 'Ogiltigt lagringsval' "$temp/invalid-output"
for project in Web Api; do
    cmp "$temp/$project-source-before" "$INSTALL_DIR/src/Privatekonomi.$project/appsettings.Production.json"
    cmp "$temp/$project-publish-before" "$INSTALL_DIR/publish/$project/appsettings.Production.json"
done

printf '1\n' | configure_storage >/dev/null
printf '\n' | configure_storage >/dev/null
grep -Fq '"Provider": "Sqlite"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Provider": "Sqlite"' "$INSTALL_DIR/publish/Api/appsettings.Production.json"

INSTALLER_LOG_FILE="$temp/installer.log"
BUILD_LOG_FILE=""
if DOTNET_BUILD_EXIT_CODE=23 DOTNET_ARGS_FILE="$temp/dotnet-args" run_dotnet_build "$INSTALL_DIR/Privatekonomi.slnx" "Testbygge" > "$temp/build-output" 2>&1; then
    echo "Expected dotnet build failure was reported as success."
    exit 1
else
    build_status=$?
fi
[ "$build_status" -eq 23 ]
grep -Fq 'exitkod 23' "$temp/build-output"
grep -Fq 'Full utdata finns i:' "$temp/build-output"
grep -Fq 'simulated dotnet build diagnostic' "$temp/installer-build.log"
grep -Fq -- '--maxcpucount:1' "$temp/dotnet-args"
grep -Fq -- '--no-restore' "$temp/dotnet-args"
grep -Fq -- '-p:UseSharedCompilation=false' "$temp/dotnet-args"

if DOTNET_BUILD_EXIT_CODE=137 run_dotnet_build "$INSTALL_DIR/Privatekonomi.slnx" "OOM-test" > "$temp/oom-output" 2>&1; then
    echo "Expected OOM-like build failure was reported as success."
    exit 1
else
    build_status=$?
fi
[ "$build_status" -eq 137 ]
grep -Fq 'OOM/minnesbrist' "$temp/oom-output"
echo "Raspberry Pi production settings are published and refreshed."
