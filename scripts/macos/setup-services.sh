#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
CONFIG_ROOT="${PRIVATEKONOMI_CONFIG_ROOT:-$REPO_ROOT}"
source "$SCRIPT_DIR/dotnet.sh"
source "$SCRIPT_DIR/../common/run-with-progress.sh"

if [[ "$(uname -s)" != "Darwin" ]]; then
    printf '[ERROR] Tjänsteinstallation är endast tillgänglig på macOS.\n' >&2
    exit 1
fi
if ! macos_use_dotnet_11 || ! command -v launchctl >/dev/null 2>&1; then
    printf '[ERROR] .NET 11 SDK och macOS launchctl krävs. Kör install.sh först.\n' >&2
    exit 1
fi

APP_SUPPORT_DIR="$HOME/Library/Application Support/Privatekonomi"
LOG_DIR="$HOME/Library/Logs/Privatekonomi"
mkdir -p "$LOG_DIR"
BUILD_LOG_FILE="${PRIVATEKONOMI_BUILD_LOG_FILE:-$LOG_DIR/build-$(date '+%Y%m%d-%H%M%S').log}"
touch "$BUILD_LOG_FILE"
AGENT_DIR="$HOME/Library/LaunchAgents"
RELEASE_DIR="$APP_SUPPORT_DIR/releases"
CURRENT_DIR="$APP_SUPPORT_DIR/current"
STATE_DIR="$APP_SUPPORT_DIR/update"
DOTNET_PATH="$(command -v dotnet)"
if [[ "$DOTNET_PATH" != /* ]]; then
    DOTNET_PATH="$(cd -- "$(dirname -- "$DOTNET_PATH")" && pwd -P)/$(basename -- "$DOTNET_PATH")"
fi
PROVIDER="${1:-}"
PLUTIL_COMMAND="${PLUTIL_COMMAND:-/usr/bin/plutil}"
staging=""

if [[ -z "$PROVIDER" && -f "$APP_SUPPORT_DIR/storage-provider" ]]; then
    IFS= read -r PROVIDER < "$APP_SUPPORT_DIR/storage-provider" || true
fi
PROVIDER="${PROVIDER:-Sqlite}"

mkdir -p "$APP_SUPPORT_DIR" "$LOG_DIR" "$AGENT_DIR" "$RELEASE_DIR" "$STATE_DIR"
chmod 700 "$APP_SUPPORT_DIR" "$STATE_DIR"
SETUP_LOCK="$STATE_DIR/setup-lock"
if ! mkdir "$SETUP_LOCK" 2>/dev/null; then
    lock_pid=""
    [[ -f "$SETUP_LOCK/pid" ]] && IFS= read -r lock_pid < "$SETUP_LOCK/pid" || true
    if [[ "$lock_pid" =~ ^[0-9]+$ ]] && kill -0 "$lock_pid" 2>/dev/null; then
        printf '[ERROR] Tjänsteinställning körs redan (PID %s).\n' "$lock_pid" >&2
        exit 1
    fi
    rm -f "$SETUP_LOCK/pid"
    rmdir "$SETUP_LOCK" 2>/dev/null || exit 1
    mkdir "$SETUP_LOCK"
fi
printf '%s\n' "$$" > "$SETUP_LOCK/pid"
cleanup_setup_lock() {
    [[ -z "$staging" ]] || rm -rf -- "$staging"
    rm -f "$SETUP_LOCK/pid"
    rmdir "$SETUP_LOCK" 2>/dev/null || true
}
trap cleanup_setup_lock EXIT
if [[ -f "$STATE_DIR/transaction" ]]; then
    cleanup_setup_lock
    trap - EXIT
    launchctl bootstrap "gui/$(id -u)" "$AGENT_DIR/com.privatekonomi.update.plist" >/dev/null 2>&1 || true
    printf '[ERROR] En tidigare uppdatering behöver återställas. Kontrollera Drift-sidan och försök sedan igen.\n' >&2
    exit 1
fi
if [[ -f "$STATE_DIR/worker-lock/pid" ]]; then
    lock_pid=""
    IFS= read -r lock_pid < "$STATE_DIR/worker-lock/pid" || true
    if [[ "$lock_pid" =~ ^[0-9]+$ ]] && kill -0 "$lock_pid" 2>/dev/null; then
        printf '[ERROR] En uppdatering körs redan (PID %s). Försök igen när den är klar.\n' "$lock_pid" >&2
        exit 1
    fi
    rm -f "$STATE_DIR/worker-lock/pid"
    rmdir "$STATE_DIR/worker-lock" 2>/dev/null || true
fi
PRIVATEKONOMI_CONFIG_ROOT="$CONFIG_ROOT" bash "$SCRIPT_DIR/configure-production.sh" "$PROVIDER"

release_is_complete() {
    [[ -f "$1/Web/Privatekonomi.Web.dll" && -f "$1/Api/Privatekonomi.Api.dll" ]]
}

if ! release_is_complete "$CURRENT_DIR"; then
    commit="$(git -C "$REPO_ROOT" rev-parse HEAD 2>/dev/null || printf 'local')"
    initial_release="$RELEASE_DIR/$commit"
    if ! release_is_complete "$initial_release"; then
        rm -rf -- "$initial_release"
        staging="$(mktemp -d "$RELEASE_DIR/.initial.XXXXXXXX")"
        run_with_progress "Publicerar Privatekonomi Web" "$BUILD_LOG_FILE" \
            dotnet publish "$REPO_ROOT/src/Privatekonomi.Web/Privatekonomi.Web.csproj" \
                --configuration Release --output "$staging/Web" /p:PublishTrimmed=false /p:PublishSingleFile=false
        run_with_progress "Publicerar Privatekonomi API" "$BUILD_LOG_FILE" \
            dotnet publish "$REPO_ROOT/src/Privatekonomi.Api/Privatekonomi.Api.csproj" \
                --configuration Release --output "$staging/Api" /p:PublishTrimmed=false /p:PublishSingleFile=false
        cp "$CONFIG_ROOT/src/Privatekonomi.Web/appsettings.Production.json" "$staging/Web/"
        cp "$CONFIG_ROOT/src/Privatekonomi.Api/appsettings.Production.json" "$staging/Api/"
        printf '%s\n' "$commit" > "$staging/commit"
        printf '%s\n' "$PROVIDER" > "$staging/provider"
        mv "$staging" "$initial_release"
        staging=""
    fi
    ln -sfn "$initial_release" "$CURRENT_DIR"
fi

launchctl bootout "gui/$(id -u)/com.privatekonomi.web" >/dev/null 2>&1 || true
launchctl bootout "gui/$(id -u)/com.privatekonomi.api" >/dev/null 2>&1 || true
cp "$CONFIG_ROOT/src/Privatekonomi.Web/appsettings.Production.json" "$CURRENT_DIR/Web/appsettings.Production.json"
cp "$CONFIG_ROOT/src/Privatekonomi.Api/appsettings.Production.json" "$CURRENT_DIR/Api/appsettings.Production.json"
if [[ ! -s "$CURRENT_DIR/update-worker.sh" ]]; then
    cp "$SCRIPT_DIR/update-worker.sh" "$CURRENT_DIR/update-worker.sh"
fi
printf '%s\n' "$PROVIDER" > "$APP_SUPPORT_DIR/storage-provider"
chmod 600 "$APP_SUPPORT_DIR/storage-provider"

current_commit_file="$CURRENT_DIR/commit"
current_branch_file="$CURRENT_DIR/branch"
if [[ ! -s "$current_commit_file" ]]; then
    git -C "$REPO_ROOT" rev-parse HEAD > "$current_commit_file" 2>/dev/null || printf 'local\n' > "$current_commit_file"
fi
if [[ ! -s "$current_branch_file" ]]; then
    git -C "$REPO_ROOT" branch --show-current > "$current_branch_file" 2>/dev/null || printf 'main\n' > "$current_branch_file"
fi
touch "$STATE_DIR/enabled"
if [[ -f "$CURRENT_DIR/update-worker.sh" ]]; then
    cp "$CURRENT_DIR/update-worker.sh" "$APP_SUPPORT_DIR/update-worker.sh"
else
    cp "$SCRIPT_DIR/update-worker.sh" "$APP_SUPPORT_DIR/update-worker.sh"
fi
chmod 700 "$APP_SUPPORT_DIR/update-worker.sh"

escape_xml() {
    local value="$1"
    value="${value//&/\&amp;}"
    value="${value//</\&lt;}"
    value="${value//>/\&gt;}"
    value="${value//\"/\&quot;}"
    printf '%s' "$value"
}

DOTNET_ROOT_ENTRY=""
if [[ -n "${DOTNET_ROOT:-}" ]]; then
    DOTNET_ROOT_ENTRY="<key>DOTNET_ROOT</key><string>$(escape_xml "$DOTNET_ROOT")</string>"
fi

write_service_agent() {
    local name="$1" port="$2" project="$3"
    local label="com.privatekonomi.$name"
    local plist="$AGENT_DIR/$label.plist"
    local temporary="$plist.tmp.$$"

    cat > "$temporary" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key><string>$label</string>
    <key>ProgramArguments</key>
    <array>
        <string>$(escape_xml "$DOTNET_PATH")</string>
        <string>$(escape_xml "$CURRENT_DIR/$project/Privatekonomi.$project.dll")</string>
    </array>
    <key>WorkingDirectory</key><string>$(escape_xml "$CURRENT_DIR/$project")</string>
    <key>RunAtLoad</key><true/>
    <key>KeepAlive</key><true/>
    <key>ThrottleInterval</key><integer>10</integer>
    <key>StandardOutPath</key><string>$(escape_xml "$LOG_DIR/$name.log")</string>
    <key>StandardErrorPath</key><string>$(escape_xml "$LOG_DIR/$name-error.log")</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>HOME</key><string>$(escape_xml "$HOME")</string>
        <key>PATH</key><string>$(escape_xml "$(dirname "$DOTNET_PATH"):$HOME/.dotnet/tools:/usr/bin:/bin:/usr/sbin:/sbin")</string>
        $DOTNET_ROOT_ENTRY
        <key>ASPNETCORE_ENVIRONMENT</key><string>Production</string>
        <key>DOTNET_ENVIRONMENT</key><string>Production</string>
        <key>PRIVATEKONOMI_ENVIRONMENT</key><string>Production</string>
        <key>PRIVATEKONOMI_LAN_ACCESS</key><string>true</string>
        <key>PORT</key><string>$port</string>
        <key>PiUpdate__Enabled</key><string>true</string>
        <key>PiUpdate__MacOSEnabled</key><string>true</string>
        <key>PiUpdate__StateDirectory</key><string>$(escape_xml "$STATE_DIR")</string>
        <key>PiUpdate__InstalledCommitFile</key><string>$(escape_xml "$current_commit_file")</string>
        <key>PiUpdate__InstalledBranchFile</key><string>$(escape_xml "$current_branch_file")</string>
    </dict>
</dict>
</plist>
EOF
    "$PLUTIL_COMMAND" -lint "$temporary" >/dev/null
    chmod 600 "$temporary"
    mv -f "$temporary" "$plist"
    launchctl bootout "gui/$(id -u)/$label" >/dev/null 2>&1 || true
    launchctl bootstrap "gui/$(id -u)" "$plist"
}

write_worker_agent() {
    local label="com.privatekonomi.update"
    local plist="$AGENT_DIR/$label.plist"
    local temporary="$plist.tmp.$$"
    cat > "$temporary" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key><string>$label</string>
    <key>ProgramArguments</key>
    <array><string>/bin/bash</string><string>$(escape_xml "$APP_SUPPORT_DIR/update-worker.sh")</string></array>
    <key>RunAtLoad</key><true/>
    <key>StartInterval</key><integer>20</integer>
    <key>StandardOutPath</key><string>$(escape_xml "$LOG_DIR/update.log")</string>
    <key>StandardErrorPath</key><string>$(escape_xml "$LOG_DIR/update-error.log")</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>HOME</key><string>$(escape_xml "$HOME")</string>
        <key>PATH</key><string>$(escape_xml "$(dirname "$DOTNET_PATH"):$HOME/.dotnet/tools:/usr/bin:/bin:/usr/sbin:/sbin")</string>
        $DOTNET_ROOT_ENTRY
        <key>PRIVATEKONOMI_UPDATE_STATE</key><string>$(escape_xml "$STATE_DIR")</string>
        <key>PRIVATEKONOMI_APP_SUPPORT</key><string>$(escape_xml "$APP_SUPPORT_DIR")</string>
        <key>PRIVATEKONOMI_SOURCE_REPOSITORY</key><string>$(escape_xml "$REPO_ROOT")</string>
        <key>PRIVATEKONOMI_WEB_PLIST</key><string>$(escape_xml "$AGENT_DIR/com.privatekonomi.web.plist")</string>
        <key>PRIVATEKONOMI_API_PLIST</key><string>$(escape_xml "$AGENT_DIR/com.privatekonomi.api.plist")</string>
    </dict>
</dict>
</plist>
EOF
    "$PLUTIL_COMMAND" -lint "$temporary" >/dev/null
    chmod 600 "$temporary"
    mv -f "$temporary" "$plist"
    launchctl bootout "gui/$(id -u)/$label" >/dev/null 2>&1 || true
    launchctl bootstrap "gui/$(id -u)" "$plist"
}

write_service_agent api 5277 Api
write_service_agent web 5274 Web
write_worker_agent
wait_for_service() {
    local name="$1" url="$2" attempt code
    for ((attempt = 1; attempt <= 30; attempt++)); do
        code="$(curl --silent --max-time 2 --output /dev/null --write-out '%{http_code}' "$url" 2>/dev/null || true)"
        if [[ "$code" == 200 ]] && launchctl print "gui/$(id -u)/com.privatekonomi.$name" >/dev/null 2>&1; then
            return 0
        fi
        sleep 2
    done
    printf '[ERROR] %s svarar inte på %s. Se %s/%s-error.log.\n' "$name" "$url" "$LOG_DIR" "$name" >&2
    return 1
}
if ! wait_for_service api http://127.0.0.1:5277/internal/health ||
   ! wait_for_service web http://127.0.0.1:5274/internal/health; then
    launchctl bootout "gui/$(id -u)/com.privatekonomi.web" >/dev/null 2>&1 || true
    launchctl bootout "gui/$(id -u)/com.privatekonomi.api" >/dev/null 2>&1 || true
    exit 1
fi
printf '[OK] Web och API startar automatiskt vid inloggning och lyssnar på hemnätverket.\n'
printf '[INFO] Publicerad version: %s\n' "$(cat "$current_commit_file")"
