#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap 'rm -rf -- "$temp"' EXIT

export HOME="$temp/home"
support="$HOME/Library/Application Support/Privatekonomi"
mkdir -p "$support" "$support/json" "$temp/bin"

cat > "$temp/bin/sqlite3" <<'EOF'
#!/usr/bin/env bash
if [[ "$2" == "PRAGMA integrity_check;" ]]; then
    printf 'ok\n'
    exit 0
fi
destination="${2#*\"}"
destination="${destination%\"*}"
cp "$1" "$destination"
EOF
chmod +x "$temp/bin/sqlite3"
export PATH="$temp/bin:$PATH"

printf 'simulated database\n' > "$support/privatekonomi.db"
printf 'Sqlite\n' > "$support/storage-provider"
/bin/bash "$repo/scripts/macos/backup.sh"
sqlite_backup="$HOME/Library/Application Support/Privatekonomi-Backups/privatekonomi_$(date '+%Y-%m-%d').db"
[[ -s "$sqlite_backup" ]]
grep -Fq 'simulated database' "$sqlite_backup"

printf '{"sample":true}\n' > "$support/json/sample.json"
printf 'JsonFile\n' > "$support/storage-provider"
/bin/bash "$repo/scripts/macos/backup.sh"
json_backup="$HOME/Library/Application Support/Privatekonomi-Backups/privatekonomi_$(date '+%Y-%m-%d').tar.gz"
tar -tzf "$json_backup" | grep -Fq './sample.json'

printf 'Unsupported\n' > "$support/storage-provider"
if /bin/bash "$repo/scripts/macos/backup.sh" > "$temp/unsupported.log" 2>&1; then
    echo "Expected unsupported provider backup to fail."
    exit 1
fi
grep -Fq 'Okänd lagringsprovider' "$temp/unsupported.log"

echo "macOS SQLite and JSON backup paths work and reject unsupported providers."
