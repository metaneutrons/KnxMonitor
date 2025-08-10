# Contributing to KNX Monitor

Thank you for your interest in contributing to KNX Monitor! This document provides guidelines and information for contributors.

## 🚀 Getting Started

### Prerequisites
- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Git](https://git-scm.com/)
- A KNX/EIB system for testing (optional but recommended)

### Development Setup
1. Fork the repository
2. Clone your fork:
   ```bash
   git clone https://github.com/metaneutrons/KnxMonitor.git
   cd KnxMonitor
   ```
3. Build the project:
   ```bash
   dotnet restore
   dotnet build
   ```
4. Run tests:
   ```bash
   dotnet test
   ```

## 📋 Development Guidelines

### Code Standards
- Follow [.NET coding conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use structured logging with proper event IDs (1000-4999 range)
- Maintain test coverage >90%
- Document public APIs with XML comments
- Use async/await patterns consistently

### Architecture Principles
- **Clean Architecture**: Maintain separation of concerns
- **CQRS Patterns**: Use command/query separation
- **Enterprise Patterns**: Follow established enterprise-grade patterns
- **Resource Management**: Implement proper disposal patterns

### Event ID Ranges
- **1000-1999**: Application lifecycle events
- **2000-2999**: Core service events
- **3000-3999**: Data service events
- **4000-4999**: UI service events

## 🔧 Making Changes

### Branch Naming
- `feature/description` - New features
- `bugfix/description` - Bug fixes
- `hotfix/description` - Critical fixes
- `docs/description` - Documentation updates

### Commit Messages
Follow [Conventional Commits](https://www.conventionalcommits.org/):
```
feat: add support for KNX secure tunneling
fix: resolve memory leak in message processing
docs: update installation instructions
```

### Pull Request Process
1. Create a feature branch from `develop`
2. Make your changes following the coding standards
3. Add/update tests as needed
4. Update documentation if required
5. Ensure all tests pass
6. Submit a pull request to `develop`

## 🧪 Testing

### Unit Tests
- Write tests for all new functionality
- Use descriptive test names
- Follow AAA pattern (Arrange, Act, Assert)
- Mock external dependencies

### Integration Tests
- Test KNX protocol interactions
- Verify CSV parsing functionality
- Test health check endpoints

### Performance Tests
- Benchmark critical paths
- Ensure memory usage remains reasonable
- Test with high message volumes

## 📚 Documentation

### Code Documentation
- Document all public APIs with XML comments
- Include usage examples for complex functionality
- Document any breaking changes

### User Documentation
- Update README.md for new features
- Add examples to the documentation
- Update Docker usage instructions

## 🐛 Bug Reports

When reporting bugs, please include:
- KNX Monitor version
- Operating system and version
- .NET version
- Steps to reproduce
- Expected vs actual behavior
- Log output (if applicable)

## 💡 Feature Requests

For feature requests, please provide:
- Clear description of the feature
- Use case and motivation
- Proposed implementation approach
- Any breaking changes

## 🏗️ Architecture Overview

### Core Components
- **KnxMonitorService**: Main monitoring engine
- **KnxGroupAddressDatabase**: Address resolution
- **KnxDptDecoder**: Data point type decoding
- **DisplayService**: Console output
- **HealthCheckService**: HTTP health endpoints

### Design Patterns
- **Dependency Injection**: Service registration and lifetime management
- **Observer Pattern**: Event-driven message processing
- **Factory Pattern**: Service creation and configuration
- **Command Pattern**: User input handling

## 📊 Performance Guidelines

### Memory Management
- Use `IAsyncDisposable` for resources
- Avoid memory leaks in long-running operations
- Use object pooling for high-frequency allocations

### Logging Performance
- Use `LoggerMessage` for high-performance logging
- Avoid string interpolation in log messages
- Use structured logging parameters

### Network Performance
- Implement proper connection pooling
- Use async patterns for I/O operations
- Handle network timeouts gracefully

## 🔒 Security Considerations

- Validate all user inputs
- Use secure defaults
- Avoid logging sensitive information
- Follow OWASP guidelines

## 📝 License

By contributing to KNX Monitor, you agree that your contributions will be licensed under the GNU Lesser General Public License v3.0.

## 🤝 Community

- Be respectful and inclusive
- Help others learn and grow
- Share knowledge and best practices
- Follow the [Code of Conduct](CODE_OF_CONDUCT.md)

## 📞 Getting Help

- Check existing [Issues](https://github.com/metaneutrons/KnxMonitor/issues)
- Start a [Discussion](https://github.com/metaneutrons/KnxMonitor/discussions)
- Review the [Wiki](https://github.com/metaneutrons/KnxMonitor/wiki)

Thank you for contributing to KNX Monitor! 🎉
