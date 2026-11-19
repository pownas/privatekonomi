#!/usr/bin/env bash

run_with_progress() {
    local label="$1"
    local log_file="$2"
    local interval="${INSTALLER_PROGRESS_INTERVAL_SECONDS:-15}"
    local elapsed=0
    local next_report
    local command_pid
    local exit_code=0
    shift 2

    if [[ ! "$interval" =~ ^[1-9][0-9]*$ ]]; then
        printf '[ERROR] INSTALLER_PROGRESS_INTERVAL_SECONDS måste vara ett positivt heltal.\n' >&2
        return 2
    fi
    if ! touch "$log_file"; then
        printf '[ERROR] Kunde inte skriva bygglogg: %s\n' "$log_file" >&2
        return 1
    fi
    {
        printf '\n[%s] %s\n' "$(date '+%Y-%m-%d %H:%M:%S')" "$label"
        printf 'Kommando:'
        printf ' %q' "$@"
        printf '\n'
    } >> "$log_file"

    printf '[INFO] %s...\n' "$label"
    printf '[INFO] Utdata visas löpande och sparas i: %s\n' "$log_file"
    (
        set -o pipefail
        "$@" 2>&1 | tee -a "$log_file"
    ) &
    command_pid=$!
    next_report="$interval"

    while kill -0 "$command_pid" 2>/dev/null; do
        sleep 1
        if kill -0 "$command_pid" 2>/dev/null; then
            elapsed=$((elapsed + 1))
            if (( elapsed >= next_report )); then
                printf '[INFO] %s pågår fortfarande (%s sekunder)...\n' "$label" "$elapsed"
                next_report=$((next_report + interval))
            fi
        fi
    done

    if wait "$command_pid"; then
        printf '[OK] %s slutfördes (%s sekunder).\n' "$label" "$elapsed"
        return 0
    else
        exit_code=$?
        printf '[ERROR] %s misslyckades (exitkod %s). Senaste byggutdata:\n' "$label" "$exit_code" >&2
        tail -n 40 "$log_file" >&2 || true
        printf '[ERROR] Full utdata finns i: %s\n' "$log_file" >&2
        return "$exit_code"
    fi
}
