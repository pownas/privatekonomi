#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap 'rm -rf -- "$temp"' EXIT

export HOME="$temp/home&user"
mkdir -p "$HOME" "$temp/bin"

cat > "$temp/bin/uname" <<'EOF'
#!/usr/bin/env bash
printf 'Darwin\n'
EOF
cat > "$temp/bin/plutil" <<'EOF'
#!/usr/bin/env bash
[[ "$1" == "-lint" && -s "$2" ]]
grep -Fq 'StartCalendarInterval' "$2"
grep -Fq '<integer>2</integer>' "$2"
grep -Fq '<integer>30</integer>' "$2"
grep -Fq 'home&amp;user' "$2"
EOF
cat > "$temp/bin/launchctl" <<'EOF'
#!/usr/bin/env bash
printf '%s\n' "$1" >> "$LAUNCHCTL_CALLS"
case "$1" in
    print) exit 0 ;;
    bootstrap|bootout) exit 0 ;;
    *) exit 1 ;;
esac
EOF
chmod +x "$temp/bin/"*
export PATH="$temp/bin:$PATH"
export LAUNCHCTL_CALLS="$temp/launchctl-calls"
export PLUTIL_COMMAND="$temp/bin/plutil"

bash "$repo/scripts/macos/setup-backup.sh"
plist="$HOME/Library/LaunchAgents/com.privatekonomi.backup.plist"
[[ -s "$plist" ]]
grep -Fq '<integer>2</integer>' "$plist"
grep -Fq '<integer>30</integer>' "$plist"
[[ "$(cat "$HOME/Library/Application Support/Privatekonomi/storage-provider")" == "Sqlite" ]]

call_count="$(grep -c '^bootstrap$' "$LAUNCHCTL_CALLS")"
bash "$repo/scripts/macos/setup-backup.sh"
[[ "$(grep -c '^bootstrap$' "$LAUNCHCTL_CALLS")" -eq "$call_count" ]]

echo "macOS LaunchAgent setup creates a nightly job and is idempotent."
