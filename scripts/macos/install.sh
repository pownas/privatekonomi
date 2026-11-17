#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
LOG_DIR="$HOME/Library/Logs/Privatekonomi"
mkdir -p "$LOG_DIR"
LOG_FILE="$LOG_DIR/install-$(date '+%Y%m%d-%H%M%S').log"
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
    export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"
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
    command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks | awk '$1 ~ /^11\./ { found=1 } END { exit !found }'
}

install_dotnet_11() {
    CURRENT_STEP="installation av .NET 11 SDK"
    add_dotnet_to_path
    if has_dotnet_11; then
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

install_aspire_cli() {
    CURRENT_STEP="installation av Aspire CLI"
    if command -v aspire >/dev/null 2>&1; then
        log_info "Aspire CLI finns redan: $(aspire --version)"
        return
    fi

    log_info "Installerar Aspire CLI som globalt .NET-verktyg."
    dotnet tool install --global Aspire.Cli
    add_dotnet_to_path
    if ! command -v aspire >/dev/null 2>&1; then
        log_error "Aspire CLI installerades inte i PATH. Öppna en ny terminal och kör skriptet igen."
        return 1
    fi
    log_success "Aspire CLI är installerat."
}

main() {
    log_info "Installationslogg: $LOG_FILE"
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

    cd "$REPO_ROOT"
    install_dotnet_11
    install_aspire_cli

    CURRENT_STEP="HTTPS-utvecklarcertifikat"
    if ! dotnet dev-certs https --check --quiet; then
        log_info "Skapar och litar på HTTPS-utvecklarcertifikatet (macOS kan visa en Keychain-bekräftelse)."
        dotnet dev-certs https --trust
    fi

    CURRENT_STEP="NuGet-återställning"
    dotnet restore Privatekonomi.slnx

    CURRENT_STEP="bygge av lösningen"
    dotnet build Privatekonomi.slnx --configuration Debug --no-restore

    local tessdata_dir="$REPO_ROOT/src/Privatekonomi.Web/tessdata"
    if [[ ! -s "$tessdata_dir/eng.traineddata" || ! -s "$tessdata_dir/swe.traineddata" ]]; then
        CURRENT_STEP="hämtning av OCR-språkdata"
        if ! bash "$REPO_ROOT/scripts/linux/setup-tesseract.sh"; then
            log_warning "OCR-språkdata kunde inte hämtas. Appen kan starta, men kvitto-OCR är inte tillgänglig förrän bash scripts/linux/setup-tesseract.sh körs."
        fi
    fi

    log_success "Installationen är klar."
    log_info "Gå till macOS-skriptkatalogen med: cd \"$REPO_ROOT/scripts/macos\""
    log_info "Starta appen med: cd \"$REPO_ROOT/scripts/macos\" && bash start.sh"
    log_info "Aspire Dashboard och appens adresser visas när startskriptet körs."
}

main "$@"
