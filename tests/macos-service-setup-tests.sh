#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

system_path="$PATH"
mkdir -p "$temp/home" "$temp/bin"
for command_name in awk basename cat chmod cp dirname git grep ln mkdir mv rm; do
    ln -s "$(command -v "$command_name")" "$temp/bin/$command_name"
done

cat > "$temp/bin/uname" <<'EOF'
#!/bin/bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/dotnet" <<'EOF'
#!/bin/bash
if [[ "${1:-}" == "--list-sdks" ]]; then
    printf '11.0.100 [%s]\n' "$HOME/.dotnet/sdk"
    exit 0
fi
project=""
output=""
while (($#)); do
    case "$1" in
        *.csproj) project="${1##*/}" ;;
        --output) shift; output="$1" ;;
    esac
    shift
done
mkdir -p "$output"
touch "$output/${project%.csproj}.dll"
EOF
cat > "$temp/bin/launchctl" <<'EOF'
#!/bin/bash
printf '%s\n' "$*" >> "$LAUNCHCTL_LOG"
exit 0
EOF
cat > "$temp/bin/plutil" <<'EOF'
#!/bin/bash
[[ "$1" == "-lint" && -f "$2" ]]
EOF
cat > "$temp/bin/curl" <<'EOF'
#!/bin/bash
printf '200'
EOF
cat > "$temp/bin/sleep" <<'EOF'
#!/bin/bash
exit 0
EOF
chmod +x "$temp/bin/uname" "$temp/bin/dotnet" "$temp/bin/launchctl" "$temp/bin/plutil"
chmod +x "$temp/bin/curl" "$temp/bin/sleep"

export HOME="$temp/home"
export PATH="$temp/bin:$system_path"
export LAUNCHCTL_LOG="$temp/launchctl.log"
export PLUTIL_COMMAND="$temp/bin/plutil"
export PRIVATEKONOMI_CONFIG_ROOT="$temp/config"

bash "$repo/scripts/macos/setup-services.sh" Sqlite
support="$HOME/Library/Application Support/Privatekonomi"
[[ -L "$support/current" ]]
[[ -f "$support/current/Web/Privatekonomi.Web.dll" ]]
[[ -f "$support/current/Api/Privatekonomi.Api.dll" ]]
[[ -f "$support/current/Web/appsettings.Production.json" ]]
[[ -x "$support/update-worker.sh" ]]
[[ -f "$support/update/enabled" ]]
[[ ! -e "$support/update/setup-lock" ]]
[[ "$(cat "$support/storage-provider")" == "Sqlite" ]]
grep -Fq 'http://0.0.0.0:5274' "$support/current/Web/appsettings.Production.json"
grep -Fq 'http://0.0.0.0:5277' "$support/current/Api/appsettings.Production.json"
grep -Fq '"Profile": "Sqlite"' "$support/current/Api/appsettings.Production.json"
grep -Fq '"SqliteConnectionString": "Data Source=' "$support/current/Api/appsettings.Production.json"
grep -Fq '<key>KeepAlive</key><true/>' "$HOME/Library/LaunchAgents/com.privatekonomi.web.plist"
grep -Fq '<key>RunAtLoad</key><true/>' "$HOME/Library/LaunchAgents/com.privatekonomi.api.plist"
grep -Fq '<key>StartInterval</key><integer>20</integer>' "$HOME/Library/LaunchAgents/com.privatekonomi.update.plist"
grep -Fq 'PiUpdate__MacOSEnabled' "$HOME/Library/LaunchAgents/com.privatekonomi.web.plist"
grep -Fq 'PRIVATEKONOMI_SOURCE_REPOSITORY' "$HOME/Library/LaunchAgents/com.privatekonomi.update.plist"
grep -Fq 'bootstrap gui/' "$LAUNCHCTL_LOG"
grep -Fq "<string>$temp/bin/dotnet</string>" "$HOME/Library/LaunchAgents/com.privatekonomi.web.plist"

bash "$repo/scripts/macos/setup-services.sh" JsonFile
[[ "$(cat "$support/storage-provider")" == "JsonFile" ]]
grep -Fq '"Provider": "JsonFile"' "$support/current/Web/appsettings.Production.json"
grep -Fq '"Provider": "InMemory"' "$support/current/Api/appsettings.Production.json"
grep -Fq '"Profile": "InMemory"' "$support/current/Api/appsettings.Production.json"
grep -Fq 'Application Support/Privatekonomi/json' "$support/current/Web/appsettings.Production.json"

printf 'fix/old-install\n' > "$support/current/branch"
bash "$repo/scripts/macos/setup-services.sh" JsonFile
[[ "$(cat "$support/current/branch")" == "fix/old-install" ]]
install_commit="$(git -C "$repo" rev-parse HEAD)"
PRIVATEKONOMI_INSTALL_SOURCE_ROOT="$repo" \
    PRIVATEKONOMI_INSTALL_COMMIT="$install_commit" \
    PRIVATEKONOMI_INSTALL_BRANCH=main \
    PRIVATEKONOMI_SOURCE_REPOSITORY="$repo" \
    PRIVATEKONOMI_CONFIG_ROOT="$temp/config" \
    bash "$repo/scripts/macos/setup-services.sh" JsonFile
[[ "$(cat "$support/current/branch")" == "main" ]]
[[ "$(cat "$support/current/commit")" == "$install_commit" ]]
grep -Fq "<string>$repo</string>" "$HOME/Library/LaunchAgents/com.privatekonomi.update.plist"

printf 'macOS service setup publishes services, installs launch agents, and updates provider configuration.\n'
