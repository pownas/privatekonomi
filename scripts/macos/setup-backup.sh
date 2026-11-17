#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
APP_SUPPORT_DIR="$HOME/Library/Application Support/Privatekonomi"
BACKUP_SCRIPT="$APP_SUPPORT_DIR/backup.sh"
AGENT_DIR="$HOME/Library/LaunchAgents"
PLIST="$AGENT_DIR/com.privatekonomi.backup.plist"
LOG_DIR="$HOME/Library/Logs/Privatekonomi"
LABEL="com.privatekonomi.backup"

if [[ "$(uname -s)" != "Darwin" ]]; then
    printf '[ERROR] Schemaläggning av backup är endast tillgänglig på macOS.\n' >&2
    exit 1
fi

mkdir -p "$APP_SUPPORT_DIR" "$AGENT_DIR" "$LOG_DIR"
chmod 700 "$APP_SUPPORT_DIR"
uid="$(id -u)"

if [[ -f "$BACKUP_SCRIPT" ]] && cmp -s "$SCRIPT_DIR/backup.sh" "$BACKUP_SCRIPT" &&
   [[ -f "$PLIST" ]] && launchctl print "gui/$uid/$LABEL" >/dev/null 2>&1; then
    printf '[INFO] Daglig backup är redan schemalagd.\n'
    exit 0
fi

temporary_backup_script="$BACKUP_SCRIPT.tmp.$$"
cp "$SCRIPT_DIR/backup.sh" "$temporary_backup_script"
chmod 700 "$temporary_backup_script"
mv -f "$temporary_backup_script" "$BACKUP_SCRIPT"
if [[ ! -s "$APP_SUPPORT_DIR/storage-provider" ]]; then
    printf 'Sqlite\n' > "$APP_SUPPORT_DIR/storage-provider"
    chmod 600 "$APP_SUPPORT_DIR/storage-provider"
fi

escape_xml() {
    local value="$1"
    value="${value//&/\&amp;}"
    value="${value//</\&lt;}"
    value="${value//>/\&gt;}"
    value="${value//\"/\&quot;}"
    printf '%s' "$value"
}

temporary_plist="$PLIST.tmp.$$"
trap 'rm -f "$temporary_plist" "$temporary_backup_script"' EXIT

cat > "$temporary_plist" <<EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>$LABEL</string>
    <key>ProgramArguments</key>
    <array>
        <string>/bin/bash</string>
        <string>$(escape_xml "$BACKUP_SCRIPT")</string>
    </array>
    <key>StartCalendarInterval</key>
    <dict>
        <key>Hour</key>
        <integer>2</integer>
        <key>Minute</key>
        <integer>30</integer>
    </dict>
    <key>StandardOutPath</key>
    <string>$(escape_xml "$LOG_DIR/backup.log")</string>
    <key>StandardErrorPath</key>
    <string>$(escape_xml "$LOG_DIR/backup-error.log")</string>
    <key>EnvironmentVariables</key>
    <dict>
        <key>HOME</key>
        <string>$(escape_xml "$HOME")</string>
        <key>PATH</key>
        <string>/usr/bin:/bin:/usr/sbin:/sbin</string>
    </dict>
</dict>
</plist>
EOF

/usr/bin/plutil -lint "$temporary_plist" >/dev/null
launchctl bootout "gui/$uid" "$PLIST" >/dev/null 2>&1 || true
mv -f "$temporary_plist" "$PLIST"
launchctl bootstrap "gui/$uid" "$PLIST"

printf '[OK] Daglig backup schemalagd kl. 02:30 (när Macen är vaken).\n'
printf '[INFO] Backupfiler: %s\n' "$HOME/Library/Application Support/Privatekonomi-Backups"
printf '[INFO] Logg: %s/backup.log\n' "$LOG_DIR"
printf '[INFO] Manuell backup: /bin/bash "%s"\n' "$BACKUP_SCRIPT"
