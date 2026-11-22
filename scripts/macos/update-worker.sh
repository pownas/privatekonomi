#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

state="${PRIVATEKONOMI_UPDATE_STATE:?}"
app_support="${PRIVATEKONOMI_APP_SUPPORT:?}"
source_repository="${PRIVATEKONOMI_SOURCE_REPOSITORY:?}"
web_plist="${PRIVATEKONOMI_WEB_PLIST:?}"
api_plist="${PRIVATEKONOMI_API_PLIST:?}"
current="$app_support/current"
releases="$app_support/releases"
worker_lock="$state/worker-lock"
stage=""
restart_requested=0

if [[ -d "$state/setup-lock" ]]; then
    exit 0
fi

write_status() {
    local temporary="$state/status.tmp.$$"
    printf '%s\n' "$1" > "$temporary"
    mv -f "$temporary" "$state/status"
}

release_worker_lock() {
    rm -f "$worker_lock/pid"
    rmdir "$worker_lock" 2>/dev/null || true
}

stop_services() {
    launchctl bootout "gui/$(id -u)/com.privatekonomi.web" >/dev/null 2>&1 || true
    launchctl bootout "gui/$(id -u)/com.privatekonomi.api" >/dev/null 2>&1 || true
}

start_services() {
    stop_services
    launchctl bootstrap "gui/$(id -u)" "$api_plist" || return 1
    launchctl bootstrap "gui/$(id -u)" "$web_plist" || return 1
}

health_check() {
    local name="$1" url="$2" attempt code
    for ((attempt = 1; attempt <= 30; attempt++)); do
        code="$(curl --silent --max-time 2 --output /dev/null --write-out '%{http_code}' "$url" 2>/dev/null || true)"
        if [[ "$code" == 200 ]] && launchctl print "gui/$(id -u)/com.privatekonomi.$name" >/dev/null 2>&1; then
            echo "$name svarar på $url"
            return 0
        fi
        sleep 2
    done
    echo "$name svarar inte på $url" >&2
    return 1
}

recover_transaction() {
    [[ -f "$state/transaction" ]] || return 0
    local old_release
    IFS= read -r old_release < "$state/transaction" || true
    if [[ -z "$old_release" ||
          "$old_release" != "$releases/"* ||
          ! -f "$old_release/Web/Privatekonomi.Web.dll" ||
          ! -f "$old_release/Api/Privatekonomi.Api.dll" ]]; then
        echo "Uppdateringens återställningsjournal är ogiltig; manuell åtgärd krävs." >&2
        return 1
    fi
    echo "Återställer föregående programversion: $old_release"
    stop_services
    ln -sfn "$old_release" "$current" || return 1
    if [[ -f "$old_release/update-worker.sh" ]]; then
        cp "$old_release/update-worker.sh" "$app_support/update-worker.sh"
        chmod 700 "$app_support/update-worker.sh"
    fi
    start_services || return 1
    health_check api http://127.0.0.1:5277/internal/health || return 1
    health_check web http://127.0.0.1:5274/internal/health || return 1
    rm -f "$state/transaction" "$state/backup-ready" || return 1
}

finish() {
    local result=$?
    trap - EXIT
    set +e
    if [[ "$result" -ne 0 ]]; then
        echo "Driftåtgärden misslyckades (kod $result)."
        if [[ -f "$state/transaction" ]]; then
            if ! recover_transaction; then
                echo "Automatisk återställning misslyckades; begäran lämnas spärrad." >&2
                write_status failed
                release_worker_lock
                exit "$result"
            fi
        elif [[ "$restart_requested" -eq 1 ]]; then
            start_services
        fi
        write_status failed
    elif [[ "$restart_requested" -eq 1 ]]; then
        write_status restarted
    else
        write_status succeeded
    fi
    [[ -z "$stage" ]] || rm -rf -- "$stage"
    rm -f "$state/request" "$state/request-branch" "$state/restart-request"
    release_worker_lock
    exit "$result"
}

if ! mkdir "$worker_lock" 2>/dev/null; then
    lock_pid=""
    [[ -f "$worker_lock/pid" ]] && IFS= read -r lock_pid < "$worker_lock/pid" || true
    if [[ "$lock_pid" =~ ^[0-9]+$ ]] && kill -0 "$lock_pid" 2>/dev/null; then
        exit 1
    fi
    rm -f "$worker_lock/pid"
    rmdir "$worker_lock" 2>/dev/null || exit 1
    mkdir "$worker_lock"
