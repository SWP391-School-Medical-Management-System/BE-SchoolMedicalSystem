#!/bin/bash

# Cấu hình thông tin No-IP
NOIP_USERNAME="pe37zxv"
NOIP_PASSWORD="rnp6UyUbBStB"
HOSTNAME="schoolmedicalsystem.ddns.net"

# Lấy IP hiện tại
CURRENT_IP=$(curl -s http://checkip.amazonaws.com)

echo "Current IP: $CURRENT_IP"

# Cập nhật IP lên No-IP
UPDATE_RESULT=$(curl -s "http://$NOIP_USERNAME:$NOIP_PASSWORD@dynupdate.no-ip.com/nic/update?hostname=$HOSTNAME&myip=$CURRENT_IP")

echo "Update result: $UPDATE_RESULT"

# Log kết quả
echo "$(date): Updated $HOSTNAME to $CURRENT_IP - Result: $UPDATE_RESULT" >> /var/log/noip-update.log