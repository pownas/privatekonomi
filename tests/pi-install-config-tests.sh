#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
temp=$(mktemp -d)
trap 'rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
mkdir -p "$HOME" "$temp/bin"
source <(sed '1s/^\xEF\xBB\xBF//; /^# Main execution$/,$d' "$repo/raspberry-pi-install.sh")

INSTALL_DIR="$HOME/privatekonomi"
DATA_DIR="$HOME/privatekonomi-data"
BACKUP_DIR="$HOME/privatekonomi-backups"
mkdir -p "$INSTALL_DIR/src/Privatekonomi.Web" "$INSTALL_DIR/src/Privatekonomi.Api"

cat > "$temp/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
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
echo "Raspberry Pi production settings are published and refreshed."
