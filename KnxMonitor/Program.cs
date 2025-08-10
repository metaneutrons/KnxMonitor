using System.CommandLine;
using System.CommandLine.Parsing;
using System.Net.Sockets;
using KnxMonitor.Logging;
using KnxMonitor.Models;
using KnxMonitor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Polly;
using Spectre.Console;

namespace KnxMonitor;

/// <summary>
/// Main program entry point for the KNX Monitor application.
/// </summary>
public static partial class Program
{
    private static readonly TaskCompletionSource<bool> _shutdownCompletionSource = new();
    private static readonly CancellationTokenSource _applicationCancellationTokenSource = new();
    private static bool _shutdownRequested = false;
    private static readonly Lock _shutdownLock = new();

    /// <summary>
    /// Main entry point for the application.
    /// </summary>
    /// <param name="args">Command line arguments.</param>
    /// <returns>Exit code.</returns>
    public static async Task<int> Main(string[] args)
    {
        // CRITICAL FIX: Filter out arguments before '--' separator
        // This handles the issue where 'dotnet watch run --' passes all arguments to the app
        var separatorIndex = Array.IndexOf(args, "--");
        if (separatorIndex >= 0 && separatorIndex < args.Length - 1)
        {
            // Take only arguments after the '--' separator
            args = args.Skip(separatorIndex + 1).ToArray();
        }
        else if (separatorIndex >= 0)
        {
            // '--' found but no arguments after it
            args = Array.Empty<string>();
        }
        // If no '--' separator found, use all arguments as-is (for direct execution)

        // Set up global Ctrl+C handling at the application level
        Console.CancelKeyPress += OnCancelKeyPress;

        try
        {
            // Create options using modern System.CommandLine pattern
            Option<string?> gatewayOption = new("--gateway", "-g")
            {
                Description = "KNX gateway address (required for tunnel connections only)",
                Required = false,
            };

            Option<string> connectionTypeOption = new("--connection-type", "-c")
            {
                Description = "Connection type: tunnel (default), router, usb",
                DefaultValueFactory = _ => "tunnel",
            };
            connectionTypeOption.AcceptOnlyFromAmong("tunnel", "router", "usb");

            Option<string?> multicastAddressOption = new("--multicast-address", "-m")
            {
                Description = "Use router mode with multicast address (default: 224.0.23.12)",
                Arity = ArgumentArity.ZeroOrOne, // Allow -m without value
            };

            Option<int> portOption = new("--port", "-p")
            {
                Description = "Port number (default: 3671)",
                DefaultValueFactory = _ => 3671,
            };

            Option<bool> verboseOption = new("--verbose", "-v") { Description = "Enable verbose logging" };

            Option<string?> filterOption = new("--filter", "-f") { Description = "Group address filter pattern" };

            Option<string?> csvOption = new("--groupaddress-csv", "--csv")
            {
                Description = "Path to KNX group address CSV file (ETS export format 3/1, semicolon separated)",
            };

            Option<bool> loggingModeOption = new("--logging-mode", "-l")
            {
                Description =
                    "Force simple logging mode instead of TUI (useful for scripting or non-interactive environments)",
            };

            Option<bool> enableHealthCheckOption = new("--enable-health-check")
            {
                Description =
                    "Enable HTTP health check service on port 8080 (automatically enabled in Docker containers)",
            };

            // Create root command using modern pattern
            RootCommand rootCommand = new("KNX Monitor - Visual debugging tool for KNX/EIB bus activity");
            rootCommand.Options.Add(gatewayOption);
            rootCommand.Options.Add(connectionTypeOption);
            rootCommand.Options.Add(multicastAddressOption);
            rootCommand.Options.Add(portOption);
            rootCommand.Options.Add(verboseOption);
            rootCommand.Options.Add(filterOption);
            rootCommand.Options.Add(csvOption);
            rootCommand.Options.Add(loggingModeOption);
            rootCommand.Options.Add(enableHealthCheckOption);

            // Parse the command line arguments
            ParseResult parseResult = rootCommand.Parse(args);

            // Check if help was requested - if so, let the system handle it
            if (args.Contains("--help") || args.Contains("-h") || args.Contains("-?"))
            {
                Console.WriteLine(rootCommand.Description);
                Console.WriteLine();
                Console.WriteLine("Options:");
                foreach (var option in rootCommand.Options)
                {
                    var aliases = string.Join(", ", option.Aliases);
                    Console.WriteLine($"  {aliases, -20} {option.Description}");
                }
                return 0;
            }

            // Check for parsing errors
            if (parseResult.Errors.Count > 0)
            {
                foreach (ParseError parseError in parseResult.Errors)
                {
                    Console.Error.WriteLine(parseError.Message);
                }
                return 1;
            }

            // Extract parsed values with null checks
            string? gateway = parseResult.GetValue(gatewayOption);
            string? connectionType = parseResult.GetValue(connectionTypeOption);
            string? multicastAddress = parseResult.GetValue(multicastAddressOption);
            int port = parseResult.GetValue(portOption);
            bool verbose = parseResult.GetValue(verboseOption);
            string? filter = parseResult.GetValue(filterOption);
            string? csvPath = parseResult.GetValue(csvOption);
            bool loggingMode = parseResult.GetValue(loggingModeOption);
            bool enableHealthCheck = parseResult.GetValue(enableHealthCheckOption);

            // If -m/--multicast-address was specified, automatically switch to router mode
            bool multicastOptionUsed = args.Contains("-m") || args.Contains("--multicast-address");
            if (multicastOptionUsed)
            {
                connectionType = "router";
                // If no specific address was provided with -m, use default
                if (string.IsNullOrEmpty(multicastAddress))
                {
                    multicastAddress = "224.0.23.12";
                }
            }

            // Set default connection type if not provided and -m wasn't used
            if (string.IsNullOrEmpty(connectionType))
            {
                connectionType = "tunnel";
            }

            // Set default multicast address if not provided
            if (string.IsNullOrEmpty(multicastAddress))
            {
                multicastAddress = "224.0.23.12";
            }

            // Validate required parameters based on connection type
            if (connectionType.ToLowerInvariant() == "tunnel" && string.IsNullOrEmpty(gateway))
            {
                Console.Error.WriteLine("Error: Gateway address is required for tunnel connections");
                return 1;
            }

            // Display startup banner only when actually running the monitor
            DisplayStartupBanner();

            // Run the monitor
            return await RunMonitorAsync(
                gateway,
                connectionType,
                multicastAddress,
                port,
                verbose,
                filter,
                csvPath,
                loggingMode,
                enableHealthCheck
            );
        }
        finally
        {
            // Clean up global resources
            Console.CancelKeyPress -= OnCancelKeyPress;
            _applicationCancellationTokenSource.Dispose();
        }
    }

