#!/bin/bash

echo "🚀 School Medical System - Stable Deployment Script"
echo "=================================================="

# Configuration
DOMAIN="schoolmedicalsystem.ddns.net"
EMAIL="minh.nguyenlevien@gmail.com"

# Pull latest code
echo "📥 Pulling latest code from GitHub..."
git pull

# Set executable permissions
echo "🔑 Setting executable permissions..."
chmod +x *.sh

# Stop existing containers and clean volumes to avoid conflicts
echo "🛑 Stopping existing containers..."
docker-compose down -v

# Clean up Docker
echo "🧹 Cleaning up Docker..."
docker system prune -f

# Check if SSL certificates exist
SSL_CERT_PATH="./certbot/conf/live/$DOMAIN/fullchain.pem"
if [ -f "$SSL_CERT_PATH" ]; then
    echo "🔐 SSL certificates found - will use HTTPS"
    USE_HTTPS=true
else
    echo "🌐 No SSL certificates - will use HTTP only"
    USE_HTTPS=false
fi

# Configure docker-compose based on SSL availability
if [ "$USE_HTTPS" = true ]; then
    echo "🔒 Configuring for HTTPS deployment..."
    # Ensure HTTPS config is used
    sed -i 's|./nginx-http\.conf:/etc/nginx/nginx.conf|./nginx-https.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
    sed -i 's|# - "443:443"|- "443:443"|g' docker-compose.yml
else
    echo "🌐 Configuring for HTTP deployment..."
    # Ensure HTTP config is used
    sed -i 's|./nginx-https\.conf:/etc/nginx/nginx.conf|./nginx-http.conf:/etc/nginx/nginx.conf|g' docker-compose.yml
    sed -i 's|- "443:443"|# - "443:443"|g' docker-compose.yml
fi

# Create necessary directories
mkdir -p certbot/conf
mkdir -p certbot/www

# Start services
echo "🐳 Starting services..."
docker-compose up -d --build

# Wait for services to be ready
echo "⏳ Waiting for services to start..."
sleep 45

# Check status
echo "📊 Checking services status..."
docker-compose ps

# Test the deployment
echo "🩺 Testing deployment..."
sleep 15

if [ "$USE_HTTPS" = true ]; then
    # Test HTTPS
    echo "🔐 Testing HTTPS connection..."
    HTTPS_STATUS=$(curl -o /dev/null -s -w "%{http_code}" https://$DOMAIN/health 2>/dev/null || echo "000")
    
    if [ "$HTTPS_STATUS" = "200" ]; then
        echo "✅ HTTPS deployment successful!"
        FINAL_PROTOCOL="https"
    else
        echo "⚠️  HTTPS test failed - checking nginx logs..."
        docker-compose logs nginx | tail -10
        echo ""
        echo "💡 Tip: If HTTPS fails, you can:"
        echo "   1. Check nginx logs: docker-compose logs nginx"
        echo "   2. Manually restart: docker-compose restart nginx"
        echo "   3. Fallback to HTTP: sed -i 's|nginx-https|nginx-http|g' docker-compose.yml && docker-compose restart nginx"
        FINAL_PROTOCOL="https-failed"
    fi
else
    # Test HTTP
    echo "🌐 Testing HTTP connection..."
    HTTP_STATUS=$(curl -o /dev/null -s -w "%{http_code}" http://$DOMAIN/health 2>/dev/null || echo "000")
    
    if [ "$HTTP_STATUS" = "200" ]; then
        echo "✅ HTTP deployment successful!"
        FINAL_PROTOCOL="http"
    else
        echo "❌ HTTP test failed - checking logs..."
        docker-compose logs nginx | tail -10
        echo ""
        docker-compose logs schoolmedicalmanagementsystem.api | tail -10
        FINAL_PROTOCOL="failed"
    fi
fi

# Final status
echo ""
echo "🎉 DEPLOYMENT COMPLETED!"
echo "========================"
echo "📊 Final Status:"
docker-compose ps
echo ""

if [ "$FINAL_PROTOCOL" = "https" ]; then
    echo "🌐 Your Application URLs:"
    echo "📍 Main Site: https://$DOMAIN"
    echo "📍 API Docs:  https://$DOMAIN/swagger"
    echo "📍 Health:    https://$DOMAIN/health"
    echo "📍 HTTP Redirect: http://$DOMAIN → https://$DOMAIN"
elif [ "$FINAL_PROTOCOL" = "http" ]; then
    echo "🌐 Your Application URLs:"
    echo "📍 Main Site: http://$DOMAIN"
    echo "📍 API Docs:  http://$DOMAIN/swagger"
    echo "📍 Health:    http://$DOMAIN/health"
    echo ""
    echo "💡 To enable HTTPS: ./setup-ssl.sh"
elif [ "$FINAL_PROTOCOL" = "https-failed" ]; then
    echo "⚠️  HTTPS deployment had issues, but certificates exist"
    echo "🌐 Try accessing:"
    echo "📍 HTTPS: https://$DOMAIN/health"
    echo "📍 HTTP:  http://$DOMAIN/health"
else
    echo "❌ Deployment failed - please check logs"
fi

echo ""
echo "🔧 Useful Commands:"
echo "  View nginx logs:  docker-compose logs nginx"
echo "  View API logs:    docker-compose logs schoolmedicalmanagementsystem.api"
echo "  Restart nginx:    docker-compose restart nginx"
echo "  Restart API:      docker-compose restart schoolmedicalmanagementsystem.api"
echo "  Full restart:     docker-compose restart"
echo "  Check status:     docker-compose ps"

echo ""
echo "✨ Deployment script completed!"
