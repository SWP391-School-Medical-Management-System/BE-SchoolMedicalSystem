#!/bin/bash

echo "Deploying School Medical System..."

# Pull latest code
echo "Pulling latest code from GitHub..."
git pull

# Set executable permissions
echo "Setting executable permissions..."
chmod +x *.sh

# Stop existing containers
echo "Stopping existing containers..."
docker-compose down

# Clean up Docker
echo "Cleaning up Docker..."
docker system prune -f

# Start services
echo "Starting services..."
docker-compose up -d --build

# Wait for services
echo "Waiting for services to start..."
sleep 30

# Check status
echo "Checking services status..."
docker-compose ps

# Test health endpoint
echo "Testing health endpoint..."
sleep 10
curl -I http://schoolmedicalsystem.ddns.net/health && echo "Health check passed" || echo "Health check failed"

echo ""
echo "Deployment completed!"
echo "HTTP:  http://schoolmedicalsystem.ddns.net"
echo "HTTPS: https://schoolmedicalsystem.ddns.net (if SSL configured)"
echo "Swagger: https://schoolmedicalsystem.ddns.net/swagger"