#!/usr/bin/env bash
set -Eeuo pipefail
umask 077

APP_SUPPORT_DIR="$HOME/Library/Application Support/Privatekonomi"
BACKUP_DIR="$HOME/Library/Application Support/Privatekonomi-Backups"
LOG_DIR="$HOME/Library/Logs/Privatekonomi"
AGENT_DIR="$HOME/Library/LaunchAgents"
AGENT_LABELS=(com.privatekonomi.web com.privatekonomi.api com.privatekonomi.update com.privatekonomi.backup)
REMOVE_DATA=false
REMOVE_BACKUPS_AND_LOGS=false
MODE_SPECIFIED=false
BACKUP_CHOICE_SPECIFIED=false
ASSUME_YES=false

log_info() { printf '[INFO] %s\n' "$1"; }
log_success() { printf '[OK] %s\n' "$1"; }
log_error() { printf '[ERROR] %s\n' "$1" >&2; }

show_help() {
    printf 'Användning: bash scripts/macos/uninstall.sh [ALTERNATIV]\n'
    printf 'Utan alternativ får du välja interaktivt. Standardvalet bevarar data, backuper och loggar.\n'
    printf '  --app-only                  Ta bort programmet men behåll SQLite/JSON-data.\n'
    printf '  --include-data              Ta även bort SQLite/JSON-data och programinställningar.\n'
    printf '  --delete-backups-and-logs   Ta även bort backup- och loggkatalogerna.\n'
    printf '  --keep-backups-and-logs     Behåll backup- och loggkatalogerna.\n'
    printf '  --yes                       Hoppa över slutbekräftelsen; ange även raderingsomfång.\n'
    printf '  --help, -h                  Visa denna hjälp.\n'
}

fail_if_symlink() {
    local path="$1"
    if [[ -L "$path" ]]; then
        log_error "Vägrar radera en symbolisk länk i stället för en katalog: $path"
        return 1
    fi
}

prompt_for_choices() {
    local answer

    if [[ "$MODE_SPECIFIED" != true ]]; then
        if [[ "$ASSUME_YES" == true || ! -t 0 ]]; then
            log_error "Ange --app-only eller --include-data när skriptet körs utan interaktiv terminal."
            return 2
        fi
        printf 'Vad ska tas bort?\n'
        printf '  1) Bara programmet (standard; behåll SQLite/JSON-data)\n'
        printf '  2) Programmet och all SQLite/JSON-data\n'
        read -r -p 'Val [1]: ' answer || answer=""
        case "${answer:-1}" in
            1) REMOVE_DATA=false ;;
            2) REMOVE_DATA=true ;;
            *) log_error "Ogiltigt val '$answer'."; return 2 ;;
        esac
    fi

    if [[ "$BACKUP_CHOICE_SPECIFIED" != true && "$ASSUME_YES" != true ]]; then
        if [[ ! -t 0 ]]; then
            log_error "Välj --keep-backups-and-logs eller --delete-backups-and-logs utan interaktiv terminal."
            return 2
        fi
        printf 'Vad ska göras med backuper och loggar?\n'
        printf '  1) Behåll dem (standard)\n'
        printf '  2) Ta bort dem\n'
        read -r -p 'Val [1]: ' answer || answer=""
        case "${answer:-1}" in
            1) REMOVE_BACKUPS_AND_LOGS=false ;;
            2) REMOVE_BACKUPS_AND_LOGS=true ;;
            *) log_error "Ogiltigt val '$answer'."; return 2 ;;
        esac
    fi
}

