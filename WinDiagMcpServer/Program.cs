using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using WinDiagMcpServer.Infrastructure;
using WinDiagMcpServer.Resources.Registry;
using WinDiagMcpServer.Services;

ConsoleUi.RenderBanner();

var builder = WebApplication.CreateBuilder(args);

var logLevel = ResolveLogLevel(Environment.GetEnvironmentVariable("MCP_LOG_LEVEL"));

builder.Logging.ClearProviders();

// Use custom McpConsoleFormatter for highlighted method names
builder.Logging.AddConsole(options => options.FormatterName = "mcp")
    .AddConsoleFormatter<McpConsoleFormatter, ConsoleFormatterOptions>();
builder.Logging.SetMinimumLevel(logLevel);

// Register event log snapshot storage as singleton
builder.Services.AddSingleton<IEventLogSnapshotStorage, EventLogSnapshotStorage>();
builder.Services.AddSingleton<IRegistrySnapshotStorage, RegistrySnapshotStorage>();
builder.Services.AddSingleton<RegistryRootsService>();

builder.Services.AddMcpServer().
    WithHttpTransport().
    WithToolsFromAssembly().
    WithResourcesFromAssembly().
    WithPromptsFromAssembly();

var app = builder.Build();

app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/sse") || context.Request.Path.StartsWithSegments("/messages"))
    {
        var apiKey = context.Request.Query["apiKey"].FirstOrDefault()
                     ?? context.Request.Headers["X-API-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || apiKey != "secure-mcp-key")
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }
    }

    await next();
});

app.MapMcp();

var startupLogger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("McpServer.Startup");

#pragma warning disable S6668 // Logging arguments should be passed to the correct parameter
startupLogger.LogInformation("WinDiag MCP Server started with log level: {ConfiguredLogLevel}", logLevel);
#pragma warning restore S6668 // Logging arguments should be passed to the correct parameter

#pragma warning disable S1075 // Refactor your code not to use hardcoded absolute paths or URIs
var url = "http://localhost:5000";
#pragma warning restore S1075 // Refactor your code not to use hardcoded absolute paths or URIs
await app.RunAsync(url);

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
