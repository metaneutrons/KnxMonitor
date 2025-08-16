![KNX Monitor](https://img.shields.io/badge/KNX-Monitor-blue) ![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple) ![Docker](https://img.shields.io/badge/Docker-Ready-blue) ![Terminal.Gui V2](https://img.shields.io/badge/Terminal.Gui-V2-green)

# KNX Monitor

A visual, colorful command-line application for monitoring KNX/EIB bus activity with **Terminal.Gui V2 interface**.

## 🏆 Features

- 🎨 **Terminal.Gui V2 Interface**: Beautiful, interactive TUI with real-time updates
- 🔄 **Dual-Mode Architecture**: Automatic switching between TUI and logging modes
- 🌐 **Modern Web Interface**: Real-time browser-based monitoring with live updates and filtering
- 🔌 **Multiple Connection Types**: Support for IP Tunneling, IP Routing, and USB
- 🔍 **Real-time Monitoring**: Live display of all KNX bus activity with zero flickering
- 🎯 **Advanced Filtering**: Interactive filter dialogs with pattern matching
- 📊 **Rich Status Display**: Connection status, message count, and uptime tracking
- 🧠 **Advanced DPT Decoding**: Falcon SDK-powered data point type decoding with auto-detection
- 🎨 **Smart Value Formatting**: Context-aware display of decoded values (temperatures, percentages, etc.) with KNX group address database support
- 🐳 **Docker Ready**: Development and production Docker containers
- ⚡ **Hot Reload**: Development mode with automatic code reloading
- 🎹 **Keyboard Shortcuts**: Full keyboard navigation and shortcuts
- 📤 **Export Functionality**: Export messages to CSV format
- 🎨 **Color Coding**: Age-based and type-based color coding for messages

## 🧠 Advanced DPT Decoding

KNX Monitor features sophisticated Data Point Type (DPT) decoding using the Falcon SDK:

### Supported DPT Types

- **DPT 1.xxx**: Boolean values with context-aware formatting
  - `1.001` Switch: `On/Off`
  - `1.008` Up/Down: `Up/Down`
  - `1.009` Open/Close: `Open/Close`
- **DPT 5.xxx**: 8-bit unsigned values
  - `5.001` Scaling: `75%`
  - `5.003` Angle: `180°`
- **DPT 9.xxx**: 2-byte float values with proper units
  - `9.001` Temperature: `21.5°C`
  - `9.004` Illuminance: `1500 lux`
  - `9.005` Wind Speed: `5.2 m/s`
  - `9.006` Pressure: `1013 Pa`
  - `9.007` Humidity: `65.0%`
- **DPT 14.xxx**: 4-byte IEEE 754 float values
  - `14.019` Electric Current: `16.5 A`
  - `14.027` Frequency: `50.0 Hz`
  - `14.056` Power: `1500.0 W`
  - `14.076` Voltage: `230.0 V`

## 🖥️ Display Modes

### Interactive Mode (Terminal.Gui V2)

When running in an interactive terminal, KNX Monitor automatically launches the **Terminal.Gui V2 interface**:

### Web Interface

KNX Monitor includes a **modern web interface** that provides real-time monitoring through your browser:

- **Automatic activation**: Starts when running in containers, with redirected output, or using `-l/--logging-mode`
- **Manual activation**: Use `-l/--logging-mode` to force web interface mode
- **Default URL**: `http://localhost:8671` (configurable via `--http-port`)
- **Features**:
  - 📊 Live message table with real-time updates
  - 🔍 Advanced filtering (supports patterns like `1/2/*` and exact matches)
  - 📤 CSV export functionality
  - 📈 Connection status and message count display
  - 🎨 Clean, responsive design

**Access the web interface**: Open `http://localhost:8671` in your browser when running in non-TUI mode.

### Logging Mode (Console Output)

When output is redirected or running in containers, automatically switches to logging mode:

```plaintext
[14:32:15.123] Write 1.1.5 -> 1/2/1 = 75 (Normal)
[14:32:15.456] Read 1.1.10 -> 1/2/5 = Empty (Normal)
[14:32:15.789] Response 1.1.5 -> 1/2/5 = false (Normal)
```

## Quick Start

### Using .NET CLI

```bash
# Install dependencies
dotnet restore

# Run with default settings (connects to knxd via tunneling)
dotnet run

# Run with custom settings
dotnet run -- --connection-type tunnel --gateway 192.168.2.8 --verbose
```

## 🛠️ Command Line Options

| Option | Short | Description | Default | Example |
|--------|-------|-------------|---------|---------|
| `--gateway` | `-g` | KNX gateway IP address | `localhost` | `--gateway 192.168.1.100` |
| `--connection-type` | `-c` | Connection type | `tunnel` | `--connection-type router` |
| `--port` | `-p` | Gateway port | `3671` | `--port 3672` |
| `--multicast-address` | `-m` | Multicast address (router mode) | `224.0.23.12` | `--multicast-address 224.0.23.13` |
| `--verbose` | `-v` | **Enable verbose logging** | `false` | `--verbose` |
| `--filter` | `-f` | Group address filter | None | `--filter "1/2/*"` |
| `--csv` | | Path to ETS CSV export | None | `--csv addresses.csv` |
| `--xml` | | Path to ETS XML export | None | `--xml project.xml` |
| `--logging-mode` | | Logging mode | `auto` | `--logging-mode console` |
| `--http-port` | | Web UI port | `8080` | `--http-port 8671` |

### 🔍 Verbose Mode

The `--verbose` flag enables detailed logging for debugging and development:

**Normal Mode (default):**
```bash
dotnet run -- --gateway 192.168.1.100
# Clean output - only KNX monitoring messages
[22:01:24.022] Write 1.1.40 -> 5/1/40 = 22.80°C (Raw: 0891)
```

**Verbose Mode:**
```bash
dotnet run -- --gateway 192.168.1.100 --verbose
# Detailed logging including:
# - Full exception stack traces
# - ASP.NET Core Kestrel startup messages
# - Connection debugging information
# - Internal component logging
```

## ⌨️ Keyboard Shortcuts (Interactive Mode)

| Shortcut | Action | Description |
|----------|--------|-------------|
| **F1** | Help | Show keyboard shortcuts help |
| **F2** | Filter | Open filter dialog |
| **F3** | Clear | Clear all messages |
| **F5** | Refresh | Refresh display |
| **F9** | Export | Export messages to CSV |
| **F10** | Quit | Exit application |
| **Ctrl+C** | Quit | Exit application |
| **Ctrl+R** | Refresh | Refresh display |
| **Ctrl+F** | Filter | Open filter dialog |
| **Ctrl+E** | Export | Export messages |
| **Arrow Keys** | Navigate | Navigate table |
| **Page Up/Down** | Scroll | Scroll through messages |
| **Home/End** | Jump | Go to first/last message |

## 🎨 Color Coding (Interactive Mode)

- 🟢 **Green**: Recent messages (< 1 second), Write operations, Connected status
- 🟡 **Yellow**: Medium age messages (< 5 seconds), Response operations, Values
- 🟠 **Orange**: Older messages (< 30 seconds), Urgent priority
- 🔵 **Cyan**: Read operations, IP Tunneling connection
- 🟣 **Magenta**: IP Routing connection
- 🔴 **Red**: System priority, Disconnected status, Errors
- ⚪ **White/Dim**: Normal priority, Very old messages

## Command Line Options

| Option | Short | Description | Default |
|--------|-------|-------------|---------|
| `--connection-type` | `-c` | Connection type: `tunnel`, `router`, or `usb` | `tunnel` |
| `--gateway` | `-g` | KNX gateway address (required for tunnel/router) | `knxd` |
| `--port` | `-p` | KNX gateway port | `3671` |
| `--verbose` | `-v` | Enable verbose logging | `false` |
| `--filter` | `-f` | Filter group addresses (e.g., `1/2/*` or `1/2/3`) | None |
| `--csv` |  | Path to KNX group address CSV file (ETS export format) | None |
| `--xml` |  | Path to KNX group address XML export (KNX GA Export 01) | None |
| `--logging-mode` | `-l` | Force simple logging mode instead of TUI | `false` |
| `--http-port` |  | HTTP port for the web UI (non-TUI mode) | `8671` |
| `--test` | `-t` | Run DPT decoding tests and exit | `false` |

## Group Address Database Support

KNX Monitor supports loading group address databases from ETS exports in two formats:

### CSV Format (ETS Export)

- Use `--csv path/to/addresses.csv`
- Requires ETS export format with semicolon separation
- Expected columns: Main, Middle, Sub, Address, Central, Unfiltered, Description, DatapointType, Security

### XML Format (KNX GA Export 01)

- Use `--xml path/to/addresses.xml`
- Supports KNX GA XML export format with namespace `http://knx.org/xml/ga-export/01`
- Automatically extracts group addresses, names, descriptions, and DPT information
- Handles hierarchical group names with automatic splitting

**Note**: `--csv` and `--xml` options are mutually exclusive - use only one at a time.

## Usage Examples

### Basic Monitoring

```bash
# Monitor KNX bus via IP tunneling to knxd
dotnet run

# Monitor with verbose logging
dotnet run -- --verbose

# Monitor with group address database (CSV)
dotnet run -- --csv ~/Documents/knx-addresses.csv

# Monitor with group address database (XML)
dotnet run -- --xml ~/Documents/knx-groupaddress.xml

# Monitor specific group addresses
dotnet run -- --filter "1/2/*"

# Use web interface (force non-TUI mode)
dotnet run -- --logging-mode
# Then open http://localhost:8671 in your browser

# Web interface on custom port
dotnet run -- --logging-mode --http-port 9000
# Then open http://localhost:9000 in your browser
```

### Different Connection Types

```bash
# IP Tunneling (most common)
dotnet run -- --connection-type tunnel --gateway 192.168.1.100

# IP Routing (multicast)
dotnet run -- --connection-type router --gateway 224.0.23.12

# USB Interface
dotnet run -- --connection-type usb
```

### Monitor Production KNX Installation

```bash
# Connect to real KNX/IP gateway
dotnet run -- --connection-type tunnel --gateway 192.168.1.100 --port 3671

# Monitor only lighting controls
dotnet run -- --gateway 192.168.1.100 --filter "1/1/*"

# Monitor with IP routing (multicast)
dotnet run -- --connection-type router --gateway 224.0.23.12
```

## Development

### Dependencies

- **Knx.Falcon.Sdk**: KNX/EIB communication
- **Terminal.Gui V2**: Terminal User Interface
- **System.CommandLine**: Command-line argument parsing
- **Spectre.Console**: Beautiful console output for logging mode
- **Microsoft.Extensions.Hosting**: Dependency injection and hosting
- **Microsoft.Extensions.Logging**: Structured logging

### Building

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Build Docker images
docker build  .
```

## License

This project is part of SnapDog2 and is licensed under the GNU GPL v3.0.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Test with both development and production Docker builds
5. Submit a pull request

---

**Happy KNX Monitoring!** 🎉
