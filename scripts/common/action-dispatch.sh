list_shell_actions() {
    local scripts_dir="$1"
    local script_path

    for script_path in "$scripts_dir"/*.sh; do
        [[ -f "$script_path" ]] || continue
        printf '  %-24s %s\n' "$(basename "$script_path")" "$script_path"
    done
}

resolve_shell_action() {
    local scripts_dir="$1"
    local choice="$2"
    local script_path
    local script_name
    local action_name
    local index=1

    for script_path in "$scripts_dir"/*.sh; do
        [[ -f "$script_path" ]] || continue
        script_name="$(basename "$script_path")"
        action_name="${script_name%.sh}"
        if [[ "$choice" == "$script_name" || "$choice" == "$action_name" || "$choice" == "$index" ]]; then
            printf '%s\n' "$script_path"
            return 0
        fi
        index=$((index + 1))
    done

    return 1
}

show_shell_action_help() {
    local scripts_dir="$1"
    local launcher_name="$2"

    printf 'Användning: bash %s [skriptnamn] [argument...]\n' "$launcher_name"
    printf 'Utelämna skriptnamn för att välja interaktivt. Argument utan skriptnamn skickas till install.sh.\n'
    printf 'Tillgängliga skript:\n'
    list_shell_actions "$scripts_dir"
}

dispatch_shell_action() {
    local scripts_dir="$1"
    local platform_name="$2"
    local launcher_name="$3"
    local action_choice=""
    local action_path=""
    local action_name=""
    local answer=""
    local script_path
    local index=1
    shift 3

    if [[ "${1:-}" == "--help" || "${1:-}" == "-h" ]]; then
        show_shell_action_help "$scripts_dir" "$launcher_name"
        return 0
    fi

    if [[ "$#" -gt 0 ]]; then
        action_choice="$1"
        if action_path="$(resolve_shell_action "$scripts_dir" "$action_choice")"; then
            shift
        elif [[ "$action_choice" == -* ]]; then
            action_path="$(resolve_shell_action "$scripts_dir" "install")" || {
                printf '[ERROR] %s saknar install.sh; ange ett tillgängligt skriptnamn.\n' "$platform_name" >&2
                return 2
            }
        else
            printf '[ERROR] Okänd åtgärd: %s\n' "$action_choice" >&2
            show_shell_action_help "$scripts_dir" "$launcher_name" >&2
            return 2
        fi
    else
        printf 'Välj åtgärd för %s:\n' "$platform_name"
        for script_path in "$scripts_dir"/*.sh; do
            [[ -f "$script_path" ]] || continue
            printf '  %d) %s\n' "$index" "$(basename "$script_path")"
            index=$((index + 1))
        done
        printf '  q) Avbryt\n'

        if [[ ! -r /dev/tty ]]; then
            printf '[ERROR] Välj ett skript direkt eller kör startaren i en interaktiv terminal.\n' >&2
            return 2
        fi
        printf 'Ange nummer eller skriptnamn: ' >/dev/tty
        if ! IFS= read -r action_choice </dev/tty; then
            printf '[INFO] Ingen åtgärd valdes.\n'
            return 0
        fi
        case "$action_choice" in
            q|Q) printf '[INFO] Avbrutet.\n'; return 0 ;;
        esac
        if ! action_path="$(resolve_shell_action "$scripts_dir" "$action_choice")"; then
            printf '[ERROR] Ogiltigt val: %s\n' "$action_choice" >&2
            return 2
        fi
    fi

    action_name="$(basename "$action_path")"
    printf '[INFO] Vald åtgärd: %s\n' "$action_path"
    if [[ ! -r /dev/tty ]]; then
        printf '[ERROR] Kör startaren i en interaktiv terminal för att bekräfta åtgärden.\n' >&2
        return 2
    fi
    printf 'Vill du köra %s? (y/n): ' "$action_name" >/dev/tty
    if ! IFS= read -r answer </dev/tty; then
        answer=""
    fi
    case "$answer" in
        [yY]|[yY][eE][sS]|[jJ]|[jJ][aA]) ;;
        *) printf '[INFO] Åtgärden avbröts.\n'; return 0 ;;
    esac

    exec bash "$action_path" "$@"
}