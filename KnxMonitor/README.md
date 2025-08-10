![KNX Monitor](https://img.shields.io/badge/KNX-Monitor-blue) ![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple) ![Docker](https://img.shields.io/badge/Docker-Ready-blue) ![Terminal.Gui V2](https://img.shields.io/badge/Terminal.Gui-V2-green)

# KNX Monitor

A visual, colorful command-line application for monitoring KNX/EIB bus activity with **award-worthy Terminal.Gui V2 interface**.

## 🏆 Features

- 🎨 **Terminal.Gui V2 Interface**: Beautiful, interactive TUI with real-time updates
- 🔄 **Dual-Mode Architecture**: Automatic switching between TUI and logging modes
- 🔌 **Multiple Connection Types**: Support for IP Tunneling, IP Routing, and USB
- 🔍 **Real-time Monitoring**: Live display of all KNX bus activity with zero flickering
- 🎯 **Advanced Filtering**: Interactive filter dialogs with pattern matching
- 📊 **Rich Status Display**: Connection status, message count, and uptime tracking
- 🧠 **Advanced DPT Decoding**: Falcon SDK-powered data point type decoding with auto-detection
- 🎨 **Smart Value Formatting**: Context-aware display of decoded values (temperatures, percentages, etc.) if KNX ETS CSV file provided
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
| `--test` | `-t` | Run DPT decoding tests and exit | `false` |

## Usage Examples

### Basic Monitoring

```bash
# Monitor KNX bus via IP tunneling to knxd
dotnet run

# Monitor with verbose logging
dotnet run -- --verbose

# Monitor specific group addresses
dotnet run -- --filter "1/2/*"
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
