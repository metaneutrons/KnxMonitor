# Multi-stage Dockerfile that builds from Git source
# Build stage - clones repo and builds binaries
FROM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

# Install git and GitVersion tool
RUN apk add --no-cache git
RUN dotnet tool install --global GitVersion.Tool
ENV PATH="$PATH:/root/.dotnet/tools"

# Build arguments
ARG GIT_REPOSITORY=https://github.com/metaneutrons/KnxMonitor.git
ARG GIT_REF=main
ARG TARGETARCH

# Clone the repository
WORKDIR /src
RUN git clone ${GIT_REPOSITORY} .
RUN git checkout ${GIT_REF}

# Calculate version using GitVersion
RUN /root/.dotnet/tools/dotnet-gitversion > /tmp/gitversion.json
RUN cat /tmp/gitversion.json

# Determine runtime based on target architecture and restore with RID
RUN if [ "$TARGETARCH" = "amd64" ]; then \
        RUNTIME="linux-x64"; \
    elif [ "$TARGETARCH" = "arm64" ]; then \
        RUNTIME="linux-arm64"; \
    else \
        echo "Unsupported architecture: $TARGETARCH" && exit 1; \
    fi && \
    echo "Building for runtime: $RUNTIME" && \
    dotnet restore KnxMonitor --runtime $RUNTIME && \
    dotnet publish KnxMonitor \
        --configuration Release \
        --runtime $RUNTIME \
        --self-contained false \
        --output /app \
        --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine

# Build arguments for labels
ARG VERSION
ARG BUILD_DATE
ARG VCS_REF

# Install runtime dependencies
RUN apk add --no-cache \
    ca-certificates \
    tzdata \
    && update-ca-certificates

# Create non-root user
RUN addgroup -g 1001 -S knxmonitor \
    && adduser -S knxmonitor -u 1001 -G knxmonitor

# Set working directory
WORKDIR /app

# Copy application from build stage
COPY --from=build /app ./
COPY --from=build /tmp/gitversion.json ./

# Set ownership
RUN chown -R knxmonitor:knxmonitor /app

# Switch to non-root user
USER knxmonitor

# Health check using dotnet
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD dotnet KnxMonitor.dll --version || exit 1

# Labels with version from GitVersion
LABEL org.opencontainers.image.title="KNX Monitor" \
      org.opencontainers.image.description="KNX/EIB bus monitoring and debugging tool built with modern .NET 9" \
      org.opencontainers.image.vendor="metaneutrons" \
      org.opencontainers.image.version="${VERSION}" \
      org.opencontainers.image.created="${BUILD_DATE}" \
      org.opencontainers.image.revision="${VCS_REF}" \
      org.opencontainers.image.source="https://github.com/metaneutrons/KnxMonitor" \
      org.opencontainers.image.documentation="https://github.com/metaneutrons/KnxMonitor/blob/main/README.md" \
      org.opencontainers.image.licenses="GPL-3.0"

# Default command
ENTRYPOINT ["dotnet", "KnxMonitor.dll"]
CMD ["--help"]
