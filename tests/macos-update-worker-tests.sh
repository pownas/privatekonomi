#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

home="$temp/home"
support="$home/Library/Application Support/Privatekonomi"
state="$support/update"
source_repository="$temp/repository"
fixture_source="$temp/source"
release_id="aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa"
new_release_id="bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"
mkdir -p "$state" "$support/releases/$release_id/Web" "$support/releases/$release_id/Api" \
    "$source_repository/.git" "$fixture_source/scripts/macos" \
    "$fixture_source/src/Privatekonomi.Web" "$fixture_source/src/Privatekonomi.Api" "$temp/bin"
ln -s "$support/releases/$release_id" "$support/current"
touch "$support/releases/$release_id/Web/Privatekonomi.Web.dll"
touch "$support/releases/$release_id/Api/Privatekonomi.Api.dll"
printf '%s\n' "$release_id" > "$support/releases/$release_id/commit"
printf 'main\n' > "$support/releases/$release_id/branch"
cp "$repo/scripts/macos/configure-production.sh" "$fixture_source/scripts/macos/"
cp "$repo/scripts/macos/update-worker.sh" "$fixture_source/scripts/macos/"
touch "$fixture_source/src/Privatekonomi.Web/Privatekonomi.Web.csproj"
touch "$fixture_source/src/Privatekonomi.Api/Privatekonomi.Api.csproj"
touch "$state/enabled" "$state/request"
printf 'main\n' > "$state/request-branch"
printf 'Sqlite\n' > "$support/storage-provider"
cat > "$support/backup.sh" <<'EOF'
#!/bin/bash
touch "$BACKUP_MARKER"
EOF
chmod +x "$support/backup.sh"

cat > "$temp/bin/git" <<'EOF'
#!/bin/bash
if [[ "${1:-}" == "-C" ]]; then shift 2; fi
case "${1:-}" in
    check-ref-format) exit 0 ;;
    fetch) exit 0 ;;
    rev-parse) printf '%s\n' "$UPDATE_COMMIT" ;;
    archive) tar -C "$UPDATE_FIXTURE_SOURCE" -cf - . ;;
    *) echo "Unexpected git command: $*" >&2; exit 1 ;;
esac
EOF
cat > "$temp/bin/dotnet" <<'EOF'
#!/bin/bash
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
exit 0
EOF
cat > "$temp/bin/curl" <<'EOF'
#!/bin/bash
printf '%s' "${HEALTH_CODE:-200}"
EOF
cat > "$temp/bin/sleep" <<'EOF'
#!/bin/bash
exit 0
EOF
chmod +x "$temp/bin/git" "$temp/bin/dotnet" "$temp/bin/launchctl" "$temp/bin/curl" "$temp/bin/sleep"

export HOME="$home"
export PATH="$temp/bin:$PATH"
export PRIVATEKONOMI_UPDATE_STATE="$state"
export PRIVATEKONOMI_APP_SUPPORT="$support"
export PRIVATEKONOMI_SOURCE_REPOSITORY="$source_repository"
export PRIVATEKONOMI_WEB_PLIST="$home/Library/LaunchAgents/com.privatekonomi.web.plist"
export PRIVATEKONOMI_API_PLIST="$home/Library/LaunchAgents/com.privatekonomi.api.plist"
export UPDATE_FIXTURE_SOURCE="$fixture_source"
export UPDATE_COMMIT="$new_release_id"
export BACKUP_MARKER="$temp/backup-ran"

bash "$repo/scripts/macos/update-worker.sh"
[[ "$(cat "$state/status")" == succeeded ]]
[[ -s "$state/log" ]]
if grep -Ev '^\[[0-9]{4}-[0-9]{2}-[0-9]{2} [0-9]{2}:[0-9]{2}:[0-9]{2}[+-][0-9]{4}\] ' "$state/log"; then
    echo "Expected every macOS update log line to have a timestamp." >&2
    exit 1
fi
[[ "$(cat "$support/current/commit")" == "$new_release_id" ]]
[[ "$(cat "$support/current/branch")" == main ]]
[[ -f "$support/current/Web/Privatekonomi.Web.dll" ]]
[[ -f "$support/current/Api/Privatekonomi.Api.dll" ]]
[[ -f "$BACKUP_MARKER" ]]
[[ -f "$support/releases/$release_id/Web/Privatekonomi.Web.dll" ]]
[[ ! -e "$state/transaction" && ! -e "$state/request" ]]

printf 'fix/macos-installer-storage-prompt\n' > "$support/current/branch"
touch "$state/request"
printf 'main\n' > "$state/request-branch"
bash "$repo/scripts/macos/update-worker.sh"
[[ "$(cat "$support/current/branch")" == main ]]
[[ "$(cat "$state/installed-branch")" == main ]]

touch "$state/request" "$state/restart-request"
bash "$repo/scripts/macos/update-worker.sh"
[[ "$(cat "$state/status")" == restarted ]]
[[ ! -e "$state/request" && ! -e "$state/restart-request" ]]

export UPDATE_COMMIT="cccccccccccccccccccccccccccccccccccccccc"
export HEALTH_CODE=500
touch "$state/request"
if bash "$repo/scripts/macos/update-worker.sh"; then
    echo "Expected failed health checks to prevent update success."
    exit 1
fi
[[ "$(cat "$state/status")" == failed ]]
[[ "$(cat "$support/current/commit")" == "$new_release_id" ]]
[[ "$(cat "$support/current/branch")" == main ]]
[[ -f "$state/transaction" && -f "$state/request" ]]

printf 'macOS updater backs up before switching releases, restarts services, and fails closed on unhealthy releases.\n'
