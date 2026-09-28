#!/usr/bin/env bash
set -euo pipefail

repo=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)
temp=$(mktemp -d)
trap 'rm -rf -- "$temp"' EXIT

setup_case() {
    root="$temp/$1"
    mkdir -p "$root/bin" "$root/home/privatekonomi/.git" "$root/home/privatekonomi/publish/Web" \
        "$root/home/privatekonomi/publish/Api" "$root/home/privatekonomi-update" \
        "$root/home/privatekonomi-data" "$root/seed/src/Privatekonomi.Web" "$root/seed/src/Privatekonomi.Api"
    touch "$root/home/privatekonomi-update/enabled" "$root/home/privatekonomi-update/request"
    printf 'old\n' > "$root/home/privatekonomi/publish/Web/Privatekonomi.Web"
    printf 'old\n' > "$root/home/privatekonomi/publish/Api/Privatekonomi.Api"
    printf 'settings\n' > "$root/home/privatekonomi/publish/Web/appsettings.Production.json"
    printf 'customer-data\n' > "$root/home/privatekonomi-data/data.db"
    sed "s@/usr/bin/systemctl@$root/bin/systemctl@g" "$repo/raspberry-pi-web-update.sh" > "$root/worker"
    cat > "$root/bin/git" <<'EOF'
#!/usr/bin/env bash
case " $* " in
    *" rev-parse "*) printf '%040d\n' 1 ;;
    *" archive "*) tar -C "$MOCK_ROOT/seed" -c . ;;
    *" fetch "*) : ;;
    *) exit 1 ;;
esac
EOF
    cat > "$root/bin/dotnet" <<'EOF'
#!/usr/bin/env bash
[[ "$*" == *"Privatekonomi.Api.csproj"* ]] && [ "${FAIL_PUBLISH_API:-0}" = 1 ] && exit 1
while [ "$1" != "-o" ]; do shift; done
out="$2"
mkdir -p "$out"
if [[ "$out" == */Web ]]; then
    printf 'new\n' > "$out/Privatekonomi.Web"
else
    printf 'new\n' > "$out/Privatekonomi.Api"
fi
EOF
    cat > "$root/bin/systemctl" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$*" >> "$MOCK_ROOT/commands"
case "$*" in
    *"stop privatekonomi-web.service"*) rm -f "$MOCK_ROOT/web-running" ;;
    *"stop privatekonomi-api.service"*) rm -f "$MOCK_ROOT/api-running" ;;
    *"start privatekonomi-web.service"*)
        if [ ! -f "$MOCK_ROOT/web-running" ]; then
            version=$(cat "$MOCK_ROOT/home/privatekonomi/publish/Web/Privatekonomi.Web")
            if [ "${FAIL_START:-0}" = 1 ] && [ "$version" = new ]; then exit 1; fi
            printf '%s\n' "$version" > "$MOCK_ROOT/web-running"
        fi ;;
    *"start privatekonomi-api.service"*)
        if [ ! -f "$MOCK_ROOT/api-running" ]; then
            cat "$MOCK_ROOT/home/privatekonomi/publish/Api/Privatekonomi.Api" > "$MOCK_ROOT/api-running"
        fi ;;
    *"is-active "*)
        if [[ "$*" == *"privatekonomi-web.service"* ]]; then
            test -f "$MOCK_ROOT/web-running"
        else
            test -f "$MOCK_ROOT/api-running"
        fi ;;
esac
EOF
    cat > "$root/bin/sudo" <<'EOF'
#!/usr/bin/env bash
shift
exec "$@"
EOF
    cat > "$root/bin/curl" <<'EOF'
#!/usr/bin/env bash
if [ "${FAIL_HEALTH:-0}" = 1 ] && [[ "$*" == *"/internal/health"* ]] &&
  [ "$(cat "$MOCK_ROOT/web-running" 2>/dev/null)" = new ]; then
    printf '503'
    exit 0
fi
if [[ "$*" == *":5274/"* ]] && [ ! -f "$MOCK_ROOT/web-running" ]; then printf '000'; exit 0; fi
if [[ "$*" == *":5277/"* ]] && [ ! -f "$MOCK_ROOT/api-running" ]; then printf '000'; exit 0; fi
printf '200'
EOF
    cat > "$root/bin/sleep" <<'EOF'
#!/usr/bin/env bash
exit 0
EOF
    cat > "$root/bin/sync" <<'EOF'
