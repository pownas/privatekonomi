#!/usr/bin/env bash
# Installed outside the checkout by raspberry-pi-enable-web-update.sh.
set -Eeuo pipefail
umask 077

state="$HOME/privatekonomi-update"
install_dir="$HOME/privatekonomi"
publish="$install_dir/publish"
backup="$HOME/privatekonomi-backups/web-update-$(date +%Y%m%d_%H%M%S)"
stage=""
web_backed_up=false
api_backed_up=false
stopped=false

finish() {
    result=$?
    trap - EXIT
    set +e
    if [ "$result" -ne 0 ]; then
        echo "Uppdateringen misslyckades (kod $result)."
        if [ "$web_backed_up" = true ]; then
            echo "Återställer föregående publicerade version från $backup."
            rm -rf -- "$publish/Web"
            mv -- "$backup/Web" "$publish/Web"
        fi
        if [ "$api_backed_up" = true ]; then
            rm -rf -- "$publish/Api"
            mv -- "$backup/Api" "$publish/Api"
        fi
        if [ "$stopped" = true ]; then
            sudo -n /usr/bin/systemctl start privatekonomi-api.service
            sudo -n /usr/bin/systemctl start privatekonomi-web.service
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
    if [ -f "$publish/$project/appsettings.Production.json" ]; then
        cp -a "$publish/$project/appsettings.Production.json" "$stage/publish/$project/"
    fi
done

echo "Stoppar tjänster och säkerhetskopierar data..."
sudo -n /usr/bin/systemctl stop privatekonomi-web.service
stopped=true
sudo -n /usr/bin/systemctl stop privatekonomi-api.service
mkdir -p "$backup"
if [ -d "$HOME/privatekonomi-data" ]; then
    cp -a -- "$HOME/privatekonomi-data" "$backup/data"
fi
mv -- "$publish/Web" "$backup/Web"
web_backed_up=true
mv -- "$publish/Api" "$backup/Api"
api_backed_up=true
mv -- "$stage/publish/Web" "$publish/Web"
mv -- "$stage/publish/Api" "$publish/Api"

echo "Startar tjänster..."
sudo -n /usr/bin/systemctl start privatekonomi-api.service
sudo -n /usr/bin/systemctl start privatekonomi-web.service
sleep 5
/usr/bin/systemctl is-active --quiet privatekonomi-api.service
/usr/bin/systemctl is-active --quiet privatekonomi-web.service
printf '%s\n' "$commit" > "$state/installed"
echo "Uppdaterad till $commit. Säkerhetskopia: $backup"
