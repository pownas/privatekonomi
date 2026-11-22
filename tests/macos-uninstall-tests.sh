#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

system_path="$PATH"
launchctl_state="$temp/launchctl-state"
mkdir -p "$temp/bin" "$launchctl_state"
ln -s "$(command -v rm)" "$temp/bin/rm"
cat > "$temp/bin/uname" <<'EOF'
#!/bin/bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/launchctl" <<'EOF'
#!/bin/bash
printf '%s\n' "$*" >> "$LAUNCHCTL_STATE_DIR/calls"
case "${1:-}" in
    print)
        case "${2:-}" in
            gui/*/com.privatekonomi.web)
                printf 'web branch\n' >> "$LAUNCHCTL_STATE_DIR/calls"
                [[ -f "$LAUNCHCTL_STATE_DIR/web" ]]
                ;;
            gui/*/com.privatekonomi.api) [[ -f "$LAUNCHCTL_STATE_DIR/api" ]] ;;
            gui/*/com.privatekonomi.update) [[ -f "$LAUNCHCTL_STATE_DIR/update" ]] ;;
            gui/*/com.privatekonomi.backup) [[ -f "$LAUNCHCTL_STATE_DIR/backup" ]] ;;
            gui/*)
                printf 'domain branch\n' >> "$LAUNCHCTL_STATE_DIR/calls"
                exit 0
                ;;
            *)
                printf 'default branch\n' >> "$LAUNCHCTL_STATE_DIR/calls"
                exit 0
                ;;
        esac
        ;;
    bootout)
        case "${2:-}" in
            gui/*/com.privatekonomi.web)
                if [[ "${FAIL_WEB_BOOTOUT:-false}" == true ]]; then
                    printf 'mock launchctl web bootout failed\n' >&2
                    exit 1
                fi
                rm -f "$LAUNCHCTL_STATE_DIR/web"
                ;;
            gui/*/com.privatekonomi.api) rm -f "$LAUNCHCTL_STATE_DIR/api" ;;
            gui/*/com.privatekonomi.update) rm -f "$LAUNCHCTL_STATE_DIR/update" ;;
            gui/*/com.privatekonomi.backup) rm -f "$LAUNCHCTL_STATE_DIR/backup" ;;
            gui/*)
                case "${3##*/}" in
                    com.privatekonomi.web.plist)
                        if [[ "${FAIL_WEB_BOOTOUT:-false}" == true ]]; then
                            printf 'mock launchctl web plist bootout failed\n' >&2
                            exit 1
                        fi
                        if [[ "${FAIL_PATH_BOOTOUT:-false}" == true ]]; then
                            printf 'mock plist bootout failed\n' >&2
                            exit 1
                        fi
                        rm -f "$LAUNCHCTL_STATE_DIR/web"
                        ;;
                    com.privatekonomi.api.plist) rm -f "$LAUNCHCTL_STATE_DIR/api" ;;
                    com.privatekonomi.update.plist) rm -f "$LAUNCHCTL_STATE_DIR/update" ;;
                    com.privatekonomi.backup.plist) rm -f "$LAUNCHCTL_STATE_DIR/backup" ;;
                esac
                ;;
        esac
        exit 0
        ;;
    bootstrap)
        case "${3##*/}" in
            com.privatekonomi.web.plist) touch "$LAUNCHCTL_STATE_DIR/web" ;;
            com.privatekonomi.api.plist) touch "$LAUNCHCTL_STATE_DIR/api" ;;
            com.privatekonomi.update.plist) touch "$LAUNCHCTL_STATE_DIR/update" ;;
            com.privatekonomi.backup.plist) touch "$LAUNCHCTL_STATE_DIR/backup" ;;
        esac
        exit 0
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

if ! HOME="$app_only_home" PATH="$temp/bin:$system_path" LAUNCHCTL_STATE_DIR="$launchctl_state" LAUNCHCTL_COMMAND="$temp/bin/launchctl" \
    bash "$repo/scripts/macos/uninstall.sh" --app-only --yes; then
    cat "$launchctl_state/calls" >&2
    exit 1
fi
[[ ! -e "$support/current" && ! -e "$support/releases" && ! -e "$support/update" ]]
[[ ! -e "$support/update-worker.sh" && ! -e "$support/backup.sh" ]]
[[ -f "$support/privatekonomi.db" && -d "$support/json" ]]
[[ "$(cat "$support/storage-provider")" == "Sqlite" ]]
[[ -f "$backups/backup.tar.gz" && -f "$logs/web.log" ]]
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    [[ ! -e "$agents/$label.plist" ]]
done

fallback_home="$temp/fallback-home"
support="$fallback_home/Library/Application Support/Privatekonomi"
agents="$fallback_home/Library/LaunchAgents"
mkdir -p "$support/releases/test/Web" "$agents"
touch "$support/privatekonomi.db" "$support/releases/test/Web/Privatekonomi.Web.dll"
touch "$agents/com.privatekonomi.web.plist"
ln -s "$support/releases/test" "$support/current"
touch "$launchctl_state/web"
HOME="$fallback_home" PATH="$temp/bin:$system_path" LAUNCHCTL_STATE_DIR="$launchctl_state" LAUNCHCTL_COMMAND="$temp/bin/launchctl" FAIL_PATH_BOOTOUT=true \
    bash "$repo/scripts/macos/uninstall.sh" --app-only --yes
[[ ! -e "$support/current" && -f "$support/privatekonomi.db" ]]
[[ ! -e "$agents/com.privatekonomi.web.plist" ]]

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

HOME="$remove_all_home" PATH="$temp/bin:$system_path" LAUNCHCTL_STATE_DIR="$launchctl_state" LAUNCHCTL_COMMAND="$temp/bin/launchctl" \
    bash "$repo/scripts/macos/uninstall.sh" --include-data --delete-backups-and-logs --yes
[[ ! -e "$support" && ! -e "$backups" && ! -e "$logs" ]]
for label in com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup; do
    [[ ! -e "$agents/$label.plist" ]]
done

failure_home="$temp/failure-home"
support="$failure_home/Library/Application Support/Privatekonomi"
agents="$failure_home/Library/LaunchAgents"
mkdir -p "$support" "$agents"
touch "$support/privatekonomi.db" "$agents/com.privatekonomi.web.plist" "$agents/com.privatekonomi.update.plist"
touch "$launchctl_state/web" "$launchctl_state/update"
if HOME="$failure_home" PATH="$temp/bin:$system_path" LAUNCHCTL_STATE_DIR="$launchctl_state" LAUNCHCTL_COMMAND="$temp/bin/launchctl" FAIL_WEB_BOOTOUT=true \
    bash "$repo/scripts/macos/uninstall.sh" --include-data --yes > "$temp/failure-output" 2>&1; then
    echo "Expected uninstall to stop before removing data when a service cannot be unloaded." >&2
    exit 1
fi
[[ -f "$support/privatekonomi.db" ]]
[[ -f "$launchctl_state/update" ]]

printf 'macOS uninstall preserves data by default and removes only explicitly selected paths.\n'
