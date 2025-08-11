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

# Create man page
mkdir -p docs
cat > docs/knxmonitor.1 << EOF
.TH KNXMONITOR 1 "$(date +%Y-%m-%d)" "KNX Monitor $VERSION" "User Commands"
.SH NAME
knxmonitor \- KNX/EIB bus monitoring and debugging tool
.SH SYNOPSIS
.B knxmonitor
[\fIOPTION\fR]...
.SH DESCRIPTION
KNX Monitor is a command-line application for monitoring, debugging, and analyzing KNX/EIB building automation networks.
.SH OPTIONS
.TP
\fB\-g\fR, \fB\-\-gateway\fR \fIADDRESS\fR
KNX gateway address (default: knxd)
.TP
\fB\-p\fR, \fB\-\-port\fR \fIPORT\fR
KNX gateway port (default: 3671)
.TP
\fB\-c\fR, \fB\-\-connection\-type\fR \fITYPE\fR
Connection type: tunnel, router, or usb (default: tunnel)
.TP
\fB\-v\fR, \fB\-\-verbose\fR
Enable verbose logging
.TP
\fB\-l\fR, \fB\-\-logging\-mode\fR
Force logging mode instead of TUI
.TP
\fB\-\-csv\fR \fIFILE\fR
Load group addresses from CSV file
.TP
\fB\-f\fR, \fB\-\-filter\fR \fIPATTERN\fR
Filter group addresses (e.g., 1/2/*)
.TP
\fB\-\-help\fR
Show help information
.TP
\fB\-\-version\fR
Show version information
.SH EXAMPLES
.TP
Monitor KNX bus via IP tunneling:
.B knxmonitor \-g 192.168.1.100
.TP
Monitor with CSV decoding:
.B knxmonitor \-g 192.168.1.100 \-\-csv addresses.csv
.TP
Monitor specific group addresses:
.B knxmonitor \-g 192.168.1.100 \-f "1/2/*"
.SH SEE ALSO
.BR docker (1)
.SH AUTHOR
KNX Monitor Team
.SH REPORTING BUGS
Report bugs at: https://github.com/metaneutrons/KnxMonitor/issues
EOF

cp docs/knxmonitor.1 "$PKG_DIR/usr/share/man/man1/"

# Build the package
dpkg-deb --build "$PKG_DIR"

echo "Package built: ${PKG_DIR}.deb"
