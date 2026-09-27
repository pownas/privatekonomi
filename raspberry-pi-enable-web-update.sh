#!/usr/bin/env bash
# Run once with sudo on a published, systemd-managed Raspberry Pi installation.
set -Eeuo pipefail

if [ "$(id -u)" -ne 0 ] || [ -z "${SUDO_USER:-}" ] || [ "$SUDO_USER" = root ]; then
    echo "Kör med sudo från installationskontot." >&2
    exit 1
fi
user="$SUDO_USER"
home_dir=$(getent passwd "$user" | cut -d: -f6)
if [[ ! "$user" =~ ^[a-z_][a-z0-9_-]*$ || ! "$home_dir" =~ ^/[a-zA-Z0-9/_-]+$ ]]; then
    echo "Ogiltigt installationskonto eller hemkatalog." >&2
    exit 1
fi
if ! grep -aq 'Raspberry Pi' /proc/device-tree/model; then
    echo "Endast Raspberry Pi stöds." >&2
    exit 1
fi
if [ "$(uname -m)" != aarch64 ]; then
    echo "Webbuppdatering kräver 64-bitars Raspberry Pi OS." >&2
    exit 1
fi
group=$(id -gn "$user")
for service in web api; do
    unit="/etc/systemd/system/privatekonomi-$service.service"
    if [ ! -f "$unit" ] || ! grep -Fq "WorkingDirectory=$home_dir/privatekonomi/publish/${service^}" "$unit" ||
       ! grep -Fq "User=$user" "$unit" ||
       [ ! -f "$home_dir/privatekonomi/publish/${service^}/Privatekonomi.${service^}" ]; then
        echo "Kräver publicerade privatekonomi-web och privatekonomi-api systemd-tjänster för $user." >&2
        exit 1
    fi
done

script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
install -o root -g root -m 755 "$script_dir/raspberry-pi-web-update.sh" /usr/local/bin/privatekonomi-web-update
install -d -o "$user" -g "$group" -m 700 "$home_dir/privatekonomi-update"
install -o "$user" -g "$group" -m 600 /dev/null "$home_dir/privatekonomi-update/enabled"

sudoers_temp=$(mktemp /etc/sudoers.d/.privatekonomi-web-update.XXXXXXXX)
trap 'rm -f -- "$sudoers_temp"' EXIT
cat > "$sudoers_temp" <<EOF
$user ALL=(root) NOPASSWD: /usr/bin/systemctl stop privatekonomi-web.service, /usr/bin/systemctl stop privatekonomi-api.service, /usr/bin/systemctl start privatekonomi-web.service, /usr/bin/systemctl start privatekonomi-api.service
EOF
chmod 440 "$sudoers_temp"
visudo -cf "$sudoers_temp"
mv -f -- "$sudoers_temp" /etc/sudoers.d/privatekonomi-web-update

mkdir -p /etc/systemd/system/privatekonomi-web.service.d
cat > /etc/systemd/system/privatekonomi-web.service.d/pi-update.conf <<EOF
[Service]
Environment=PiUpdate__Enabled=true
Environment=PiUpdate__StateDirectory=$home_dir/privatekonomi-update
EOF

cat > /etc/systemd/system/privatekonomi-update.service <<EOF
[Unit]
Description=Privatekonomi Raspberry Pi web update
After=network-online.target
Wants=network-online.target

[Service]
Type=oneshot
User=$user
ExecStart=/usr/local/bin/privatekonomi-web-update
TimeoutStartSec=30min

EOF

cat > /etc/systemd/system/privatekonomi-update.path <<EOF
[Unit]
Description=Watch for Privatekonomi web update requests

[Path]
PathExists=$home_dir/privatekonomi-update/request
Unit=privatekonomi-update.service

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable --now privatekonomi-update.path
systemctl restart privatekonomi-web.service
echo "Webbuppdatering aktiverad under Inställningar → Uppdatera Raspberry Pi."