    /// <summary>
    /// Handles Ctrl+C at the application level to coordinate shutdown between main program and TUI.
    /// </summary>
    private static void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
    {
        lock (_shutdownLock)
        {
            if (_shutdownRequested)
            {
                // If shutdown already requested, allow immediate termination
                return;
            }

            // Prevent immediate termination and initiate graceful shutdown
            e.Cancel = true;
            _shutdownRequested = true;

            Console.WriteLine("\n[Ctrl+C] Initiating graceful shutdown...");

            // Signal cancellation to all components
            _applicationCancellationTokenSource.Cancel();

            // Signal shutdown completion
            _shutdownCompletionSource.TrySetResult(true);
        }
    }

    /// <summary>
    /// Runs the KNX monitor with the specified configuration.
    /// </summary>
    /// <param name="gateway">Gateway address (for tunnel connections).</param>
    /// <param name="connectionType">Connection type (tunnel/router/usb).</param>
    /// <param name="multicastAddress">Multicast address (for router connections).</param>
    /// <param name="port">Port number.</param>
    /// <param name="verbose">Enable verbose logging.</param>
    /// <param name="filter">Group address filter.</param>
    /// <param name="csvPath">Path to KNX group address CSV file exported from ETS.</param>
    /// <param name="loggingMode">Force simple logging mode instead of TUI.</param>
    /// <param name="enableHealthCheck">Enable HTTP health check service (auto-enabled in containers).</param>
    /// <returns>Exit code (0 = success, >0 = error).</returns>
    private static async Task<int> RunMonitorAsync(
        string? gateway,
        string connectionType,
        string multicastAddress,
        int port,
        bool verbose,
        string? filter,
        string? csvPath,
        bool loggingMode,
        bool enableHealthCheck
    )
    {
        IHost? host = null;
        IKnxMonitorService? monitorService = null;
        IDisplayService? displayService = null;
        HealthCheckService? healthCheckService = null;

        try
        {
            // Create configuration
            var config = new KnxMonitorConfig
            {
                ConnectionType = ParseConnectionType(connectionType),
                Gateway = gateway,
                MulticastAddress = multicastAddress,
                Port = port,
                Verbose = verbose,
                Filter = filter,
                GroupAddressCsvPath = csvPath,
            };

            // Validate configuration
            if (!ValidateConfiguration(config))
            {
                return 1; // Configuration error
            }

            // Create host builder
            var hostBuilder = Host.CreateDefaultBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();

                    // In logging mode, we want clean output without Microsoft logging prefixes
                    if (ShouldUseTuiMode(loggingMode))
                    {
                        // TUI mode - use normal Microsoft logging for debugging
                        if (verbose)
                        {
                            logging.AddConsole();
                            logging.SetMinimumLevel(LogLevel.Debug);
                        }
                        else
                        {
                            logging.SetMinimumLevel(LogLevel.Warning);
                        }
                    }
                    else
                    {
                        // Logging mode - use custom formatter for clean KNX message output
                        logging.AddConsole(options =>
                        {
                            options.FormatterName = "knx";
                        });
                        logging.AddConsoleFormatter<KnxConsoleFormatter, KnxConsoleFormatterOptions>();
                        logging.SetMinimumLevel(verbose ? LogLevel.Debug : LogLevel.Information);
                    }
                })
                .ConfigureServices(services =>
                {
                    services.AddSingleton(config);

                    // Register supporting services
                    services.AddSingleton<KnxGroupAddressDatabase>();
                    services.AddSingleton<KnxDptDecoder>();

                    // 🚀 Use simplified Falcon SDK-only KNX monitoring service
                    services.AddSingleton<IKnxMonitorService, KnxMonitorService>();

                    // Health check service registration logic:
                    // - Enabled by default in Docker containers (for container health monitoring)
                    // - Disabled by default in standalone mode (unless --enable-health-check is used)
                    bool shouldEnableHealthCheck =
                        enableHealthCheck
                        || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

                    if (shouldEnableHealthCheck)
                    {
                        services.AddSingleton<HealthCheckService>();
                    }

                    // Register appropriate display service based on environment
                    if (ShouldUseTuiMode(loggingMode))
                    {
                        services.AddSingleton<IDisplayService>(provider =>
                        {
                            var logger = provider.GetRequiredService<ILogger<TuiDisplayService>>();
                            return new TuiDisplayService(logger, config);
                        });
                    }
                    else
                    {
                        services.AddSingleton<IDisplayService, DisplayService>();
                    }
                });

