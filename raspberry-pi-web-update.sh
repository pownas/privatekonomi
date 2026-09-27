#!/usr/bin/env bash
# Installed outside the checkout by raspberry-pi-enable-web-update.sh.
set -Eeuo pipefail
umask 077

state="$HOME/privatekonomi-update"
install_dir="$HOME/privatekonomi"
publish="$install_dir/publish"
stage=""

start_services() {
    sudo -n /usr/bin/systemctl start privatekonomi-api.service || return 1
    sudo -n /usr/bin/systemctl start privatekonomi-web.service || return 1
}

# The transaction file is written before stopping the services. The ready marker
# is written only after both backups are complete; recovery never uses a partial backup.
recover() {
    if [ ! -f "$state/transaction" ]; then
        return 0
    fi
    local backup
    backup=$(cat "$state/transaction")
    echo "Återställer avbruten uppdatering från $backup"
    if [ -f "$state/backup-ready" ]; then
        test -d "$backup/Web" && test -d "$backup/Api" || return 1
        for project in Web Api; do
            rm -rf -- "${publish:?}/$project" || return 1
            cp -a -- "$backup/$project" "$publish/$project" || return 1
        done
    fi
    start_services || return 1
    /usr/bin/systemctl is-active --quiet privatekonomi-api.service || return 1
    /usr/bin/systemctl is-active --quiet privatekonomi-web.service || return 1
    rm -f -- "$state/transaction" "$state/backup-ready" || return 1
}

health_check() {
    local name="$1" url="$2" attempt
    for ((attempt = 1; attempt <= 30; attempt++)); do
        if [ "$(curl --silent --show-error --max-time 2 --output /dev/null --write-out '%{http_code}' "$url")" = 200 ] &&
           /usr/bin/systemctl is-active --quiet "privatekonomi-$name.service"; then
            echo "$name svarar på $url"
            return 0
        fi
        sleep 2
    done
    echo "$name svarar inte på $url" >&2
    return 1
}

finish() {
    local result=$?
    trap - EXIT
    set +e
    if [ "$result" -ne 0 ]; then
        echo "Uppdateringen misslyckades (kod $result)."
        if ! recover; then
            echo "Automatisk återställning misslyckades. Begäran lämnas spärrad; följ återställningsguiden." >&2
            printf 'failed\n' > "$state/status"
            exit "$result"
        fi
        printf 'failed\n' > "$state/status"
    else
        printf 'succeeded\n' > "$state/status"
    fi
    [ -z "$stage" ] || rm -rf -- "$stage"
    rm -f -- "$state/request"
    exit "$result"
}

exec 9>"$state/lock"
flock -n 9 || exit 1
trap finish EXIT
trap 'exit 1' INT TERM HUP

# A reboot or SIGKILL can leave both request and transaction behind.
if [ -f "$state/transaction" ]; then
    exec >> "$state/log" 2>&1
    echo "Avbruten uppdatering upptäckt."
    if recover; then
        printf 'failed\n' > "$state/status"
        rm -f -- "$state/request"
        [ -z "$stage" ] || rm -rf -- "$stage"
    else
        echo "Återställningen misslyckades; manuell åtgärd krävs." >&2
        printf 'failed\n' > "$state/status"
    fi
    trap - EXIT
    exit 1
fi

rm -f -- "$state/backup-ready"
printf 'running\n' > "$state/status"
: > "$state/log"
exec >> "$state/log" 2>&1

echo "Hämtar godkänd main från GitHub..."
test -f "$state/enabled"
test -d "$install_dir/.git"
test -f "$publish/Web/Privatekonomi.Web"
test -f "$publish/Api/Privatekonomi.Api"
git -C "$install_dir" fetch --no-tags https://github.com/pownas/Privatekonomi.git main
commit=$(git -C "$install_dir" rev-parse FETCH_HEAD)
echo "Målcommit: $commit"

stage=$(mktemp -d "$state/staging.XXXXXXXX")
mkdir -p "$stage/source" "$stage/publish/Web" "$stage/publish/Api"
git -C "$install_dir" archive "$commit" | tar -x -C "$stage/source"
export PATH="$HOME/.dotnet:$PATH"
export DOTNET_ROOT="$HOME/.dotnet"
for project in Web Api; do
    echo "Publicerar $project..."
    dotnet publish "$stage/source/src/Privatekonomi.$project/Privatekonomi.$project.csproj" \
        --runtime linux-arm64 --self-contained true --configuration Release \
        -o "$stage/publish/$project" /p:PublishTrimmed=false /p:PublishSingleFile=false
    test -f "$stage/publish/$project/Privatekonomi.$project"
    if [ -f "$publish/$project/appsettings.Production.json" ]; then
        cp -a "$publish/$project/appsettings.Production.json" "$stage/publish/$project/"
    fi
    printf '%s\n' "$commit" > "$stage/publish/$project/.privatekonomi-commit"
done

backup="$HOME/privatekonomi-backups/web-update-$(date +%Y%m%d_%H%M%S)-$$"
echo "Stoppar tjänster och säkerhetskopierar data..."
printf '%s\n' "$backup" > "$state/transaction"
sync -f "$state/transaction"
sudo -n /usr/bin/systemctl stop privatekonomi-web.service
sudo -n /usr/bin/systemctl stop privatekonomi-api.service
mkdir -p "$backup"
if [ -d "$HOME/privatekonomi-data" ]; then
    cp -a -- "$HOME/privatekonomi-data" "$backup/data"
fi
cp -a -- "$publish/Web" "$publish/Api" "$backup/"
touch "$state/backup-ready"
sync
for project in Web Api; do
    rm -rf -- "${publish:?}/$project"
    mv -- "$stage/publish/$project" "$publish/$project"
done

echo "Startar tjänster och verifierar hälsa..."
start_services
health_check api http://127.0.0.1:5277/internal/health
health_check web http://127.0.0.1:5274/internal/health
rm -f -- "$state/transaction" "$state/backup-ready"
echo "Uppdaterad till $commit. Säkerhetskopia: $backup"
