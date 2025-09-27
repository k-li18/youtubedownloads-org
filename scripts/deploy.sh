#!/bin/bash

# YouTube Downloader Web API Deployment Script
set -e

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

echo -e "${GREEN}Starting YouTube Downloader Web API deployment...${NC}"

# Check if Docker is installed
if ! command -v docker &> /dev/null; then
    echo -e "${RED}Error: Docker is not installed or not in PATH${NC}"
    exit 1
fi

# Check if Docker Compose is installed
if ! command -v docker-compose &> /dev/null; then
    echo -e "${RED}Error: Docker Compose is not installed or not in PATH${NC}"
    exit 1
fi

# Function to check if a port is available
check_port() {
    local port=$1
    if lsof -Pi :$port -sTCP:LISTEN -t >/dev/null 2>&1; then
        echo -e "${YELLOW}Warning: Port $port is already in use${NC}"
        return 1
    fi
    return 0
}

# Check required ports
echo "Checking required ports..."
check_port 80 || echo -e "${YELLOW}Port 80 is in use - Nginx may not start properly${NC}"
check_port 8080 || echo -e "${YELLOW}Port 8080 is in use - API may not start properly${NC}"
check_port 5432 || echo -e "${YELLOW}Port 5432 is in use - PostgreSQL may not start properly${NC}"

# Create necessary directories
echo "Creating necessary directories..."
mkdir -p logs ssl

# Build and start services
echo -e "${GREEN}Building and starting services...${NC}"
docker-compose down --remove-orphans
docker-compose build --no-cache
docker-compose up -d

# Wait for services to be ready
echo "Waiting for services to start..."
sleep 30

# Check service health
echo "Checking service health..."

# Check PostgreSQL
if docker-compose exec -T postgres pg_isready -U postgres > /dev/null 2>&1; then
    echo -e "${GREEN}✓ PostgreSQL is ready${NC}"
else
    echo -e "${RED}✗ PostgreSQL is not ready${NC}"
fi

# Check Redis
if docker-compose exec -T redis redis-cli ping | grep -q PONG; then
    echo -e "${GREEN}✓ Redis is ready${NC}"
else
    echo -e "${RED}✗ Redis is not ready${NC}"
fi

# Check API
if curl -f http://localhost:8080/api/health > /dev/null 2>&1; then
    echo -e "${GREEN}✓ API is ready${NC}"
else
    echo -e "${RED}✗ API is not ready${NC}"
    echo "Checking API logs..."
    docker-compose logs api
fi

# Check Nginx
if curl -f http://localhost/health > /dev/null 2>&1; then
    echo -e "${GREEN}✓ Nginx is ready${NC}"
else
    echo -e "${RED}✗ Nginx is not ready${NC}"
fi

echo -e "${GREEN}Deployment completed!${NC}"
echo
echo "Services are available at:"
echo "  - API: http://localhost:8080"
echo "  - Swagger UI: http://localhost:8080"
echo "  - Hangfire Dashboard: http://localhost:8080/hangfire"
echo "  - Nginx Proxy: http://localhost"
echo
echo "To view logs:"
echo "  docker-compose logs -f [service_name]"
echo
echo "To stop services:"
echo "  docker-compose down"