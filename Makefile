.PHONY: help build build-docker up up-dev up-prod up-monitoring down logs test clean setup-env migrate-db

# Default target
help:
	@echo "AutoDocOps Backend - Available commands:"
	@echo ""
	@echo "Build commands:"
	@echo "  make build        - Build the .NET application"
	@echo "  make build-docker - Build Docker images"
	@echo ""
	@echo "Run commands:"
	@echo "  make up           - Start all services with docker-compose"
	@echo "  make up-dev       - Start services with development override"
	@echo "  make up-prod      - Start services for production"
	@echo "  make up-monitoring - Start services with monitoring (Grafana/Prometheus)"
	@echo "  make down         - Stop all services"
	@echo ""
	@echo "Development commands:"
	@echo "  make logs         - Show logs from all services"
	@echo "  make test         - Run unit tests"
	@echo "  make test-coverage - Run tests with coverage report"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make setup-env    - Copy .env.example to .env"
	@echo "  make migrate-db   - Run database migrations"
	@echo ""
	@echo "Quick start:"
	@echo "  1. make setup-env"
	@echo "  2. Edit .env with your configuration"
	@echo "  3. make up-dev"

# Build the .NET application
build:
	@echo "Building AutoDocOps backend..."
	dotnet build src/AutoDocOps.WebAPI --configuration Release

# Build Docker images
build-docker:
	@echo "Building Docker images..."
	docker compose build

# Start all services
up:
	@echo "Starting AutoDocOps services..."
	docker compose up -d

# Start with development override
up-dev:
	@echo "Starting AutoDocOps services (development mode)..."
	docker compose -f docker-compose.yml -f docker-compose.override.yml up -d

# Start for production
up-prod:
	@echo "Starting AutoDocOps services (production mode)..."
	docker compose -f docker-compose.prod.yml up -d

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

# Show logs for specific service
logs-webapi:
	docker compose logs -f webapi

logs-postgres:
	docker compose logs -f postgres

logs-redis:
	docker compose logs -f redis

# Run tests
test:
	@echo "Running unit tests..."
	dotnet test tests/AutoDocOps.Tests --logger "console;verbosity=normal"

# Run tests with coverage
test-coverage:
	@echo "Running tests with coverage (OpenCover + JSON) ..."
	dotnet test --no-build --configuration Release \
		/p:CollectCoverage=true \
		/p:CoverletOutputFormat=opencover,json \
		/p:CoverletOutput=tests/TestResults/coverage/ \
		/p:Threshold=80 \
		/p:ThresholdType=line \
		/p:ThresholdStat=total \
		--settings tests/AutoDocOps.Tests/AutoDocOps.runsettings \
		--logger "console;verbosity=minimal"
	@echo "Normalizing coverage report path..."
	@LAST_FILE=$$(ls -1t tests/AutoDocOps.Tests/TestResults/*/coverage.opencover.xml | head -1); \
		mkdir -p tests/TestResults/coverage && cp $$LAST_FILE tests/TestResults/coverage/coverage.opencover.xml; \
		echo "Using $$LAST_FILE as source coverage file";
	@echo "Generating HTML coverage report..."
	dotnet tool run reportgenerator -reports:tests/TestResults/coverage/coverage.opencover.xml -targetdir:tests/TestResults/report -reporttypes:HtmlSummary
	@echo "HTML summary at tests/TestResults/report/index.html"

test-coverage-html: test-coverage ## Alias

test-fast:
	@echo "Running tests (minimal output)..."
	dotnet test --no-build --configuration Debug --settings tests/AutoDocOps.Tests/AutoDocOps.runsettings --logger "console;verbosity=minimal"

# Clean build artifacts
clean:
	@echo "Cleaning build artifacts..."
	dotnet clean
	docker compose down --volumes --remove-orphans

# Database migrations
migrate-db:
	@echo "Running database migrations..."
	cd src/AutoDocOps.WebAPI && dotnet ef database update

# Setup environment file
setup-env:
	@echo "Setting up environment file..."
	@if not exist .env (copy .env.example .env && echo Created .env file from .env.example && echo Please edit .env with your configuration) else (echo .env file already exists)

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

