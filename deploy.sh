#!/bin/bash

echo "🚀 School Medical System - Complete Deployment Script"
echo "===================================================="

# Configuration
DOMAIN="schoolmedicalsystem.ddns.net"
EMAIL="minh.nguyenlevien@gmail.com"
VPS_IP="14.225.211.217"

# Pull latest code
echo "📥 Pulling latest code from GitHub..."
git pull

# Set executable permissions
echo "🔑 Setting executable permissions..."
chmod +x *.sh

# Stop existing containers
echo "🛑 Stopping existing containers..."
docker-compose down

# Clean up Docker
echo "🧹 Cleaning up Docker..."
docker system prune -f

# Check if SSL certificates exist
SSL_CERT_PATH="./certbot/conf/live/$DOMAIN/fullchain.pem"
if [ -f "$SSL_CERT_PATH" ]; then
    echo "🔐 SSL certificates found - deploying with HTTPS"
    USE_SSL=true
else
    echo "⚠️  SSL certificates not found - will setup SSL automatically"
    USE_SSL=false
fi

# Phase 1: Deploy with HTTP first
echo ""
echo "📋 Phase 1: HTTP Deployment"
echo "=========================="

# Configure for HTTP
echo "🌐 Configuring for HTTP..."
sed -i 's|./nginx-https\.conf:/etc/nginx/nginx.conf|./nginx-http.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
sed -i 's|- "443:443"|# - "443:443"|g' docker-compose.yml

# Create certbot directories
mkdir -p certbot/conf
mkdir -p certbot/www

# Start services with HTTP
echo "🐳 Starting services with HTTP..."
docker-compose up -d --build

# Wait for services
echo "⏳ Waiting for services to start..."
sleep 30

# Check HTTP status
echo "📊 Checking services status..."
docker-compose ps

# Test HTTP
echo "🩺 Testing HTTP connection..."
sleep 10
HTTP_STATUS=$(curl -o /dev/null -s -w "%{http_code}" http://$DOMAIN/health 2>/dev/null || echo "000")

if [ "$HTTP_STATUS" = "200" ]; then
    echo "✅ HTTP deployment successful!"
    
    # Phase 2: Setup SSL if not exists
    if [ "$USE_SSL" = false ]; then
        echo ""
        echo "📋 Phase 2: SSL Setup"
        echo "==================="
        
        echo "🔐 Obtaining SSL certificate from Let's Encrypt..."
        
        # Get SSL certificate
        SSL_RESULT=$(docker-compose run --rm certbot \
            certonly \
            --webroot \
            --webroot-path=/var/www/certbot \
            --email $EMAIL \
            --agree-tos \
            --no-eff-email \
            --keep-until-expiring \
            --rsa-key-size 2048 \
            -d $DOMAIN \
            --non-interactive 2>&1)
        
        # Check if SSL was successful
        if [ -f "$SSL_CERT_PATH" ]; then
            echo "✅ SSL certificate obtained successfully!"
            
            # Phase 3: Switch to HTTPS
            echo ""
            echo "📋 Phase 3: HTTPS Configuration"
            echo "==============================="
            
            echo "🔒 Switching to HTTPS configuration..."
            sed -i 's|./nginx-http\.conf:/etc/nginx/nginx.conf|./nginx-https.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
            sed -i 's|# - "443:443"|- "443:443"|g' docker-compose.yml
            
            # Restart nginx with HTTPS
            echo "🔄 Restarting nginx with HTTPS..."
            docker-compose restart nginx
            
            # Wait for nginx restart
            sleep 10
            
            # Test HTTPS
            echo "🔐 Testing HTTPS connection..."
            HTTPS_STATUS=$(curl -o /dev/null -s -w "%{http_code}" https://$DOMAIN/health 2>/dev/null || echo "000")
            
            if [ "$HTTPS_STATUS" = "200" ]; then
                echo "✅ HTTPS deployment successful!"
                
                # Setup auto-renewal
                echo "🔄 Setting up SSL auto-renewal..."
                (crontab -l 2>/dev/null; echo "0 12 * * * cd $(pwd) && docker-compose run --rm certbot renew --quiet && docker-compose restart nginx") | crontab -
                echo "✅ Auto-renewal configured!"
                
                FINAL_PROTOCOL="https"
            else
                echo "⚠️  HTTPS test failed, but HTTP is working"
                echo "   You can access the site via HTTP and troubleshoot HTTPS later"
                FINAL_PROTOCOL="http"
            fi
        else
            echo "⚠️  SSL certificate setup failed"
            echo "   Continuing with HTTP deployment"
            echo "   You can manually run './setup-ssl.sh' later"
            FINAL_PROTOCOL="http"
        fi
    else
        echo ""
        echo "📋 Phase 2: HTTPS Configuration (certificates exist)"
        echo "=================================================="
        
        # Switch to HTTPS since certificates exist
        echo "🔒 Switching to HTTPS configuration..."
        sed -i 's|./nginx-http\.conf:/etc/nginx/nginx.conf|./nginx-https.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
        sed -i 's|# - "443:443"|- "443:443"|g' docker-compose.yml
        
        # Restart nginx with HTTPS
        docker-compose restart nginx
        sleep 10
        
        # Test HTTPS
        HTTPS_STATUS=$(curl -o /dev/null -s -w "%{http_code}" https://$DOMAIN/health 2>/dev/null || echo "000")
        
        if [ "$HTTPS_STATUS" = "200" ]; then
            echo "✅ HTTPS deployment successful!"
            FINAL_PROTOCOL="https"
        else
            echo "⚠️  HTTPS test failed, falling back to HTTP"
            # Fallback to HTTP
            sed -i 's|./nginx-https\.conf:/etc/nginx/nginx.conf|./nginx-http.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
            sed -i 's|- "443:443"|# - "443:443"|g' docker-compose.yml
            docker-compose restart nginx
            FINAL_PROTOCOL="http"
        fi
    fi
    
else
    echo "❌ HTTP deployment failed!"
    echo "   Please check the logs: docker-compose logs nginx"
    echo "   And API logs: docker-compose logs schoolmedicalmanagementsystem.api"
    exit 1
fi

# Final status
echo ""
echo "🎉 DEPLOYMENT COMPLETED!"
echo "========================"
echo "📊 Final Status:"
docker-compose ps
echo ""
echo "🌐 Your Application URLs:"
if [ "$FINAL_PROTOCOL" = "https" ]; then
    echo "📍 Main Site: https://$DOMAIN"
    echo "📍 API Docs:  https://$DOMAIN/swagger"
    echo "📍 Health:    https://$DOMAIN/health"
    echo "📍 HTTP Redirect: http://$DOMAIN → https://$DOMAIN"
else
    echo "📍 Main Site: http://$DOMAIN"
    echo "📍 API Docs:  http://$DOMAIN/swagger"
    echo "📍 Health:    http://$DOMAIN/health"
fi

echo ""
echo "🔧 Useful Commands:"
echo "  View logs: docker-compose logs [service_name]"
echo "  Restart:   docker-compose restart [service_name]"
echo "  Status:    docker-compose ps"
echo "  SSL Setup: ./setup-ssl.sh (if needed)"

echo ""
echo "✨ Deployment script completed successfully!"
echo "   Your School Medical System is now running!"