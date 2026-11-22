#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
source "$SCRIPT_DIR/dotnet.sh"
source "$SCRIPT_DIR/../common/run-with-progress.sh"
LOG_DIR="$HOME/Library/Logs/Privatekonomi"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/install-$(date '+%Y%m%d-%H%M%S').log"
BUILD_LOG_FILE="${LOG_FILE%.log}-build.log"
REPOSITORY_URL="https://github.com/pownas/Privatekonomi.git"
INSTALL_BRANCH="${PRIVATEKONOMI_BRANCH:-}"
INSTALL_COMMIT=""
INSTALL_SOURCE_ROOT=""
SKIP_INTERACTIVE="${PRIVATEKONOMI_SKIP_INTERACTIVE:-false}"
touch "$BUILD_LOG_FILE"
export PRIVATEKONOMI_BUILD_LOG_FILE="$BUILD_LOG_FILE"
exec > >(tee -a "$LOG_FILE") 2>&1

CURRENT_STEP="start"

log_info() { printf '[INFO] %s\n' "$1"; }
log_success() { printf '[OK] %s\n' "$1"; }
log_warning() { printf '[WARN] %s\n' "$1" >&2; }
log_error() { printf '[ERROR] %s\n' "$1" >&2; }

on_error() {
    local exit_code="$1"
    trap - ERR
    log_error "Installationen avbröts i steget '$CURRENT_STEP' (exitkod $exit_code)."
    log_error "Full logg: $LOG_FILE"
    exit "$exit_code"
}
trap 'on_error "$?"' ERR

add_dotnet_to_path() {
    if ! macos_use_dotnet_11; then
        export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
    fi
    local profile
    case "${SHELL##*/}" in
        zsh) profile="$HOME/.zprofile" ;;
        bash) profile="$HOME/.bash_profile" ;;
        *) profile="$HOME/.profile" ;;
    esac
    local path_line='export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"'
    touch "$profile"
    if ! grep -Fqx "$path_line" "$profile"; then
        printf '\n%s\n' "$path_line" >> "$profile"
        log_info "Lade till .NET-verktygens sökväg i $profile."
    fi
}

has_dotnet_11() {
    macos_use_dotnet_11
}

install_dotnet_11() {
    CURRENT_STEP="installation av .NET 11 SDK"
    if has_dotnet_11; then
        add_dotnet_to_path
        log_info ".NET 11 SDK finns redan: $(dotnet --list-sdks | awk '$1 ~ /^11\./ { print $1; exit }')"
        return
    fi

    local installer
    installer="$(mktemp "${TMPDIR:-/tmp}/dotnet-install.XXXXXX")"
    log_info "Hämtar Microsofts .NET-installationsskript."
    if ! curl --fail --location --silent --show-error https://dot.net/v1/dotnet-install.sh --output "$installer"; then
        rm -f "$installer"
        return 1
    fi
    if ! bash "$installer" --channel 11.0 --install-dir "$HOME/.dotnet"; then
        rm -f "$installer"
        return 1
    fi
    rm -f "$installer"
    add_dotnet_to_path

    if ! has_dotnet_11; then
        log_error ".NET 11 SDK kunde inte verifieras efter installationen."
        return 1
    fi
    log_success ".NET 11 SDK är installerat."
}

select_storage_provider() {
    local selection="${PRIVATEKONOMI_STORAGE_PROVIDER:-}"
    local provider_file="$HOME/Library/Application Support/Privatekonomi/storage-provider"
    if [[ -z "$selection" && -f "$provider_file" ]]; then
        IFS= read -r selection < "$provider_file" || true
    fi
    if [[ -z "${PRIVATEKONOMI_STORAGE_PROVIDER:-}" && -t 0 ]]; then
        local answer
        printf 'Välj hur Privatekonomi ska spara dina data:\n' >&2
        printf '  1) SQLite - rekommenderat, data sparas i en lokal databasfil.\n' >&2
        printf '  2) JSON-filer - data sparas i separata filer.\n' >&2
        read -r -p "Datalagring [${selection:-Sqlite}]: " answer || answer=""
        selection="${answer:-$selection}"
    fi

    case "${selection:-Sqlite}" in
        1|[Ss][Qq][Ll][Ii][Tt][Ee]) printf 'Sqlite\n' ;;
        2|[Jj][Ss][Oo][Nn][Ff][Ii][Ll][Ee]|[Jj][Ss][Oo][Nn]) printf 'JsonFile\n' ;;
        *)
            log_error "Okänt eller ej stödd lagringsprovider '$selection'. Välj SQLite eller JSON-filer."
            return 1
            ;;
    esac
}

