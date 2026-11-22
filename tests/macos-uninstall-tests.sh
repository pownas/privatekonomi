#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

system_path="$PATH"
mkdir -p "$temp/bin"
ln -s "$(command -v rm)" "$temp/bin/rm"
cat > "$temp/bin/uname" <<'EOF'
#!/bin/bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/launchctl" <<'EOF'
#!/bin/bash
case "${1:-}" in
    print)
        case "${2:-}" in
            gui/*/com.privatekonomi.web)
                if [[ "${FAIL_BOOTOUT:-false}" == true ]]; then
                    exit 0
                fi
                exit 1
                ;;
            gui/*/com.privatekonomi.*) exit 1 ;;
            *) exit 0 ;;
        esac
        ;;
    bootout)
        [[ "${FAIL_BOOTOUT:-false}" != true ]]
        exit
        ;;
    *) exit 0 ;;
esac
EOF
chmod +x "$temp/bin/uname" "$temp/bin/launchctl"

app_only_home="$temp/app-only-home"
support="$app_only_home/Library/Application Support/Privatekonomi"
backups="$app_only_home/Library/Application Support/Privatekonomi-Backups"
logs="$app_only_home/Library/Logs/Privatekonomi"
agents="$app_only_home/Library/LaunchAgents"
mkdir -p "$support/releases/test/Web" "$support/update" "$support/json" "$backups" "$logs" "$agents"
ln -s "$support/releases/test" "$support/current"
touch "$support/privatekonomi.db" "$support/releases/test/Web/Privatekonomi.Web.dll"
touch "$support/update-worker.sh" "$support/backup.sh" "$backups/backup.tar.gz" "$logs/web.log"
printf 'Sqlite\n' > "$support/storage-provider"
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    touch "$agents/$label.plist"
done

HOME="$app_only_home" PATH="$temp/bin:$system_path" \
    bash "$repo/scripts/macos/uninstall.sh" --app-only --yes
[[ ! -e "$support/current" && ! -e "$support/releases" && ! -e "$support/update" ]]
[[ ! -e "$support/update-worker.sh" && ! -e "$support/backup.sh" ]]
[[ -f "$support/privatekonomi.db" && -d "$support/json" ]]
[[ "$(cat "$support/storage-provider")" == "Sqlite" ]]
[[ -f "$backups/backup.tar.gz" && -f "$logs/web.log" ]]
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    [[ ! -e "$agents/$label.plist" ]]
done

remove_all_home="$temp/remove-all-home"
support="$remove_all_home/Library/Application Support/Privatekonomi"
backups="$remove_all_home/Library/Application Support/Privatekonomi-Backups"
logs="$remove_all_home/Library/Logs/Privatekonomi"
agents="$remove_all_home/Library/LaunchAgents"
mkdir -p "$support/releases/test/Web" "$support/update" "$backups" "$logs" "$agents"
touch "$support/privatekonomi.db" "$support/releases/test/Web/Privatekonomi.Web.dll"
touch "$backups/backup.tar.gz" "$logs/web.log"
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    touch "$agents/$label.plist"
done

HOME="$remove_all_home" PATH="$temp/bin:$system_path" \
    bash "$repo/scripts/macos/uninstall.sh" --include-data --delete-backups-and-logs --yes
[[ ! -e "$support" && ! -e "$backups" && ! -e "$logs" ]]
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    [[ ! -e "$agents/$label.plist" ]]
done

failure_home="$temp/failure-home"
support="$failure_home/Library/Application Support/Privatekonomi"
agents="$failure_home/Library/LaunchAgents"
mkdir -p "$support" "$agents"
touch "$support/privatekonomi.db" "$agents/com.privatekonomi.web.plist"
if HOME="$failure_home" PATH="$temp/bin:$system_path" FAIL_BOOTOUT=true \
    bash "$repo/scripts/macos/uninstall.sh" --include-data --yes > "$temp/failure-output" 2>&1; then
    echo "Expected uninstall to stop before removing data when a service cannot be unloaded." >&2
    exit 1
fi
[[ -f "$support/privatekonomi.db" ]]

printf 'macOS uninstall preserves data by default and removes only explicitly selected paths.\n'
