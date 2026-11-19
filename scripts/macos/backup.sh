#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

APP_SUPPORT_DIR="$HOME/Library/Application Support/Privatekonomi"
DATA_DIR="$APP_SUPPORT_DIR/json"
BACKUP_DIR="$HOME/Library/Application Support/Privatekonomi-Backups"
PROVIDER_FILE="$APP_SUPPORT_DIR/storage-provider"
LOCK_DIR="$BACKUP_DIR/.backup.lock"

log() {
    printf '[%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$*"
}

mkdir -p "$BACKUP_DIR"
chmod 700 "$BACKUP_DIR"
if ! mkdir "$LOCK_DIR" 2>/dev/null; then
    lock_pid=""
    if [[ -f "$LOCK_DIR/pid" ]]; then
        IFS= read -r lock_pid < "$LOCK_DIR/pid" || true
    fi
    if [[ "$lock_pid" =~ ^[0-9]+$ ]] && kill -0 "$lock_pid" 2>/dev/null; then
        log "Backup avbryts: en annan backup körs med PID $lock_pid."
        exit 1
    fi
    rm -f "$LOCK_DIR/pid"
    rmdir "$LOCK_DIR" 2>/dev/null || {
        log "Backup avbryts: kunde inte rensa ett gammalt lås."
        exit 1
    }
    mkdir "$LOCK_DIR"
fi
printf '%s\n' "$$" > "$LOCK_DIR/pid"

temporary_backup=""
cleanup() {
    if [[ -n "$temporary_backup" ]]; then
        rm -f -- "$temporary_backup"
    fi
    rm -f "$LOCK_DIR/pid"
    rmdir "$LOCK_DIR" 2>/dev/null || true
}
trap cleanup EXIT

provider="Sqlite"
if [[ -f "$PROVIDER_FILE" ]]; then
    IFS= read -r provider < "$PROVIDER_FILE" || true
fi
backup_date="$(date '+%Y-%m-%d')"

case "$provider" in
    Sqlite)
        database="$APP_SUPPORT_DIR/privatekonomi.db"
        if [[ ! -s "$database" ]]; then
            log "SQLite-databasen saknas eller är tom: $database"
            exit 1
        fi
        if ! command -v sqlite3 >/dev/null 2>&1; then
            log "sqlite3 saknas; installera SQLite CLI innan backup kan köras."
            exit 1
        fi

        destination="$BACKUP_DIR/privatekonomi_$backup_date.db"
        temporary_backup="$BACKUP_DIR/.privatekonomi_$backup_date.db.tmp.$$"
        escaped_path="${temporary_backup//\\/\\\\}"
        escaped_path="${escaped_path//\"/\\\"}"
        if ! sqlite3 "$database" ".backup \"$escaped_path\""; then
            log "SQLite kunde inte skapa backup."
            exit 1
        fi
        integrity="$(sqlite3 "$temporary_backup" 'PRAGMA integrity_check;')"
        if [[ "$integrity" != "ok" ]]; then
            log "SQLite-backupens integritetskontroll misslyckades: $integrity"
            exit 1
        fi
        mv -f -- "$temporary_backup" "$destination"
        temporary_backup=""
        log "Verifierad SQLite-backup skapad: $destination"
        find "$BACKUP_DIR" -type f -name 'privatekonomi_*.db' -mtime +60 -delete
        ;;
    JsonFile)
        if [[ ! -d "$DATA_DIR" ]] || ! find "$DATA_DIR" -type f -name '*.json' -print -quit | grep -q .; then
            log "JSON-data saknas; ingen backup skapades."
            exit 1
        fi

        destination="$BACKUP_DIR/privatekonomi_$backup_date.tar.gz"
        temporary_backup="$BACKUP_DIR/.privatekonomi_$backup_date.tar.gz.tmp.$$"
        if ! tar -czf "$temporary_backup" -C "$DATA_DIR" .; then
            log "Kunde inte arkivera JSON-data."
            exit 1
        fi
        if ! tar -tzf "$temporary_backup" >/dev/null; then
            log "JSON-backupens arkivkontroll misslyckades."
            exit 1
        fi
        mv -f -- "$temporary_backup" "$destination"
        temporary_backup=""
        log "Verifierad JSON-backup skapad: $destination"
        find "$BACKUP_DIR" -type f -name 'privatekonomi_*.tar.gz' -mtime +60 -delete
        ;;
    *)
        log "Okänd lagringsprovider '$provider'; backup avbryts."
        exit 1
        ;;
esac
