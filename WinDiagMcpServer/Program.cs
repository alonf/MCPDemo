using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

ConsoleUi.RenderBanner();

var builder = Host.CreateApplicationBuilder(args);

var logLevel = ResolveLogLevel(Environment.GetEnvironmentVariable("MCP_LOG_LEVEL"));

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.TimestampFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";
    options.UseUtcTimestamp = true;
    options.IncludeScopes = true;
    options.JsonWriterOptions = new System.Text.Json.JsonWriterOptions
    {
        Indented = false
    };
});
builder.Services.Configure<Microsoft.Extensions.Logging.Console.ConsoleLoggerOptions>(options =>
{
    // Force all logs to stderr to avoid polluting stdout (which breaks MCP protocol)
    options.LogToStandardErrorThreshold = LogLevel.Trace;
});
builder.Logging.SetMinimumLevel(logLevel);

// Register event log snapshot storage as singleton
builder.Services.AddSingleton<IEventLogSnapshotStorage, EventLogSnapshotStorage>();

builder.Services.AddMcpServer().
    WithStdioServerTransport().
    WithToolsFromAssembly().
    WithResourcesFromAssembly().
    WithPromptsFromAssembly();

var app = builder.Build();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("McpServer.Startup");

startupLogger.LogInformation("WinDiag MCP Server started with log level: {LogLevel}", logLevel);

await app.RunAsync();

static LogLevel ResolveLogLevel(string? configuredLevel)
{
    if (!string.IsNullOrWhiteSpace(configuredLevel) && Enum.TryParse<LogLevel>(configuredLevel, true, out var parsed))
    {
        return parsed;
    }

    var verboseProtocol = Environment.GetEnvironmentVariable("MCP_VERBOSE_PROTOCOL");
    if (string.Equals(verboseProtocol, "true", StringComparison.OrdinalIgnoreCase))
    {
        return LogLevel.Debug;
    }

    return LogLevel.Information;
}