            host = hostBuilder.Build();

            // Get services
            monitorService = host.Services.GetRequiredService<IKnxMonitorService>();
            displayService = host.Services.GetRequiredService<IDisplayService>();

            // Conditionally get health check service (same logic as registration)
            bool shouldEnableHealthCheck =
                enableHealthCheck || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";

            if (shouldEnableHealthCheck)
            {
                healthCheckService = host.Services.GetRequiredService<HealthCheckService>();
            }

            // Start health check service if enabled
            if (healthCheckService != null)
            {
                await healthCheckService.StartAsync(8080, _applicationCancellationTokenSource.Token);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Health check service started on port 8080");
            }

            // Start monitoring service first
            try
            {
                await StartMonitoringWithRetry(monitorService, _applicationCancellationTokenSource.Token);
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {ex.Message} Exiting.");
                return 2; // Exit code 2 for connection failure
            }

            // Start display service with proper lifecycle coordination
            var displayTask = displayService.StartAsync(monitorService, _applicationCancellationTokenSource.Token);

            // Wait for either shutdown signal or display service completion
            var completedTask = await Task.WhenAny(displayTask, _shutdownCompletionSource.Task);

            if (completedTask == _shutdownCompletionSource.Task)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Shutdown signal received, stopping services...");
            }
            else
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Display service completed, shutting down...");
            }

            return 0; // Success
        }
        catch (OperationCanceledException)
        {
            // Expected when Ctrl+C is pressed or cancellation is requested
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Operation cancelled, shutting down gracefully...");
            return 0; // Normal shutdown
        }
        catch (Exception ex)
        {
            AnsiConsole.WriteException(ex);
            return 1; // General error
        }
        finally
        {
            // Ensure graceful cleanup of all services
            await CleanupServicesAsync(host, monitorService, displayService, healthCheckService);
        }
    }

    /// <summary>
    /// Performs graceful cleanup of all services in the correct order.
    /// </summary>
    /// <param name="host">The host instance.</param>
    /// <param name="monitorService">The KNX monitor service.</param>
    /// <param name="displayService">The display service.</param>
    /// <param name="healthCheckService">The health check service.</param>
    private static async Task CleanupServicesAsync(
        IHost? host,
        IKnxMonitorService? monitorService,
        IDisplayService? displayService,
        HealthCheckService? healthCheckService = null
    )
    {
        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting graceful cleanup...");

            // Stop health check service first
            if (healthCheckService != null)
            {
                try
                {
                    await healthCheckService.StopAsync();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Health check service stopped");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Error stopping health check service: {ex.Message}"
                    );
                }
            }

            // Stop display service first (this handles TUI shutdown)
            if (displayService != null)
            {
                try
                {
                    await displayService.StopAsync(_applicationCancellationTokenSource.Token);
                    await displayService.DisposeAsync();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Display service stopped and disposed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error stopping display service: {ex.Message}");
                }
            }

            // Stop monitoring service
            if (monitorService != null)
            {
                try
                {
                    await monitorService.StopMonitoringAsync(_applicationCancellationTokenSource.Token);
                    await monitorService.DisposeAsync();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Monitor service stopped and disposed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error stopping monitor service: {ex.Message}");
                }
            }

            // Stop host
            if (host != null)
            {
                try
                {
                    await host.StopAsync(TimeSpan.FromSeconds(5));
                    host.Dispose();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Host stopped and disposed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error stopping host: {ex.Message}");
                }
            }

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] KNX Monitor stopped gracefully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Error during cleanup: {ex.Message}");
        }
    }

    /// <summary>
    /// Displays the startup banner.
    /// </summary>
    private static void DisplayStartupBanner()
    {
        AnsiConsole.Write(new FigletText("KNX Monitor").LeftJustified().Color(Color.Cyan1));

        AnsiConsole.MarkupLine("[dim]Visual debugging tool for KNX/EIB bus activity[/]");
        AnsiConsole.MarkupLine("[dim]Press Ctrl+C to stop monitoring[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// Parses the connection type string.
    /// </summary>
    /// <param name="connectionType">Connection type string.</param>
    /// <returns>Parsed connection type.</returns>
    private static KnxConnectionType ParseConnectionType(string connectionType)
    {
        if (string.IsNullOrEmpty(connectionType))
        {
            throw new ArgumentException("Connection type cannot be null or empty");
        }

        return connectionType.ToLowerInvariant() switch
        {
            "tunnel" => KnxConnectionType.Tunnel,
            "router" => KnxConnectionType.Router,
            "usb" => KnxConnectionType.Usb,
            _ => throw new ArgumentException(
                $"Invalid connection type: {connectionType}. Valid values are: tunnel, router, usb"
            ),
        };
    }

    /// <summary>
    /// Validates the configuration.
    /// </summary>
    /// <param name="config">Configuration to validate.</param>
    /// <returns>True if valid, false otherwise.</returns>
    private static bool ValidateConfiguration(KnxMonitorConfig config)
    {
        // Gateway is only required for tunnel connections
        if (config.ConnectionType == KnxConnectionType.Tunnel && string.IsNullOrEmpty(config.Gateway))
        {
            AnsiConsole.MarkupLine("[red]Error: Gateway address is required for tunnel connections[/]");
            return false;
        }

        // Validate multicast address for router connections
        if (config.ConnectionType == KnxConnectionType.Router && string.IsNullOrEmpty(config.MulticastAddress))
        {
            AnsiConsole.MarkupLine("[red]Error: Multicast address is required for router connections[/]");
            return false;
        }

        if (config.Port <= 0 || config.Port > 65535)
        {
            AnsiConsole.MarkupLine("[red]Error: Port must be between 1 and 65535[/]");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Determines whether to use Terminal.Gui TUI mode or console logging mode.
    /// </summary>
    /// <param name="forceLoggingMode">Force logging mode regardless of environment.</param>
    /// <returns>True if TUI mode should be used, false for logging mode.</returns>
    private static bool ShouldUseTuiMode(bool forceLoggingMode = false)
    {
        // Force logging mode if explicitly requested
        if (forceLoggingMode)
        {
            return false;
        }

        // Use logging mode if output is redirected or in container environment
        if (
            Console.IsOutputRedirected
            || Console.IsInputRedirected
            || Environment.GetEnvironmentVariable("KNX_MONITOR_LOGGING_MODE") == "true"
            || Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true"
        )
        {
            return false;
        }

        // Use TUI mode for interactive terminals
        return true;
    }

    /// <summary>
    /// Starts the monitoring service with Polly retry logic.
    /// </summary>
    /// <param name="monitorService">The monitor service to start.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Task representing the async operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown when connection fails after all retries.</exception>
    private static async Task StartMonitoringWithRetry(
        IKnxMonitorService monitorService,
        CancellationToken cancellationToken = default
    )
    {
        // Create a retry policy with Polly
        var retryPolicy = Policy
            .Handle<Exception>(ex =>
                // Retry on most exceptions, but not on cancellation
                ex is not OperationCanceledException
                || !cancellationToken.IsCancellationRequested
            )
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff: 2s, 4s, 8s
                onRetry: (exception, timespan, retryCount, context) =>
                {
                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Connection attempt {retryCount} failed: {exception.Message}"
                    );

                    // Provide specific guidance for common errors
                    if (
                        exception is SocketException socketEx
                        && socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse
                    )
                    {
                        Console.WriteLine(
                            $"[{DateTime.Now:HH:mm:ss.fff}] Hint: Another KNX application may be using the multicast address. Try stopping other KNX tools."
                        );
                    }

                    Console.WriteLine(
                        $"[{DateTime.Now:HH:mm:ss.fff}] Retrying in {timespan.TotalSeconds} seconds... (attempt {retryCount + 1}/4)"
                    );
                }
            );

        try
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Starting KNX connection with retry policy...");

            await retryPolicy.ExecuteAsync(
                async (ct) =>
                {
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Attempting KNX connection...");

                    // Create a timeout for each individual attempt
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(10));

                    await monitorService.StartMonitoringAsync(timeoutCts.Token);

                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] KNX Monitor connection successful");
                },
                cancellationToken
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Connection cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            var errorMessage = ex switch
            {
                SocketException socketEx when socketEx.SocketErrorCode == SocketError.AddressAlreadyInUse =>
                    "Address already in use. Another KNX application may be running.",
                SocketException socketEx when socketEx.SocketErrorCode == SocketError.NetworkUnreachable =>
                    "Network unreachable. Check your network connection and KNX gateway.",
                SocketException socketEx when socketEx.SocketErrorCode == SocketError.TimedOut =>
                    "Connection timed out. Check if the KNX gateway is reachable.",
                _ => ex.Message,
            };

            throw new InvalidOperationException($"Failed to connect after 4 attempts. Last error: {errorMessage}", ex);
        }
    }
}
