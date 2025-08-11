#!/bin/bash
set -e

VERSION=${1:-"0.9.17"}
ARCH=${2:-"amd64"}
RUNTIME="linux-x64"

if [ "$ARCH" = "arm64" ]; then
    RUNTIME="linux-arm64"
fi

echo "Building .deb package for version $VERSION, architecture $ARCH"

# Create package directory structure
PKG_DIR="knxmonitor_${VERSION}_${ARCH}"
mkdir -p "$PKG_DIR/DEBIAN"
mkdir -p "$PKG_DIR/usr/bin"
mkdir -p "$PKG_DIR/usr/share/doc/knxmonitor"
mkdir -p "$PKG_DIR/usr/share/man/man1"

# Copy control file
cp packaging/debian/control "$PKG_DIR/DEBIAN/"
sed -i "s/Version: .*/Version: $VERSION/" "$PKG_DIR/DEBIAN/control"
sed -i "s/Architecture: .*/Architecture: $ARCH/" "$PKG_DIR/DEBIAN/control"

# Copy binary from release artifacts
if [ -f "artifacts/KnxMonitor-$RUNTIME/KnxMonitor" ]; then
    cp "artifacts/KnxMonitor-$RUNTIME/KnxMonitor" "$PKG_DIR/usr/bin/knxmonitor"
    chmod +x "$PKG_DIR/usr/bin/knxmonitor"
else
    echo "Error: Binary not found at artifacts/KnxMonitor-$RUNTIME/KnxMonitor"
    exit 1
fi

# Copy documentation
cp README.md "$PKG_DIR/usr/share/doc/knxmonitor/"
cp LICENSE "$PKG_DIR/usr/share/doc/knxmonitor/"

# Copy man page (should exist in source)
if [ -f "docs/knxmonitor.1" ]; then
    cp docs/knxmonitor.1 "$PKG_DIR/usr/share/man/man1/"
else
    echo "Warning: Man page not found at docs/knxmonitor.1"
fi

# Copy example CSV file if it exists
if [ -f "knx-addresses.csv" ]; then
    mkdir -p "$PKG_DIR/usr/share/doc/knxmonitor/examples"
    cp knx-addresses.csv "$PKG_DIR/usr/share/doc/knxmonitor/examples/"
fi

# Build the package
dpkg-deb --build "$PKG_DIR"

echo "Package built: ${PKG_DIR}.deb"