select_repository_branch() {
    local remote_refs branches branch default_branch answer
    remote_refs="$(git ls-remote --heads "$REPOSITORY_URL")" || {
        log_error "Kunde inte hämta grenar från GitHub."
        return 1
    }
    branches="$(printf '%s\n' "$remote_refs" | awk '$2 ~ /^refs\/heads\// { sub(/^refs\/heads\//, "", $2); print $2 }')"
    if [[ -z "$branches" ]]; then
        log_error "Inga grenar hittades i GitHub-repot."
        return 1
    fi

    if [[ -z "$INSTALL_BRANCH" && -t 0 && "$SKIP_INTERACTIVE" != true ]]; then
        default_branch="main"
        printf '%s\n' "$branches" | grep -Fxq -- "$default_branch" || default_branch="${branches%%$'\n'*}"
        printf 'Tillgängliga grenar i Privatekonomi:\n%s\n' "$branches" >&2
        read -r -p "Välj installationsgren [$default_branch] (Enter = $default_branch): " answer || answer=""
        INSTALL_BRANCH="${answer:-$default_branch}"
    fi
    INSTALL_BRANCH="${INSTALL_BRANCH:-main}"

    if ! printf '%s\n' "$branches" | grep -Fxq -- "$INSTALL_BRANCH"; then
        log_error "Grenen '$INSTALL_BRANCH' finns inte i GitHub-repot."
        return 1
    fi
    log_info "Vald installationsgren: $INSTALL_BRANCH"
}

prepare_install_source() {
    CURRENT_STEP="hämtning av vald installationsgren"
    git -C "$REPO_ROOT" fetch --no-tags "$REPOSITORY_URL" "refs/heads/$INSTALL_BRANCH"
    INSTALL_COMMIT="$(git -C "$REPO_ROOT" rev-parse FETCH_HEAD)"
    if [[ ! "$INSTALL_COMMIT" =~ ^[0-9a-f]{40}$ ]]; then
        log_error "Kunde inte verifiera committen för grenen '$INSTALL_BRANCH'."
        return 1
    fi

    INSTALL_SOURCE_ROOT="$(mktemp -d "${TMPDIR:-/tmp}/privatekonomi-install.XXXXXXXX")"
    INSTALL_SOURCE_ROOT="$(cd -- "$INSTALL_SOURCE_ROOT" && pwd -P)"
    git -C "$REPO_ROOT" archive "$INSTALL_COMMIT" | tar -x -C "$INSTALL_SOURCE_ROOT"
}

cleanup_install_source() {
    if [[ -n "$INSTALL_SOURCE_ROOT" && -d "$INSTALL_SOURCE_ROOT" ]]; then
        rm -rf -- "$INSTALL_SOURCE_ROOT"
    fi
}

show_help() {
    printf 'Användning: bash scripts/macos/install.sh [--branch BRANCH] [--skip-interactive]\n'
    printf '  --branch BRANCH       Installera senaste versionen från vald GitHub-gren.\n'
    printf '  --skip-interactive    Använd standardgrenen main utan frågor.\n'
    printf '  --help, -h            Visa denna hjälp.\n'
    printf '  PRIVATEKONOMI_BRANCH  Kan också användas för att ange gren.\n'
}

