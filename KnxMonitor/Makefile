# KNX Monitor Makefile

.PHONY: help build run run-verbose run-docker clean test

# Default target
help:
	@echo "KNX Monitor - Available commands:"
	@echo ""
	@echo "  make build        - Build the application"
	@echo "  make run          - Run with default settings (knxd tunnel)"
	@echo "  make run-verbose  - Run with verbose logging"
	@echo "  make run-docker   - Run in Docker development mode"
	@echo "  make clean        - Clean build artifacts"
	@echo "  make test         - Run tests (when available)"
	@echo ""
	@echo "Examples:"
	@echo "  make run ARGS='--gateway 192.168.1.100'"
	@echo "  make run ARGS='--connection-type router --gateway 224.0.23.12'"
	@echo "  make run ARGS='--filter \"1/2/*\" --verbose'"

# Build the application
build:
	dotnet build

# Run with default settings
run: build
	dotnet run -- $(ARGS)

# Run with verbose logging
run-verbose: build
	dotnet run -- --verbose $(ARGS)

# Run in Docker development mode (part of SnapDog2 dev environment)
run-docker:
	cd .. && make dev

# View KNX Monitor logs
logs:
	cd .. && docker compose logs knx-monitor -f

# Run KNX Monitor separately in development mode
run-docker-standalone:
	docker compose --profile dev up knx-monitor-dev

# Build Docker images
docker-build-dev:
	docker build --target development -t knx-monitor:dev .

docker-build-prod:
	cd .. && docker build --target production -t knx-monitor:prod -f KnxMonitor/Dockerfile .

docker-build: docker-build-dev docker-build-prod

# Clean build artifacts
clean:
	dotnet clean
	rm -rf bin obj

# Run tests (placeholder for future tests)
test:
	@echo "Tests not implemented yet"

# Quick development commands
dev: run-verbose

# Monitor SnapDog2 traffic (assumes SnapDog2 is running)
monitor-snapdog: 
	dotnet run -- --gateway knxd --verbose

# Monitor with common filters
monitor-lights:
	dotnet run -- --filter "1/1/*" --verbose

monitor-blinds:
	dotnet run -- --filter "1/2/*" --verbose

monitor-heating:
	dotnet run -- --filter "1/3/*" --verbose
