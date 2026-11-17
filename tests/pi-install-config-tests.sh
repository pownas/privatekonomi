#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
temp=$(mktemp -d)
trap 'rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
mkdir -p "$HOME" "$temp/bin"
source <(sed '1s/^\xEF\xBB\xBF//; /^# Main execution$/,$d' "$repo/scripts/raspberry-pi/install.sh")

INSTALL_DIR="$HOME/privatekonomi"
DATA_DIR="$HOME/privatekonomi-data"
BACKUP_DIR="$HOME/privatekonomi-backups"
mkdir -p "$INSTALL_DIR/src/Privatekonomi.Web" "$INSTALL_DIR/src/Privatekonomi.Api"

cat > "$temp/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
if [ "${1:-}" = "build" ]; then
    printf '%s\n' "$@" > "${DOTNET_ARGS_FILE:-/dev/null}"
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

printf '1\n' | configure_storage >/dev/null
publish_application >/dev/null

grep -Fq '"Provider": "Sqlite"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5274"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Urls": "http://0.0.0.0:5277"' "$INSTALL_DIR/publish/Api/appsettings.Production.json"

printf 'stale\n' > "$INSTALL_DIR/publish/Web/appsettings.Production.json"
printf 'stale\n' > "$INSTALL_DIR/publish/Api/appsettings.Production.json"
printf '2\n' | configure_storage >/dev/null

grep -Fq '"Provider": "JsonFile"' "$INSTALL_DIR/publish/Web/appsettings.Production.json"
grep -Fq '"Provider": "JsonFile"' "$INSTALL_DIR/publish/Api/appsettings.Production.json"

INSTALLER_LOG_FILE="$temp/installer.log"
if DOTNET_BUILD_EXIT_CODE=23 DOTNET_ARGS_FILE="$temp/dotnet-args" run_dotnet_build "$INSTALL_DIR/Privatekonomi.slnx" "Testbygge" > "$temp/build-output" 2>&1; then
    echo "Expected dotnet build failure was reported as success."
    exit 1
else
    build_status=$?
fi
[ "$build_status" -eq 23 ]
grep -Fq 'exitkod 23' "$temp/build-output"
grep -Fq -- '--maxcpucount' "$temp/dotnet-args"
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