fi
printf '%s\n' "$$" > "$worker_lock/pid"
trap finish EXIT
trap 'exit 1' INT TERM HUP
if [[ -d "$state/setup-lock" ]]; then
    trap - EXIT
    release_worker_lock
    exit 0
fi

if [[ -f "$state/transaction" ]]; then
    echo "Avbruten uppdatering upptäckt."
    recover_transaction
    write_status failed
    rm -f "$state/request" "$state/request-branch" "$state/restart-request"
    trap - EXIT
    release_worker_lock
    exit 1
fi

if [[ ! -f "$state/enabled" || ! -f "$state/request" ]]; then
    trap - EXIT
    release_worker_lock
    exit 0
fi
write_status running
: > "$state/log"
exec > >(while IFS= read -r line; do
    printf '[%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S%z')" "$line"
done >> "$state/log") 2>&1

if [[ -f "$state/restart-request" ]]; then
    restart_requested=1
    echo "Startar om Privatekonomi Web och API..."
    stop_services
    start_services
    health_check api http://127.0.0.1:5277/internal/health
    health_check web http://127.0.0.1:5274/internal/health
    echo "Web och API har startats om."
    exit 0
fi

[[ -d "$source_repository/.git" || -f "$source_repository/.git" ]]
[[ -d "$current/Web" && -d "$current/Api" ]]
branch="main"
if [[ -f "$state/request-branch" ]]; then
    IFS= read -r branch < "$state/request-branch" || true
fi
branch="${branch:-main}"
git -C "$source_repository" check-ref-format "refs/heads/$branch"
echo "Hämtar grenen '$branch' från GitHub..."
git -C "$source_repository" fetch --no-tags https://github.com/pownas/Privatekonomi.git "refs/heads/$branch"
commit="$(git -C "$source_repository" rev-parse FETCH_HEAD)"
[[ "$commit" =~ ^[0-9a-f]{40}$ ]]
echo "Målcommit: $commit"

stage="$(mktemp -d "$releases/.staging.XXXXXXXX")"
mkdir -p "$stage/source" "$stage/Web" "$stage/Api"
git -C "$source_repository" archive "$commit" | tar -x -C "$stage/source"
PRIVATEKONOMI_CONFIG_ROOT="$stage/source" \
    bash "$stage/source/scripts/macos/configure-production.sh" \
    "$(cat "$app_support/storage-provider")"
for project in Web Api; do
    echo "Publicerar $project..."
    dotnet publish "$stage/source/src/Privatekonomi.$project/Privatekonomi.$project.csproj" \
        --configuration Release --output "$stage/$project" /p:PublishTrimmed=false /p:PublishSingleFile=false \
        -p:SourceRevisionId="$commit"
    [[ -f "$stage/$project/Privatekonomi.$project.dll" ]]
    cp "$stage/source/src/Privatekonomi.$project/appsettings.Production.json" "$stage/$project/"
done
[[ -f "$stage/source/scripts/macos/update-worker.sh" ]]
cp "$stage/source/scripts/macos/update-worker.sh" "$stage/update-worker.sh"
rm -rf -- "$stage/source"
printf '%s\n' "$commit" > "$stage/commit"
printf '%s\n' "$branch" > "$stage/branch"
release="$releases/$commit"
if [[ -f "$release/Web/Privatekonomi.Web.dll" &&
      -f "$release/Api/Privatekonomi.Api.dll" &&
      -f "$release/update-worker.sh" ]]; then
    rm -rf -- "$stage"
    stage=""
else
    rm -rf -- "$release"
    mv "$stage" "$release"
    stage=""
fi

old_release="$(cd "$current" && pwd -P)"
[[ "$old_release" == "$releases/"* ]]
printf '%s\n' "$old_release" > "$state/transaction"
sync
echo "Stoppar tjänster och verifierar en säkerhetskopia..."
stop_services
bash "$app_support/backup.sh"
touch "$state/backup-ready"
sync
ln -sfn "$release" "$current"
echo "Startar den nya versionen..."
start_services
health_check api http://127.0.0.1:5277/internal/health
health_check web http://127.0.0.1:5274/internal/health
printf '%s\n' "$commit" > "$state/installed-commit"
printf '%s\n' "$branch" > "$state/installed-branch"
cp "$release/update-worker.sh" "$app_support/update-worker.sh"
chmod 700 "$app_support/update-worker.sh"
rm -f "$state/transaction" "$state/backup-ready"
echo "Uppdaterad till $commit på grenen $branch."
