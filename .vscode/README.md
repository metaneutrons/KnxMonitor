# VS Code Configuration for KnxMonitor

This directory contains VS Code configuration files for optimal development experience with the KnxMonitor project.

## 🚀 Quick Start

### Keyboard Shortcuts

| Shortcut | Action | Description |
|----------|--------|-------------|
| **Shift+Cmd+B** | Build and Run | **Main shortcut** - Builds project and runs in router mode |
| **Shift+Cmd+R** | Run Router | Run KnxMonitor in IP routing mode |
| **Shift+Cmd+T** | Run Tunnel | Run KnxMonitor in IP tunneling mode |
| **Shift+Cmd+W** | Watch Mode | Run with hot reload (dotnet watch) |
| **Cmd+K Cmd+T** | Run Tests | Execute all unit tests |
| **Cmd+K Cmd+C** | Clean | Clean build artifacts |
| **Cmd+K Cmd+V** | Version | Show version information |
| **F5** | Debug | Start debugging with breakpoints |
| **Ctrl+F5** | Run | Run without debugging |
| **Shift+F5** | Stop | Stop debugging session |

### Primary Development Workflow

1. **Press Shift+Cmd+B** to build and run the application in router mode
2. The application will start monitoring KNX traffic
3. Use **Ctrl+C** in the terminal to stop the application
4. Use **F5** to debug with breakpoints

## 📁 Configuration Files

### `tasks.json` - Build and Run Tasks
- **build and run** (default): Builds project and runs in router mode
- **build**: Compiles the project
- **run-router**: Runs in IP routing mode with verbose logging
- **run-tunnel**: Runs in IP tunneling mode (requires KNX gateway IP)
- **run-with-csv**: Runs with group address CSV database
- **watch**: Hot reload development mode
- **test**: Runs unit tests
- **clean**: Cleans build artifacts
- **version**: Shows comprehensive version information

### `launch.json` - Debug Configurations
- **KnxMonitor - Router Mode**: Debug in IP routing mode
- **KnxMonitor - Tunnel Mode**: Debug in IP tunneling mode
- **KnxMonitor - Router with CSV**: Debug with group address database
- **KnxMonitor - Help**: Debug help command
- **KnxMonitor - Health Check Mode**: Debug with health check endpoint
- **KnxMonitor - Version Info**: Debug version information display

### `settings.json` - Project Settings
- .NET and C# development optimizations
- File associations and exclusions
- Editor formatting and code actions
- Terminal environment variables
- Performance optimizations

### `extensions.json` - Recommended Extensions
Essential extensions for .NET development:
- **ms-dotnettools.csdevkit**: C# Dev Kit
- **ms-dotnettools.csharp**: C# language support
- **editorconfig.editorconfig**: EditorConfig support
- **eamodio.gitlens**: Git integration
- **ms-azuretools.vscode-docker**: Docker support

### `keybindings.json` - Custom Shortcuts
Custom keyboard shortcuts for common development tasks.

## 🔧 Development Modes

### 1. Quick Development (Recommended)
```bash
# Press Shift+Cmd+B or use Command Palette
> Tasks: Run Task > build and run
```

### 2. Watch Mode (Hot Reload)
```bash
# Press Shift+Cmd+W or use Command Palette
> Tasks: Run Task > watch
```

### 3. Debug Mode
```bash
# Press F5 or use Command Palette
> Debug: Start Debugging > KnxMonitor - Router Mode
```

### 4. Testing
```bash
# Press Cmd+K Cmd+T or use Command Palette
> Tasks: Run Task > test
```

## 🌐 Connection Types

### IP Routing (Default)
- **Task**: `run-router`
- **Args**: `--connection-type routing --verbose`
- **Use Case**: Local network multicast monitoring
- **Requirements**: None (uses multicast)

### IP Tunneling
- **Task**: `run-tunnel`
- **Args**: `--connection-type tunneling --host 192.168.1.100 --verbose`
- **Use Case**: Remote KNX gateway connection
- **Requirements**: KNX IP gateway IP address

### With CSV Database
- **Task**: `run-with-csv`
- **Args**: `--connection-type routing --csv-path knx-addresses.csv --verbose`
- **Use Case**: Group address name resolution
- **Requirements**: CSV file with group addresses

## 📊 Sample CSV File

A sample `knx-addresses.csv` file is included with common KNX group addresses:
- Lighting controls (0/1/x)
- Dimming controls (0/2/x)
- Temperature sensors (1/1/x)
- Humidity sensors (1/2/x)
- Security contacts (2/1/x)
- HVAC controls (3/1/x)

## 🎯 Tips for Development

### 1. Use the Default Build Task
- **Shift+Cmd+B** is configured as the primary development shortcut
- It builds the project and immediately runs it in router mode
- Perfect for quick testing and development

### 2. Debugging
- Set breakpoints in your code
- Press **F5** to start debugging
- Choose the appropriate launch configuration
- Use the integrated terminal for output

### 3. Hot Reload Development
- Use **Shift+Cmd+W** for watch mode
- Code changes automatically trigger rebuilds
- Great for rapid development cycles

### 4. Testing Workflow
- Use **Cmd+K Cmd+T** to run tests
- Tests run with verbose output
- Results appear in the terminal

### 5. Clean Builds
- Use **Cmd+K Cmd+C** when you need a clean build
- Removes all build artifacts
- Useful for troubleshooting build issues

## 🔍 Troubleshooting

### Build Issues
1. Run clean task: **Cmd+K Cmd+C**
2. Rebuild: **Shift+Cmd+B**
3. Check terminal output for errors

### Connection Issues
1. Verify network connectivity
2. Check KNX gateway IP address (for tunneling)
3. Ensure no firewall blocking multicast (for routing)

### Extension Issues
1. Install recommended extensions from `extensions.json`
2. Reload VS Code window: **Cmd+R**
3. Check extension compatibility

## 📚 Additional Resources

- **KnxMonitor Documentation**: [README.md](../README.md)
- **Contributing Guide**: [CONTRIBUTING.md](../CONTRIBUTING.md)
- **VS Code .NET Documentation**: https://code.visualstudio.com/docs/languages/dotnet
- **C# Dev Kit**: https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csdevkit

---

**Happy coding! 🎉**
