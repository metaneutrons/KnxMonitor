# Build arguments
ARG VERSION=1.0.0
ARG BUILD_DATE
ARG VCS_REF

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build
WORKDIR /src

# Copy project files
COPY ["KnxMonitor/KnxMonitor.csproj", "KnxMonitor/"]
COPY ["Directory.Build.props", "./"]
COPY ["GitVersion.props", "./"]
COPY ["GitVersion.yml", "./"]

# Restore dependencies
RUN dotnet restore "KnxMonitor/KnxMonitor.csproj"

# Copy source code
COPY . .

# Build and publish
WORKDIR "/src/KnxMonitor"
RUN dotnet publish "KnxMonitor.csproj" \
    --configuration Release \
    --runtime linux-musl-x64 \
    --self-contained true \
    --output /app/publish \
    -p:PublishTrimmed=true \
    -p:PublishSingleFile=true

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine AS runtime

# Install required packages for KNX monitoring
RUN apk add --no-cache \
    ca-certificates \
    tzdata \
    && update-ca-certificates

# Create non-root user
RUN addgroup -g 1001 -S knxmonitor \
    && adduser -S knxmonitor -u 1001 -G knxmonitor

# Set working directory
WORKDIR /app

# Copy published application
COPY --from=build /app/publish .

# Set ownership
RUN chown -R knxmonitor:knxmonitor /app

# Switch to non-root user
USER knxmonitor

# Add labels
LABEL org.opencontainers.image.title="KNX Monitor" \
      org.opencontainers.image.description="Enterprise-grade KNX/EIB bus monitoring and debugging tool" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.vendor="KnxMonitor" \
      org.opencontainers.image.licenses="GPL-3.0-or-later" \
      org.opencontainers.image.source="https://github.com/metaneutrons/KnxMonitor"

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ./KnxMonitor --health-check || exit 1

# Expose health check port
EXPOSE 8080

# Set entrypoint
ENTRYPOINT ["./KnxMonitor"]
