using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WinDiagMcpServer;

// Only show banner if not running under Inspector
if (Environment.GetEnvironmentVariable("MCP_INSPECTOR") != "true")
{
    ConsoleUi.RenderBanner();
}

var builder = Host.CreateApplicationBuilder(args);

// Reduce logging noise - only show warnings and errors
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options =>
{
    options.LogToStandardErrorThreshold = LogLevel.Warning;
});
builder.Logging.SetMinimumLevel(LogLevel.Warning);

builder.Services.AddMcpServer().
    WithStdioServerTransport().
    WithToolsFromAssembly();

var app = builder.Build();

await app.RunAsync();
