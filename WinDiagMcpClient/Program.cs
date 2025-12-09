using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace WinDiagMcpClient;

/// <summary>
/// Simple MCP client that demonstrates connecting to the WinDiag MCP Server
/// and calling the get_system_info tool.
/// </summary>
class Program
{
    static async Task<int> Main(string[] args)
    {
        Console.WriteLine("============================================");
        Console.WriteLine("WinDiag MCP Client - C# Demo");
        Console.WriteLine("============================================");
        Console.WriteLine();

        try
        {
            // Path to the server project
            var serverProjectPath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "..",
                "WinDiagMcpServer",
                "WinDiagMcpServer.csproj");

            if (!File.Exists(serverProjectPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"ERROR: Server project not found at: {serverProjectPath}");
                Console.ResetColor();
                return 1;
            }

            Console.WriteLine($"Server project: {serverProjectPath}");
            Console.WriteLine();

            // Create MCP client
            using var client = new McpClient(serverProjectPath);

            Console.WriteLine("[1/4] Starting MCP server...");
            await client.StartAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("      Server started successfully!");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("[2/4] Initializing connection...");
            await client.InitializeAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("      Connection initialized!");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("[3/4] Listing available tools...");
            var tools = await client.ListToolsAsync();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"      Found {tools.Length} tool(s):");
            Console.ResetColor();
            foreach (var tool in tools)
            {
                Console.WriteLine($"        - {tool}");
            }
            Console.WriteLine();

            Console.WriteLine("[4/4] Calling get_system_info tool...");
            var result = await client.CallToolAsync("get_system_info", new { });
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("      Tool executed successfully!");
            Console.ResetColor();
            Console.WriteLine();

            Console.WriteLine("============================================");
            Console.WriteLine("System Information:");
            Console.WriteLine("============================================");
            Console.WriteLine(JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true }));
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✓ Client test completed successfully!");
            Console.ResetColor();

            return 0;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"ERROR: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
            Console.ResetColor();
            return 1;
        }
    }
}

/// <summary>
/// Simple MCP client implementation using STDIO transport.
/// </summary>
class McpClient : IDisposable
{
    private readonly string _serverProjectPath;
    private Process? _serverProcess;
    private int _requestId = 1;
    private readonly SemaphoreSlim _writeLock = new(1, 1);

    public McpClient(string serverProjectPath)
    {
        _serverProjectPath = serverProjectPath;
    }

    public async Task StartAsync()
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"run --project \"{_serverProjectPath}\"",
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        _serverProcess = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start server process");

        // Give the server a moment to start
        await Task.Delay(1000);

        if (_serverProcess.HasExited)
        {
            throw new InvalidOperationException("Server process exited unexpectedly");
        }
    }

    public async Task InitializeAsync()
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = _requestId++,
            method = "initialize",
            @params = new
            {
                protocolVersion = "2024-11-05",
                capabilities = new { },
                clientInfo = new
                {
                    name = "WinDiagMcpClient",
                    version = "1.0.0"
                }
            }
        };

        var response = await SendRequestAsync(request);
        if (response == null)
        {
            throw new InvalidOperationException("No response from initialize");
        }

        // Send initialized notification
        var notification = new
        {
            jsonrpc = "2.0",
            method = "notifications/initialized"
        };

        await SendNotificationAsync(notification);
    }

    public async Task<string[]> ListToolsAsync()
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = _requestId++,
            method = "tools/list",
            @params = new { }
        };

        var response = await SendRequestAsync(request);
        if (response?["result"]?["tools"] is JsonArray tools)
        {
            return tools
                .Select(t => t?["name"]?.GetValue<string>() ?? "unknown")
                .ToArray();
        }

        return Array.Empty<string>();
    }

    public async Task<JsonNode?> CallToolAsync(string toolName, object arguments)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = _requestId++,
            method = "tools/call",
            @params = new
            {
                name = toolName,
                arguments
            }
        };

        var response = await SendRequestAsync(request);
        return response?["result"];
    }

    private async Task<JsonNode?> SendRequestAsync(object request)
    {
        if (_serverProcess == null || _serverProcess.HasExited)
        {
            throw new InvalidOperationException("Server process is not running");
        }

        await _writeLock.WaitAsync();
        try
        {
            // Send request
            var json = JsonSerializer.Serialize(request);
            await _serverProcess.StandardInput.WriteLineAsync(json);
            await _serverProcess.StandardInput.FlushAsync();

            // Read response
            var responseLine = await _serverProcess.StandardOutput.ReadLineAsync();
            if (string.IsNullOrEmpty(responseLine))
            {
                return null;
            }

            return JsonNode.Parse(responseLine);
        }
        finally
        {
            _writeLock.Release();
        }
    }

    private async Task SendNotificationAsync(object notification)
    {
        if (_serverProcess == null || _serverProcess.HasExited)
        {
            throw new InvalidOperationException("Server process is not running");
        }

        await _writeLock.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(notification);
            await _serverProcess.StandardInput.WriteLineAsync(json);
            await _serverProcess.StandardInput.FlushAsync();
        }
        finally
        {
            _writeLock.Release();
        }
    }

    public void Dispose()
    {
        if (_serverProcess != null && !_serverProcess.HasExited)
        {
            _serverProcess.Kill(true);
            _serverProcess.Dispose();
        }
        _writeLock.Dispose();
    }
}
