#!/usr/bin/env bash
set -euo pipefail

repo="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
temp="$(mktemp -d)"
trap '/bin/rm -rf -- "$temp"' EXIT

source "$repo/scripts/common/run-with-progress.sh"
cat > "$temp/slow-command.sh" <<'EOF'
#!/usr/bin/env bash
printf 'compiler output\n'
sleep 2
EOF
cat > "$temp/failing-command.sh" <<'EOF'
#!/usr/bin/env bash
printf 'compiler failure\n'
exit 7
EOF
chmod +x "$temp/slow-command.sh" "$temp/failing-command.sh"

INSTALLER_PROGRESS_INTERVAL_SECONDS=1
export INSTALLER_PROGRESS_INTERVAL_SECONDS
output="$(run_with_progress 'Bygger testprojektet' "$temp/build.log" "$temp/slow-command.sh")"
[[ "$output" == *"compiler output"* ]]
[[ "$output" == *"pågår fortfarande"* ]]
[[ "$output" == *"slutfördes"* ]]
grep -Fq 'compiler output' "$temp/build.log"

if run_with_progress 'Verifierar felhantering' "$temp/build.log" "$temp/failing-command.sh" >"$temp/failure.log" 2>&1; then
    echo "Expected the failing command to return its non-zero status."
    exit 1
else
    exit_code=$?
fi
[[ "$exit_code" -eq 7 ]]
grep -Fq 'compiler failure' "$temp/failure.log"
grep -Fq 'Full utdata finns i:' "$temp/failure.log"

echo "Installer progress reports long-running commands and preserves their output and exit status."
