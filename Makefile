# =============================================================================
# Author: Vladyslav Zaiets | https://sarmkadan.com
# CTO & Software Architect
# =============================================================================

.PHONY: help build test clean restore run docker-build docker-run docker-down docs

help:
	@echo "dotnet-cqrs-eventsourcing Build Tasks"
	@echo "======================================"
	@echo "make help          - Show this help message"
	@echo "make restore       - Restore NuGet dependencies"
	@echo "make build         - Build project (Debug)"
	@echo "make build-release - Build project (Release)"
	@echo "make test          - Run unit tests"
	@echo "make run           - Run application"
	@echo "make clean         - Clean build artifacts"
	@echo "make pack          - Pack as NuGet package"
	@echo "make docker-build  - Build Docker image"
	@echo "make docker-run    - Run Docker Compose stack"
	@echo "make docker-down   - Stop Docker Compose stack"
	@echo "make docs          - Generate documentation"
	@echo "make format        - Format code with dotnet format"
	@echo "make lint          - Run code analysis"
	@echo ""

restore:
	@echo "Restoring NuGet dependencies..."
	dotnet restore

build: restore
	@echo "Building project (Debug)..."
	dotnet build

build-release: restore
	@echo "Building project (Release)..."
	dotnet build -c Release

test: build
	@echo "Running tests..."
	dotnet test --no-build --verbosity normal

run: build
	@echo "Running application..."
	dotnet run

clean:
	@echo "Cleaning build artifacts..."
	rm -rf bin/ obj/ .vs .vscode
	find . -type d -name "bin" -exec rm -rf {} +
	find . -type d -name "obj" -exec rm -rf {} +

pack: build-release
	@echo "Creating NuGet package..."
	dotnet pack -c Release -o ./nupkg

docker-build:
	@echo "Building Docker image..."
	docker build -t dotnet-cqrs:latest .

docker-run:
	@echo "Starting Docker Compose stack..."
	docker-compose up -d

docker-down:
	@echo "Stopping Docker Compose stack..."
	docker-compose down

docker-logs:
	@echo "Following Docker Compose logs..."
	docker-compose logs -f

format:
	@echo "Formatting code..."
	dotnet format

lint:
	@echo "Running code analysis..."
	dotnet analyze

examples:
	@echo "Running example programs..."
	@cd examples/01-BasicAccount && dotnet run
	@echo ""
	@cd examples/02-EventHandling && dotnet run
	@echo ""
	@cd examples/03-Projections && dotnet run

docs:
	@echo "Documentation files are in docs/ directory"
	@ls -la docs/

all: clean restore build test pack
	@echo "Build complete!"

.DEFAULT_GOAL := help
