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
ln -s "$(command -v grep)" "$temp/bin/grep"
ln -s "$(command -v mkdir)" "$temp/bin/mkdir"

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
printf 'PRIVATEKONOMI_LAN_ACCESS=%s\nPRIVATEKONOMI_STORAGE_PROVIDER=%s\nStorage__Provider=%s\nStorage__ConnectionString=%s\nPRIVATEKONOMI_STORAGE_CONNECTION_STRING=%s\nARGS=%s\n' \
    "$PRIVATEKONOMI_LAN_ACCESS" "$PRIVATEKONOMI_STORAGE_PROVIDER" \
    "$Storage__Provider" "$Storage__ConnectionString" "$PRIVATEKONOMI_STORAGE_CONNECTION_STRING" "$*"
EOF
chmod +x "$temp/bin/uname" "$temp/bin/route" "$temp/bin/ipconfig" "$temp/bin/aspire"

PATH_SDKS=10.0.100 HOME_SDKS=11.0.100
export PATH_SDKS HOME_SDKS
PATH="$temp/bin"
export PATH
output="$(PRIVATEKONOMI_STORAGE_PROVIDER=Sqlite /bin/bash "$repo/scripts/macos/start.sh" --test-option)"
[[ "$output" == *"Web: http://localhost:5274"* ]]
[[ "$output" == *"http://192.168.1.23:5274"* ]]
[[ "$output" == *"PRIVATEKONOMI_LAN_ACCESS=true"* ]]
[[ "$output" == *"PRIVATEKONOMI_STORAGE_PROVIDER=Sqlite"* ]]
[[ "$output" == *"Storage__Provider=Sqlite"* ]]
[[ "$output" == *"privatekonomi.db"* ]]
[[ "$output" == *"ARGS=run --test-option"* ]]

output="$(PRIVATEKONOMI_STORAGE_PROVIDER=JsonFile /bin/bash "$repo/scripts/macos/start.sh")"
[[ "$output" == *"PRIVATEKONOMI_STORAGE_PROVIDER=JsonFile"* ]]
[[ "$output" == *"Storage__Provider=JsonFile"* ]]
[[ "$output" == *"Application Support/Privatekonomi/json"* ]]

if PRIVATEKONOMI_STORAGE_PROVIDER=PostgreSQL /bin/bash "$repo/scripts/macos/start.sh" >"$temp/postgres-output" 2>&1; then
    echo "Expected unsupported PostgreSQL selection to fail."
    exit 1
fi
grep -Fq 'saknar PostgreSQL EF Core-provider' "$temp/postgres-output"

echo "macOS startup selects .NET 11, runs Aspire attached, and configures supported storage providers."
