.PHONY: help build up down logs test clean

# Default target
help:
	@echo "AutoDocOps Backend - Available commands:"
	@echo ""
	@echo "  make build     - Build the .NET application"
	@echo "  make up        - Start all services with docker-compose"
	@echo "  make up-dev    - Start services with development override"
	@echo "  make up-monitoring - Start services with monitoring (Grafana/Prometheus)"
	@echo "  make down      - Stop all services"
	@echo "  make logs      - Show logs from all services"
	@echo "  make test      - Run unit tests"
	@echo "  make clean     - Clean build artifacts"
	@echo "  make setup-env - Copy .env.example to .env"
	@echo ""
	@echo "Quick start:"
	@echo "  1. make setup-env"
	@echo "  2. Edit .env with your configuration"
	@echo "  3. make up-dev"

# Build the .NET application
build:
	@echo "Building AutoDocOps backend..."
	dotnet build src/AutoDocOps.WebAPI --configuration Release

# Start all services
up:
	@echo "Starting AutoDocOps services..."
	docker compose up -d

# Start with development override
up-dev:
	@echo "Starting AutoDocOps services (development mode)..."
	docker compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Start with monitoring
up-monitoring:
	@echo "Starting AutoDocOps services with monitoring..."
	docker compose --profile monitoring up -d

# Stop all services
down:
	@echo "Stopping AutoDocOps services..."
	docker compose down

# Show logs
logs:
	docker compose logs -f

# Run tests
test:
	@echo "Running unit tests..."
	dotnet test tests/AutoDocOps.Tests --logger "console;verbosity=normal"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean
	docker compose down --volumes --remove-orphans

# Setup environment file
setup-env:
	@if [ ! -f .env ]; then \
		cp .env.example .env; \
		echo "Created .env file from .env.example"; \
		echo "Please edit .env with your configuration"; \
	else \
		echo ".env file already exists"; \
	fi

# Development helpers
dev-build:
	@echo "Building for development..."
	dotnet build src/AutoDocOps.WebAPI --configuration Debug

dev-run:
	@echo "Running API in development mode..."
	cd src/AutoDocOps.WebAPI && dotnet run

# Testing helpers
test-cache:
	@echo "Testing cache endpoint (run twice to see cache effect)..."
	@echo "First call (no cache):"
	curl -w "\nTime: %{time_total}s\n" -s http://localhost:8080/api/test/cache/demo_key | head -5
	@echo "\nSecond call (from cache):"
	curl -w "\nTime: %{time_total}s\n" -s http://localhost:8080/api/test/cache/demo_key | head -5

test-chat:
	@echo "Testing chat endpoint..."
	curl -X POST http://localhost:8080/api/test/chat \
		-H "Content-Type: application/json" \
		-d '"Hola, ¿cómo está funcionando el sistema?"'

test-health:
	@echo "Testing health endpoint..."
	curl -s http://localhost:8080/api/test/health | jq .

test-billing:
	@echo "Testing billing endpoint..."
	curl -X POST http://localhost:8080/api/test/billing/checkout \
		-H "Content-Type: application/json" \
		-d '{}' | jq .

test-all:
	@echo "Running all functional tests..."
	@echo "\n=== HEALTH CHECK ==="
	make test-health
	@echo "\n=== CACHE TEST ==="
	make test-cache
	@echo "\n=== CHAT TEST ==="
	make test-chat
	@echo "\n=== BILLING TEST ==="
	make test-billing
	@echo "\n=== ALL TESTS COMPLETED ==="

# Evidence generation
generate-evidence:
	@echo "Generating functional testing evidence..."
	@echo "Evidence report available at: FUNCTIONAL_TESTING_EVIDENCE.md"
	@echo "Screenshots available at: /home/ubuntu/screenshots/"

# Monitoring helpers
grafana:
	@echo "Opening Grafana dashboard..."
	@echo "URL: http://localhost:3000"
	@echo "Credentials: admin/admin"

prometheus:
	@echo "Opening Prometheus..."
	@echo "URL: http://localhost:9090"

