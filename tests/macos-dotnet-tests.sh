#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
system_path="$PATH"
mkdir -p "$HOME/.dotnet" "$temp/bin"
ln -s "$(command -v awk)" "$temp/bin/awk"
ln -s "$(command -v dirname)" "$temp/bin/dirname"

cat > "$temp/bin/dotnet" <<'EOF'
#!/bin/bash
printf '%s\n' "${PATH_SDKS:-10.0.100} [/system/dotnet/sdk]"
EOF
cat > "$HOME/.dotnet/dotnet" <<'EOF'
#!/bin/bash
printf '%s\n' "${HOME_SDKS:-10.0.100} [$HOME/.dotnet/sdk]"
EOF
chmod +x "$temp/bin/dotnet" "$HOME/.dotnet/dotnet"
export PATH="$temp/bin"
source "$repo/scripts/macos/dotnet.sh"

PATH_SDKS=10.0.100 HOME_SDKS=11.0.100
export PATH_SDKS HOME_SDKS
macos_use_dotnet_11
[[ "${DOTNET_ROOT:-}" == "$HOME/.dotnet" ]]
[[ "${PATH%%:*}" == "$HOME/.dotnet" ]]

unset DOTNET_ROOT
PATH_SDKS=11.0.100 HOME_SDKS=10.0.100
export PATH_SDKS HOME_SDKS
macos_use_dotnet_11
[[ -z "${DOTNET_ROOT:-}" ]]
[[ "${PATH%%:*}" == "$temp/bin" ]]

PATH="$system_path"
export PATH
cat > "$temp/bin/uname" <<'EOF'
#!/bin/bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/route" <<'EOF'
#!/bin/bash
printf 'interface: en0\n'
EOF
cat > "$temp/bin/ipconfig" <<'EOF'
#!/bin/bash
printf '192.168.1.23\n'
EOF
cat > "$temp/bin/aspire" <<'EOF'
#!/bin/bash
printf 'PRIVATEKONOMI_LAN_ACCESS=%s\nARGS=%s\n' "$PRIVATEKONOMI_LAN_ACCESS" "$*"
EOF
chmod +x "$temp/bin/uname" "$temp/bin/route" "$temp/bin/ipconfig" "$temp/bin/aspire"

PATH_SDKS=10.0.100 HOME_SDKS=11.0.100
export PATH_SDKS HOME_SDKS
PATH="$temp/bin"
export PATH
output="$(PATH="$temp/bin" /bin/bash "$repo/scripts/macos/start.sh" --test-option)"
[[ "$output" == *"Web: http://localhost:5274"* ]]
[[ "$output" == *"http://192.168.1.23:5274"* ]]
[[ "$output" == *"PRIVATEKONOMI_LAN_ACCESS=true"* ]]
[[ "$output" == *"ARGS=start --test-option"* ]]

echo ".NET SDK discovery selects an installed .NET 11 host."