main() {
    while [[ $# -gt 0 ]]; do
        case "$1" in
            --branch)
                if [[ $# -lt 2 || -z "$2" ]]; then
                    log_error "--branch kräver ett grennamn."
                    return 2
                fi
                INSTALL_BRANCH="$2"
                shift
                ;;
            --branch=*)
                INSTALL_BRANCH="${1#--branch=}"
                if [[ -z "$INSTALL_BRANCH" ]]; then
                    log_error "--branch kräver ett grennamn."
                    return 2
                fi
                ;;
            --skip-interactive)
                SKIP_INTERACTIVE=true
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

    log_info "Installationslogg: $LOG_FILE"
    log_info "Separat bygglogg: $BUILD_LOG_FILE"
    CURRENT_STEP="kontroll av macOS"
    if [[ "$(uname -s)" != "Darwin" ]]; then
        log_error "Det här skriptet är endast avsett för macOS."
        return 1
    fi
    case "$(uname -m)" in
        arm64|x86_64) ;;
        *) log_error "Arkitekturen $(uname -m) stöds inte av .NET för macOS."; return 1 ;;
    esac

    CURRENT_STEP="kontroll av projektrot"
    if [[ ! -f "$REPO_ROOT/Privatekonomi.slnx" ]]; then
        log_error "Hittar inte Privatekonomi.slnx. Kör skriptet från ett klonat Privatekonomi-repository."
        return 1
    fi

    CURRENT_STEP="val av installationsgren"
    select_repository_branch
    trap cleanup_install_source EXIT
    prepare_install_source

    cd "$INSTALL_SOURCE_ROOT"
    install_dotnet_11

    CURRENT_STEP="NuGet-återställning"
    run_with_progress "Återställer NuGet-paket" "$BUILD_LOG_FILE" \
        dotnet restore "$INSTALL_SOURCE_ROOT/Privatekonomi.slnx"

    CURRENT_STEP="bygge av lösningen"
    run_with_progress "Bygger lösningen" "$BUILD_LOG_FILE" \
        dotnet build "$INSTALL_SOURCE_ROOT/Privatekonomi.slnx" --configuration Debug --verbosity minimal --nologo --no-restore

    local tessdata_dir="$INSTALL_SOURCE_ROOT/src/Privatekonomi.Web/tessdata"
    if [[ ! -s "$tessdata_dir/eng.traineddata" || ! -s "$tessdata_dir/swe.traineddata" ]]; then
        CURRENT_STEP="hämtning av OCR-språkdata"
        if ! bash "$INSTALL_SOURCE_ROOT/scripts/linux/setup-tesseract.sh"; then
            log_warning "OCR-språkdata kunde inte hämtas. Appen kan starta, men kvitto-OCR är inte tillgänglig förrän bash scripts/linux/setup-tesseract.sh körs."
        fi
    fi

    CURRENT_STEP="publicering och starttjänster"
    log_info "Publicerar Web och API och installerar bakgrundstjänster."
    local storage_provider
    storage_provider="$(select_storage_provider)"
    if [[ "$storage_provider" == "Sqlite" ]]; then
        log_info "Vald datalagring: SQLite."
    else
        log_info "Vald datalagring: JSON-filer."
    fi
    PRIVATEKONOMI_INSTALL_SOURCE_ROOT="$INSTALL_SOURCE_ROOT" \
        PRIVATEKONOMI_INSTALL_COMMIT="$INSTALL_COMMIT" \
        PRIVATEKONOMI_INSTALL_BRANCH="$INSTALL_BRANCH" \
        PRIVATEKONOMI_SOURCE_REPOSITORY="$REPO_ROOT" \
        PRIVATEKONOMI_CONFIG_ROOT="$INSTALL_SOURCE_ROOT" \
        bash "$SCRIPT_DIR/setup-services.sh" "$storage_provider"
    local installed_branch_file="$HOME/Library/Application Support/Privatekonomi/current/branch"
    local installed_branch=""
    if [[ -s "$installed_branch_file" ]]; then
        IFS= read -r installed_branch < "$installed_branch_file" || true
    fi
    if [[ "$installed_branch" != "$INSTALL_BRANCH" ]]; then
        log_error "Installerad gren blev '$installed_branch' i stället för vald gren '$INSTALL_BRANCH'. Kontrollera tjänsteinstallationen."
        return 1
    fi
    log_success "Verifierad installerad gren: $installed_branch."
    bash "$SCRIPT_DIR/setup-backup.sh"

    log_success "Installationen är klar."
    log_info "Gå till macOS-skriptkatalogen med: cd \"$REPO_ROOT/scripts/macos\""
    log_info "Starta appen med: cd \"$REPO_ROOT/scripts/macos\" && bash start.sh"
    log_info "Web och API startar automatiskt när du loggar in och kan nås på Macens IP-adress."
    log_info "Systemadministratörer kan uppdatera eller starta om appen via Drift-sidan i webbgränssnittet."
}

main "$@"
