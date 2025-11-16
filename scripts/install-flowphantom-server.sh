#!/bin/bash

set -e

echo "=============================================="
echo " Installing FlowPhantom.Server on Ubuntu VPS  "
echo "=============================================="

if [[ $EUID -ne 0 ]]; then
   echo "❌ Please run as root!"
   exit 1
fi

# 1. Установка зависимостей
echo "[1/7] Installing dependencies..."
apt update
apt install -y dotnet-runtime-8.0 iproute2 iptables iptables-persistent

# 2. Создаём директорию
echo "[2/7] Creating /opt/flowphantom..."
mkdir -p /opt/flowphantom
chmod 755 /opt/flowphantom

# 3. Копирование файлов DLL
echo "[3/7] Copying server DLL files..."
cp FlowPhantom.* /opt/flowphantom/

# 4. Создаём TUN phantom0
echo "[4/7] Creating phantom0 TUN interface..."

cat >/etc/systemd/system/phantom0.service <<EOF
[Unit]
Description=FlowPhantom TUN Interface
After=network.target

[Service]
Type=oneshot
ExecStart=/usr/sbin/ip tuntap add dev phantom0 mode tun
ExecStart=/usr/sbin/ip addr add 10.99.0.1/24 dev phantom0
ExecStart=/usr/sbin/ip link set phantom0 up
RemainAfterExit=yes

[Install]
WantedBy=multi-user.target
EOF

systemctl enable phantom0
systemctl start phantom0

# 5. Разрешаем маршрутизацию
echo "[5/7] Enabling IPv4 forwarding..."
sysctl -w net.ipv4.ip_forward=1
echo "net.ipv4.ip_forward=1" >/etc/sysctl.d/99-flowphantom-forwarding.conf

# 6. NAT
echo "[6/7] Configuring NAT (MASQUERADE)..."
iptables -t nat -A POSTROUTING -o eth0 -j MASQUERADE

netfilter-persistent save

# 7. Systemd unit для FlowPhantom.Server
echo "[7/7] Creating FlowPhantom systemd service..."

cat >/etc/systemd/system/flowphantom.service <<EOF
[Unit]
Description=FlowPhantom VPN Server
After=network.target phantom0.service
Wants=phantom0.service

[Service]
WorkingDirectory=/opt/flowphantom
ExecStart=/usr/bin/dotnet /opt/flowphantom/FlowPhantom.Server.dll
Restart=always
User=root

[Install]
WantedBy=multi-user.target
EOF

systemctl daemon-reload
systemctl enable flowphantom
systemctl restart flowphantom

echo "=============================================="
echo " FlowPhantom.Server installed successfully!"
echo "----------------------------------------------"
echo " Interface: phantom0  (10.99.0.1/24)"
echo " NAT: enabled"
echo " Systemd: active (systemctl status flowphantom)"
echo "=============================================="