main() {
    local uid label target plist unload_failed=false confirmation=""

    while [[ $# -gt 0 ]]; do
        case "$1" in
            --app-only)
                if [[ "$MODE_SPECIFIED" == true && "$REMOVE_DATA" == true ]]; then
                    log_error "--app-only och --include-data kan inte kombineras."
                    return 2
                fi
                REMOVE_DATA=false
                MODE_SPECIFIED=true
                ;;
            --include-data)
                if [[ "$MODE_SPECIFIED" == true && "$REMOVE_DATA" == false ]]; then
                    log_error "--app-only och --include-data kan inte kombineras."
                    return 2
                fi
                REMOVE_DATA=true
                MODE_SPECIFIED=true
                ;;
            --delete-backups-and-logs)
                if [[ "$BACKUP_CHOICE_SPECIFIED" == true && "$REMOVE_BACKUPS_AND_LOGS" == false ]]; then
                    log_error "Välj antingen --keep-backups-and-logs eller --delete-backups-and-logs."
                    return 2
                fi
                REMOVE_BACKUPS_AND_LOGS=true
                BACKUP_CHOICE_SPECIFIED=true
                ;;
            --keep-backups-and-logs)
                if [[ "$BACKUP_CHOICE_SPECIFIED" == true && "$REMOVE_BACKUPS_AND_LOGS" == true ]]; then
                    log_error "Välj antingen --keep-backups-and-logs eller --delete-backups-and-logs."
                    return 2
                fi
                REMOVE_BACKUPS_AND_LOGS=false
                BACKUP_CHOICE_SPECIFIED=true
                ;;
            --yes)
                ASSUME_YES=true
                ;;
            --help|-h)
                show_help
                return 0
                ;;
            *)
                log_error "Okänt argument: $1"
                show_help >&2
                return 2
                ;;
        esac
        shift
    done

    if [[ "$(uname -s)" != "Darwin" ]]; then
        log_error "Avinstallation är endast tillgänglig på macOS."
        return 1
    fi
    if [[ -z "${HOME:-}" || "$HOME" != /* || "$HOME" == "/" ]]; then
        log_error "HOME är tom eller ogiltig; radering avbryts."
        return 1
    fi
    if ! command -v launchctl >/dev/null 2>&1; then
        log_error "launchctl saknas; tjänsterna kan inte stoppas säkert."
        return 1
    fi

    prompt_for_choices
    fail_if_symlink "$AGENT_DIR"
    fail_if_symlink "$APP_SUPPORT_DIR"
    if [[ "$REMOVE_BACKUPS_AND_LOGS" == true ]]; then
        fail_if_symlink "$BACKUP_DIR"
        fail_if_symlink "$LOG_DIR"
    fi

    printf '\nDetta rensas:\n'
    printf '  - Privatekonomis LaunchAgents och publicerade programfiler.\n'
    if [[ "$REMOVE_DATA" == true ]]; then
        printf '  - Hela katalogen %s, inklusive SQLite/JSON-data.\n' "$APP_SUPPORT_DIR"
    else
        printf '  - Programdata i %s behålls (SQLite/JSON och lagringsval).\n' "$APP_SUPPORT_DIR"
    fi
    if [[ "$REMOVE_BACKUPS_AND_LOGS" == true ]]; then
        printf '  - Backuper: %s\n' "$BACKUP_DIR"
        printf '  - Loggar: %s\n' "$LOG_DIR"
    else
        printf '  - Backuper och loggar behålls.\n'
    fi
    printf '  - Källkodsklonen och .NET SDK påverkas inte.\n'

    if [[ "$ASSUME_YES" != true ]]; then
        if [[ ! -t 0 ]]; then
            log_error "Slutbekräftelsen kräver en interaktiv terminal eller --yes."
            return 2
        fi
        read -r -p 'Skriv RADERA för att fortsätta: ' confirmation || confirmation=""
        if [[ "$confirmation" != "RADERA" ]]; then
            log_info "Avbrutet; inga filer togs bort."
            return 0
        fi
    fi

    uid="$(id -u)"
    if ! launchctl print "gui/$uid" >/dev/null 2>&1; then
        log_error "Kan inte läsa användarens launchd-domän gui/$uid; inget har raderats."
        return 1
    fi

    for label in "${AGENT_LABELS[@]}"; do
        target="gui/$uid/$label"
        if launchctl print "$target" >/dev/null 2>&1; then
            if ! launchctl bootout "$target" >/dev/null 2>&1 ||
               launchctl print "$target" >/dev/null 2>&1; then
                log_error "Kunde inte stoppa tjänsten $label; filer lämnas orörda."
                unload_failed=true
            else
                log_info "Stoppade $label."
            fi
        else
            log_info "$label var inte startad."
        fi
    done
    if [[ "$unload_failed" == true ]]; then
        return 1
    fi

    for label in "${AGENT_LABELS[@]}"; do
        plist="$AGENT_DIR/$label.plist"
        if [[ -e "$plist" || -L "$plist" ]]; then
            rm -f -- "$plist"
        fi
    done

    if [[ "$REMOVE_DATA" == true ]]; then
        if [[ -e "$APP_SUPPORT_DIR" ]]; then
            rm -rf -- "$APP_SUPPORT_DIR"
        fi
    else
        rm -rf -- \
            "$APP_SUPPORT_DIR/current" \
            "$APP_SUPPORT_DIR/releases" \
            "$APP_SUPPORT_DIR/update"
        rm -f -- "$APP_SUPPORT_DIR/update-worker.sh" "$APP_SUPPORT_DIR/backup.sh"
    fi

    if [[ "$REMOVE_BACKUPS_AND_LOGS" == true ]]; then
        if [[ -e "$BACKUP_DIR" ]]; then
            rm -rf -- "$BACKUP_DIR"
        fi
        if [[ -e "$LOG_DIR" ]]; then
            rm -rf -- "$LOG_DIR"
        fi
    fi

    log_success "Privatekonomis LaunchAgents och programfiler har tagits bort."
    if [[ "$REMOVE_DATA" == false ]]; then
        log_info "SQLite/JSON-data och lagringsval har bevarats."
    fi
    if [[ "$REMOVE_BACKUPS_AND_LOGS" == false ]]; then
        log_info "Backuper och loggar har bevarats."
    fi
}

main "$@"
