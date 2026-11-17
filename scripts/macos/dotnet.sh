#!/usr/bin/env bash

macos_dotnet_has_sdk_11() {
    [[ -x "$1" ]] && "$1" --list-sdks 2>/dev/null | awk '$1 ~ /^11\./ { found=1 } END { exit !found }'
}

macos_use_dotnet_11() {
    local current_dotnet
    local candidate
    local candidates=()

    current_dotnet="$(command -v dotnet || true)"
    if [[ -n "$current_dotnet" ]]; then
        candidates+=("$current_dotnet")
    fi
    while IFS= read -r candidate; do
        [[ -n "$candidate" ]] && candidates+=("$candidate")
    done < <(type -a -p dotnet 2>/dev/null || true)
    candidates+=(
        "$HOME/.dotnet/dotnet"
        /usr/local/share/dotnet/dotnet
        /opt/homebrew/opt/dotnet/libexec/dotnet
        /usr/local/opt/dotnet/libexec/dotnet
    )

    for candidate in "${candidates[@]}"; do
        if macos_dotnet_has_sdk_11 "$candidate"; then
            if [[ "$candidate" == "$HOME/.dotnet/dotnet" ]]; then
                export DOTNET_ROOT="$HOME/.dotnet"
            fi
            export PATH="$(dirname "$candidate"):$HOME/.dotnet/tools:$PATH"
            return 0
        fi
    done

    return 1
}