#!/usr/bin/env bash
exit 0
EOF
    cat > "$root/bin/mv" <<'EOF'
#!/usr/bin/env bash
if [ "${CRASH_DURING_SWAP:-0}" = 1 ] && [[ "$*" == *"/publish/Api" ]] &&
   [[ "$*" == *"/staging."* ]]; then
    kill -KILL "$PPID"
    exit 1
fi
exec /bin/mv "$@"
EOF
    cat > "$root/bin/cp" <<'EOF'
#!/usr/bin/env bash
if [ "${FAIL_RESTORE:-0}" = 1 ] && [[ "$*" == *"web-update-"*"/Web "*"/publish/Web" ]]; then
    exit 1
fi
exec /bin/cp "$@"
EOF
    chmod +x "$root/bin/"*
}

run_worker() {
    HOME="$root/home" MOCK_ROOT="$root" PATH="$root/bin:$PATH" \
        bash "$root/worker" >/dev/null 2>&1
}

assert_old() {
    test "$(cat "$root/home/privatekonomi/publish/Web/Privatekonomi.Web")" = old
    test "$(cat "$root/home/privatekonomi/publish/Api/Privatekonomi.Api")" = old
    test "$(cat "$root/home/privatekonomi-data/data.db")" = customer-data
}

assert_running_old() {
    assert_old
    test "$(cat "$root/web-running")" = old
    test "$(cat "$root/api-running")" = old
}

setup_case success
run_worker
test "$(cat "$root/home/privatekonomi-update/status")" = succeeded
test "$(cat "$root/home/privatekonomi/publish/Web/Privatekonomi.Web")" = new
test "$(cat "$root/home/privatekonomi/publish/Api/Privatekonomi.Api")" = new
test "$(cat "$root/home/privatekonomi/publish/Web/appsettings.Production.json")" = settings
test "$(cat "$root/home/privatekonomi/publish/Web/.privatekonomi-commit")" = "$(printf '%040d' 1)"
test ! -e "$root/home/privatekonomi-update/request"
test ! -e "$root/home/privatekonomi-update/transaction"

setup_case missing_installation
rm -rf "$root/home/privatekonomi/.git"
if run_worker; then exit 1; fi
assert_old
test "$(cat "$root/home/privatekonomi-update/status")" = failed
test ! -e "$root/home/privatekonomi-update/request"

setup_case publish_failure
if FAIL_PUBLISH_API=1 run_worker; then exit 1; fi
assert_old
test "$(cat "$root/home/privatekonomi-update/status")" = failed
test ! -e "$root/home/privatekonomi-update/request"
test ! -s "$root/commands"

setup_case restart_failure
if FAIL_START=1 run_worker; then exit 1; fi
assert_running_old
test "$(cat "$root/home/privatekonomi-update/status")" = failed
test ! -e "$root/home/privatekonomi-update/transaction"

setup_case health_failure
if FAIL_HEALTH=1 run_worker; then exit 1; fi
assert_running_old
test "$(grep -c 'stop privatekonomi-web.service' "$root/commands")" -eq 2
test "$(cat "$root/home/privatekonomi-update/status")" = failed

setup_case blocked_recovery
if FAIL_HEALTH=1 FAIL_RESTORE=1 run_worker; then exit 1; fi
test -e "$root/home/privatekonomi-update/request"
test -e "$root/home/privatekonomi-update/transaction"
test "$(cat "$root/home/privatekonomi-update/status")" = failed
if FAIL_RESTORE=1 run_worker; then exit 1; fi
test -e "$root/home/privatekonomi-update/transaction"
run_worker && exit 1
assert_running_old
test ! -e "$root/home/privatekonomi-update/request"

setup_case power_failure
if (CRASH_DURING_SWAP=1 run_worker); then exit 1; fi
test -e "$root/home/privatekonomi-update/transaction"
test ! -e "$root/home/privatekonomi/publish/Api/Privatekonomi.Api" ||
    test "$(cat "$root/home/privatekonomi/publish/Web/Privatekonomi.Web")" = new
if run_worker; then exit 1; fi
assert_running_old
test "$(cat "$root/home/privatekonomi-update/status")" = failed
test ! -e "$root/home/privatekonomi-update/transaction"
test ! -e "$root/home/privatekonomi-update/request"

echo "Lyckad uppdatering, publiceringsfel, omstartsfel, hälsofel och avbrott verifierade."
