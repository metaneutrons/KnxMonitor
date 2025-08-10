# Runtime-only Docker image with multi-architecture support
# Application is built in GitHub Actions and copied as pre-built binary
FROM mcr.microsoft.com/dotnet/runtime-deps:9.0-alpine

# Build arguments
ARG VERSION=1.0.0
ARG BUILD_DATE
ARG VCS_REF
ARG TARGETARCH

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

# Copy pre-built application based on target architecture
COPY publish-${TARGETARCH}/ ./

# Set ownership and make executable
RUN chown -R knxmonitor:knxmonitor /app && \
    chmod +x ./KnxMonitor

# Switch to non-root user
USER knxmonitor

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD ./KnxMonitor --version || exit 1

# Labels
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
ENTRYPOINT ["./KnxMonitor"]
CMD ["--help"]
