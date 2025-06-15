#!/bin/bash

# Cấu hình
DOMAIN="schoolmedicalsystem.ddns.net"
EMAIL="minh.nguyenlevien@gmail.com"
VPS_IP="14.225.211.217"

echo "=== School Medical System SSL Setup ==="
echo "Domain: $DOMAIN"
echo "VPS IP: $VPS_IP"
echo "Email: $EMAIL"
echo ""

# Kiểm tra DNS
echo "1. Checking DNS resolution..."
RESOLVED_IP=$(nslookup $DOMAIN | grep "Address:" | tail -1 | awk '{print $2}')
echo "Resolved IP: $RESOLVED_IP"

if [ "$RESOLVED_IP" != "$VPS_IP" ]; then
    echo "Warning: DNS not pointing to correct IP"
    echo "Expected: $VPS_IP, Got: $RESOLVED_IP"
    echo "Continuing anyway..."
fi

# Tạo thư mục cần thiết
echo ""
echo "2. Creating directories..."
mkdir -p certbot/conf
mkdir -p certbot/www

# Dùng config HTTP đơn giản trước
echo ""
echo "3. Switching to HTTP-only config..."
cp nginx-simple.conf nginx-current.conf

# Cập nhật docker-compose để dùng config HTTP
sed -i 's|./nginx\.conf:/etc/nginx/nginx.conf|./nginx-current.conf:/etc/nginx/nginx.conf|g' docker-compose.yml

# Khởi động nginx và API để có thể validate domain
echo ""
echo "4. Starting services with HTTP config..."
docker-compose down
docker-compose up -d nginx schoolmedicalmanagementsystem.api

# Đợi services khởi động
echo "Waiting for services to start..."
sleep 30

# Kiểm tra services
echo ""
echo "5. Checking services status..."
docker-compose ps

# Test HTTP connection
echo ""
echo "6. Testing HTTP connection..."
curl -I http://$DOMAIN 2>/dev/null && echo "HTTP OK" || echo "HTTP failed"

# Lấy SSL certificate
echo ""
echo "7. Obtaining SSL certificate from Let's Encrypt..."
docker-compose run --rm certbot \
    certonly --webroot --webroot-path=/var/www/certbot \
    --email $EMAIL --agree-tos --no-eff-email \
    -d $DOMAIN

# Kiểm tra certificate
if [ -f "./certbot/conf/live/$DOMAIN/fullchain.pem" ]; then
    echo ""
    echo "SSL certificate obtained successfully!"
    
    # Chuyển về config SSL
    echo "8. Switching to HTTPS config..."
    cp nginx.conf nginx-current.conf
    docker-compose restart nginx
    
    # Đợi nginx restart
    sleep 10
    
    # Test HTTPS
    echo ""
    echo "9. Testing HTTPS connection..."
    curl -I https://$DOMAIN:7280 2>/dev/null && echo "HTTPS OK" || echo "HTTPS failed"
    
    echo ""
    echo "Setup completed successfully!"
    echo ""
    echo "Your API endpoints:"
    echo "HTTP:  http://$DOMAIN (redirects to HTTPS)"
    echo "HTTPS: https://$DOMAIN:7280"
    echo "Swagger: https://$DOMAIN:7280/swagger"
    echo "Health: https://$DOMAIN:7280/health"
    
    # Setup auto-renewal
    echo ""
    echo "10. Setting up auto-renewal..."
    (crontab -l 2>/dev/null; echo "0 12 * * * cd $(pwd) && docker-compose run --rm certbot renew --quiet && docker-compose restart nginx") | crontab -
    echo "Auto-renewal configured"
    
else
    echo ""
    echo "Failed to obtain SSL certificate"
    echo ""
    echo "Troubleshooting steps:"
    echo "1. Check if domain points to your IP: nslookup $DOMAIN"
    echo "2. Check if port 80 is accessible: curl http://$DOMAIN"
    echo "3. Check firewall: ufw status"
    echo "4. Check nginx logs: docker-compose logs nginx"
fi