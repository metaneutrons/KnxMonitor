![KNX Monitor](https://img.shields.io/badge/KNX-Monitor-blue) ![.NET 9.0](https://img.shields.io/badge/.NET-9.0-purple) ![Docker](https://img.shields.io/badge/Docker-Ready-blue) ![Terminal.Gui V2](https://img.shields.io/badge/Terminal.Gui-V2-green)

# KNX Monitor

A visual, colorful command-line application for monitoring KNX/EIB bus activity with **award-worthy Terminal.Gui V2 interface**. Perfect for debugging SnapDog2's KNX integration and understanding KNX bus traffic.

## 🏆 Award-Worthy Features

- 🎨 **Enterprise Terminal.Gui V2 Interface**: Beautiful, interactive TUI with real-time updates
- 🔄 **Dual-Mode Architecture**: Automatic switching between TUI and logging modes
- 🔌 **Multiple Connection Types**: Support for IP Tunneling, IP Routing, and USB
- 🔍 **Real-time Monitoring**: Live display of all KNX bus activity with zero flickering
- 🎯 **Advanced Filtering**: Interactive filter dialogs with pattern matching
- 📊 **Rich Status Display**: Connection status, message count, and uptime tracking
- 🧠 **Advanced DPT Decoding**: Falcon SDK-powered data point type decoding with auto-detection
- 🎨 **Smart Value Formatting**: Context-aware display of decoded values (temperatures, percentages, etc.)
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

### Auto-Detection

The monitor automatically detects DPT types based on:

- Data length and patterns
- Value ranges (e.g., temperature ranges for DPT 9.001)
- Context clues from the data content

### Test DPT Decoding

```bash
# Run built-in DPT decoding tests
./KnxMonitor --test
```

Example output:

```
=== DPT Decoding Tests ===

--- DPT 1.001 (Boolean Switch) Tests ---
Data: 01 -> Value: True -> Formatted: On
Data: 00 -> Value: False -> Formatted: Off

--- DPT 9.001 (Temperature) Tests ---
Data: 086E -> Value: 2,2 -> Formatted: 2,2°C
Data: 860C -> Value: -5 -> Formatted: -5,0°C
```

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

### Using Docker (Recommended)

```bash
# Start SnapDog2 development environment (includes KNX Monitor)
cd /path/to/snapdog
make dev

# View KNX Monitor output
docker compose logs knx-monitor -f

# Or start KNX Monitor separately in development mode
docker compose --profile dev up knx-monitor-dev

# Production mode
docker compose --profile prod up knx-monitor
```

### Using .NET CLI

```bash
# Install dependencies
dotnet restore

# Run with default settings (connects to knxd via tunneling)
dotnet run

# Run with custom settings
dotnet run -- --connection-type tunnel --gateway knxd --port 3671 --verbose
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

### Docker Examples

```bash
# Development with hot reload
docker compose --profile dev up knx-monitor-dev

# Production
docker compose --profile prod up knx-monitor

# Build from parent directory (required for central package management)
cd /path/to/snapdog
docker build --target development -t knx-monitor:dev -f KnxMonitor/Dockerfile .
docker build --target production -t knx-monitor:prod -f KnxMonitor/Dockerfile .

# Monitor with custom gateway
docker run --rm -it \
  --network snapdog_snapdog-dev \
  knx-monitor:prod \
  --connection-type tunnel --gateway knxd --verbose

# Monitor with filtering
docker run --rm -it \
  --network snapdog_snapdog-dev \
  knx-monitor:prod \
  --filter "1/1/*" --verbose
```

### Advanced Usage Examples

#### Monitor SnapDog2 KNX Traffic

```bash
# Start SnapDog2 development environment
cd /path/to/snapdog
make dev

# In another terminal, start KNX Monitor
cd /path/to/snapdog/KnxMonitor
dotnet run -- --verbose

# Now trigger KNX operations in SnapDog2 and watch the traffic!
```

#### Monitor Production KNX Installation

```bash
# Connect to real KNX/IP gateway
dotnet run -- --connection-type tunnel --gateway 192.168.1.100 --port 3671

# Monitor only lighting controls
dotnet run -- --gateway 192.168.1.100 --filter "1/1/*"

# Monitor with IP routing (multicast)
dotnet run -- --connection-type router --gateway 224.0.23.12
```

#### USB KNX Interface

```bash
# Auto-detect USB KNX interface
dotnet run -- --connection-type usb
```

## Troubleshooting

### Connection Issues

- **"Gateway address is required"**: Specify `--gateway` for tunnel/router connections
- **"No KNX USB devices found"**: Ensure USB device is connected and accessible
- **"Connection refused"**: Check if KNX daemon/gateway is running and accessible

#### Common Connection Error Messages

```
✗ Connection failed: Failed to read device description from...
```

- Check if KNX gateway/daemon is running
- Verify gateway address and port
- Ensure network connectivity

```
No KNX USB devices found
```

- Ensure USB KNX interface is connected
- Check device permissions
- Try running with elevated privileges if needed

### Display Issues

- **Garbled output**: Ensure terminal supports ANSI colors and Unicode
- **Layout problems**: Resize terminal window or use `--verbose` for simpler output

#### No Messages Appearing

- Check if KNX devices are actually sending traffic
- Verify filter settings (remove filter to see all messages)
- Enable verbose logging with `--verbose`

### Docker Issues

- **Network not found**: Ensure SnapDog2 development environment is running
- **Permission denied**: Check Docker permissions and user configuration

## Tips & Best Practices

- Use `--verbose` for detailed logging when troubleshooting
- Filter messages with patterns like `1/2/*` to focus on specific areas
- The monitor shows the last 20 messages - older messages scroll off
- Press Ctrl+C to stop monitoring gracefully
- Colors help identify message types and priorities quickly
- Use the monitor alongside SnapDog2 for real-time debugging of KNX integration

## Development

### Project Structure

```
KnxMonitor/
├── Models/
│   ├── KnxMonitorConfig.cs    # Configuration model
│   └── KnxMessage.cs          # Message representation
├── Services/
│   ├── IKnxMonitorService.cs  # Monitor service interface
│   ├── KnxMonitorService.cs   # KNX bus monitoring logic
│   ├── IDisplayService.cs     # Display service interface
│   └── DisplayService.cs      # Visual display logic
├── Program.cs                 # Main entry point
├── KnxMonitor.csproj         # Project file
├── Dockerfile                # Multi-stage Dockerfile (dev + prod)
├── docker-compose.yml        # Docker Compose configuration
└── .dockerignore             # Docker build optimization
```

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
# Development: Simple build (no file copying needed due to bind mount)
docker build --target development -t knx-monitor:dev .

# Production: Full build from parent directory (for central package management)
cd /path/to/snapdog
docker build --target production -t knx-monitor:prod -f KnxMonitor/Dockerfile .
```

### Hot Reload Development

```bash
# Start with hot reload
dotnet watch run

# Or using Docker
docker compose --profile dev up knx-monitor-dev
```

## Integration with SnapDog2

The KNX Monitor is designed to work seamlessly with SnapDog2's development environment:

1. **Automatic Startup**: Included in the standard `make dev` command
2. **Shared Network**: Uses the same Docker network as SnapDog2
3. **Same KNX Daemon**: Connects to the same `knxd` container
4. **Compatible Configuration**: Uses the same connection parameters

### Debugging Workflow

1. Start SnapDog2 development environment:

   ```bash
   cd /path/to/snapdog
   make dev
   ```

2. View KNX Monitor output:

   ```bash
   docker compose logs knx-monitor -f
   ```

3. Trigger KNX operations in SnapDog2 and observe the traffic in real-time
4. Debug issues based on the message flow

### Advanced Integration Features

The KNX Monitor provides perfect integration for debugging SnapDog2:

- **Same Network**: Uses SnapDog2's Docker network for seamless connectivity
- **Same KNX Daemon**: Connects to the same `knxd` container as SnapDog2
- **Real-time Debugging**: Perfect for debugging SnapDog2's KNX integration
- **Message Correlation**: See exactly what KNX messages SnapDog2 sends and receives

### Detailed Debugging Process

1. **Start SnapDog2**: `make dev` starts the complete environment including KNX Monitor
2. **Monitor Traffic**: `docker compose logs knx-monitor -f` shows real-time KNX bus activity
3. **Trigger Operations**: Use SnapDog2's API or UI to trigger KNX operations
4. **Observe Messages**: Watch the KNX Monitor to see the actual bus traffic
5. **Debug Issues**: Use the message flow to identify and fix integration problems

This workflow makes it incredibly easy to understand and debug KNX integration issues in SnapDog2.

## Troubleshooting

### Connection Issues

- **"Gateway address is required"**: Specify `--gateway` for tunnel/router connections
- **"No KNX USB devices found"**: Ensure USB device is connected and accessible
- **"Connection refused"**: Check if KNX daemon/gateway is running and accessible

### Display Issues

- **Garbled output**: Ensure terminal supports ANSI colors and Unicode
- **Layout problems**: Resize terminal window or use `--verbose` for simpler output

### Docker Issues

- **Network not found**: Ensure SnapDog2 development environment is running
- **Permission denied**: Check Docker permissions and user configuration

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
