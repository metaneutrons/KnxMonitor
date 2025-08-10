# Multi-stage Dockerfile for KnxMonitor
# Supports both development (with hot reload) and production builds

# Development stage with hot reload support
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS development

# Install required packages for development
RUN apt-get update && apt-get install -y \
    curl \
    git \
    procps \
    net-tools \
    iputils-ping \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for development
RUN groupadd -r knxmonitor && useradd -r -g knxmonitor -m -s /bin/bash knxmonitor

# Create NuGet directory with proper permissions
RUN mkdir -p /home/knxmonitor/.nuget && chown -R knxmonitor:knxmonitor /home/knxmonitor/.nuget

# Set working directory
WORKDIR /app

# Switch to non-root user
USER knxmonitor

# Set environment variables for development
ENV DOTNET_ENVIRONMENT=Development
ENV DOTNET_USE_POLLING_FILE_WATCHER=true
ENV DOTNET_WATCH_RESTART_ON_RUDE_EDIT=true
ENV DOTNET_WATCH_SUPPRESS_LAUNCH_BROWSER=true

# Development command with hot reload
# No need to copy files or build - bind mount provides source code
# dotnet watch will handle restore and build automatically
ENTRYPOINT ["dotnet", "watch", "run", "--"]

# Production build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

WORKDIR /src

# Copy project files and restore dependencies
COPY ["Directory.Packages.props", "./"]
COPY ["KnxMonitor/KnxMonitor.csproj", "./KnxMonitor/"]
RUN dotnet restore "KnxMonitor/KnxMonitor.csproj"

# Copy the rest of the code
COPY . .

# Build the application
RUN dotnet build "KnxMonitor/KnxMonitor.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "KnxMonitor/KnxMonitor.csproj" -c Release -o /app/publish --no-restore

# Production runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS production

# Install required runtime packages for KNX USB support
RUN apt-get update && apt-get install -y \
    libusb-1.0-0 \
    && rm -rf /var/lib/apt/lists/*

# Create non-root user for production
RUN groupadd -r knxmonitor && useradd -r -g knxmonitor -m -s /bin/bash knxmonitor

WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Change ownership to non-root user
RUN chown -R knxmonitor:knxmonitor /app

# Switch to non-root user
USER knxmonitor

# Set production environment
ENV DOTNET_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD pgrep -f "KnxMonitor" > /dev/null || exit 1

# Production entrypoint
ENTRYPOINT ["dotnet", "KnxMonitor.dll"]
