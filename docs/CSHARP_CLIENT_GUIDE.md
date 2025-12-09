# C# MCP Client Guide

This guide demonstrates how to create a C# client that connects to an MCP server and calls tools.

## Overview

The **WinDiagMcpClient** is a simple C# console application that demonstrates:
- ✅ Starting an MCP server as a child process
- ✅ Communicating via STDIO transport
- ✅ Implementing the MCP JSON-RPC protocol
- ✅ Calling MCP tools programmatically
- ✅ Handling responses and displaying results

## Quick Start

### Run the Client

```powershell
.\run-csharp-client.ps1
```

This will:
1. Build the client project
2. Start the MCP server
3. Initialize the connection
4. List available tools
5. Call the `get_system_info` tool
6. Display the results

### Expected Output

```
============================================
WinDiag MCP Client - C# Demo
============================================

Server project: C:\Dev\MCPDemo\WinDiagMcpServer\WinDiagMcpServer.csproj

[1/4] Starting MCP server...
      Server started successfully!

[2/4] Initializing connection...
      Connection initialized!

[3/4] Listing available tools...
      Found 1 tool(s):
        - get_system_info

[4/4] Calling get_system_info tool...
      Tool executed successfully!

============================================
System Information:
============================================
{
  "content": [
    {
      "type": "text",
      "text": "{
        \"machineName\": \"HOMEALON11\",
        \"userName\": \"alon\",
        \"osDescription\": \"Microsoft Windows 10.0.22631\",
        ...
      }"
    }
  ]
}

✓ Client test completed successfully!
```

---

## Architecture

### STDIO Transport

The client uses **STDIO (Standard Input/Output)** transport:
- Starts the MCP server as a child process
- Sends JSON-RPC requests via `StandardInput`
- Receives responses via `StandardOutput`
- Errors appear on `StandardError`

### Communication Flow

```
┌─────────────┐                  ┌─────────────┐
│             │  Initialize      │             │
│             ├─────────────────>│             │
│             │<─────────────────┤             │
│             │  Initialized     │             │
│             │                  │             │
│   Client    │  tools/list      │   Server    │
│             ├─────────────────>│             │
│             │<─────────────────┤             │
│             │  Tool List       │             │
│             │                  │             │
│             │  tools/call      │             │
│             ├─────────────────>│             │
│             │<─────────────────┤             │
│             │  Tool Result     │             │
└─────────────┘                  └─────────────┘
```

---

## Code Structure

### Main Program (`Program.cs`)

The main program demonstrates the complete workflow:

```csharp
// 1. Create client
using var client = new McpClient(serverProjectPath);

// 2. Start server
await client.StartAsync();

// 3. Initialize connection
await client.InitializeAsync();

// 4. List tools
var tools = await client.ListToolsAsync();

// 5. Call a tool
var result = await client.CallToolAsync("get_system_info", new { });
```

### McpClient Class

The `McpClient` class handles:
- **Process Management**: Starting and stopping the server process
- **JSON-RPC Protocol**: Formatting requests and parsing responses
- **Thread Safety**: Using `SemaphoreSlim` for synchronized writes
- **Error Handling**: Detecting server failures

Key methods:
- `StartAsync()` - Starts the server process
- `InitializeAsync()` - Performs MCP handshake
- `ListToolsAsync()` - Discovers available tools
- `CallToolAsync()` - Executes a tool

---

## JSON-RPC Protocol

### Initialize Request

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "method": "initialize",
  "params": {
    "protocolVersion": "2024-11-05",
    "capabilities": {},
    "clientInfo": {
      "name": "WinDiagMcpClient",
      "version": "1.0.0"
    }
  }
}
```

### Initialize Response

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "protocolVersion": "2024-11-05",
    "capabilities": { ... },
    "serverInfo": {
      "name": "WinDiag MCP Server",
      "version": "1.0.0"
    }
  }
}
```

### Tool Call Request

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "method": "tools/call",
  "params": {
    "name": "get_system_info",
    "arguments": {}
  }
}
```

### Tool Call Response

```json
{
  "jsonrpc": "2.0",
  "id": 2,
  "result": {
    "content": [
      {
        "type": "text",
        "text": "{ \"machineName\": \"...\", ... }"
      }
    ]
  }
}
```

---

## Building Your Own Client

### Step 1: Add Required Packages

```xml
<ItemGroup>
  <PackageReference Include="System.Text.Json" Version="10.0.0" />
</ItemGroup>
```

### Step 2: Start the Server Process

```csharp
var startInfo = new ProcessStartInfo
{
    FileName = "dotnet",
    Arguments = $"run --project \"{serverProjectPath}\"",
    UseShellExecute = false,
    RedirectStandardInput = true,
    RedirectStandardOutput = true,
    RedirectStandardError = true,
    CreateNoWindow = true
};

var process = Process.Start(startInfo);
```

### Step 3: Send JSON-RPC Requests

```csharp
var request = new
{
    jsonrpc = "2.0",
    id = requestId++,
    method = "tools/call",
    @params = new
    {
        name = "get_system_info",
        arguments = new { }
    }
};

var json = JsonSerializer.Serialize(request);
await process.StandardInput.WriteLineAsync(json);
await process.StandardInput.FlushAsync();
```

### Step 4: Read Responses

```csharp
var responseLine = await process.StandardOutput.ReadLineAsync();
var response = JsonNode.Parse(responseLine);
return response?["result"];
```

---

## Error Handling

### Server Startup Failures

```csharp
if (_serverProcess.HasExited)
{
    throw new InvalidOperationException("Server process exited unexpectedly");
}
```

### Communication Errors

```csharp
if (string.IsNullOrEmpty(responseLine))
{
    throw new InvalidOperationException("No response from server");
}
```

### Thread Safety

```csharp
private readonly SemaphoreSlim _writeLock = new(1, 1);

await _writeLock.WaitAsync();
try
{
    // Send request
}
finally
{
    _writeLock.Release();
}
```

---

## Advanced Features

### Tool Parameters

```csharp
// Call a tool with parameters
var result = await client.CallToolAsync("list_processes", new
{
    top = 10,
    sortBy = "cpu"
});
```

### Multiple Tool Calls

```csharp
// Call multiple tools
var systemInfo = await client.CallToolAsync("get_system_info", new { });
var processes = await client.CallToolAsync("list_processes", new { });
var diskUsage = await client.CallToolAsync("get_disk_usage", new { });
```

### Notification Handling

```csharp
private async Task SendNotificationAsync(object notification)
{
    var json = JsonSerializer.Serialize(notification);
    await _serverProcess.StandardInput.WriteLineAsync(json);
    await _serverProcess.StandardInput.FlushAsync();
}
```

---

## Comparison with Other Clients

| Feature | C# Client | Claude Desktop | mcp-cli |
|---------|-----------|----------------|---------|
| **Language** | C# | N/A | Python |
| **LLM Integration** | ❌ No | ✅ Yes | ❌ No |
| **Programmatic** | ✅ Yes | ❌ No | ⚠️ Limited |
| **Custom Logic** | ✅ Yes | ❌ No | ❌ No |
| **UI** | ❌ No | ✅ Yes | ❌ No |
| **Best For** | Automation, Integration | End Users | Testing |

---

## Use Cases

### 1. Automated Monitoring

```csharp
// Check system health every 5 minutes
while (true)
{
    var info = await client.CallToolAsync("get_system_info", new { });
    await MonitoringService.RecordAsync(info);
    await Task.Delay(TimeSpan.FromMinutes(5));
}
```

### 2. Integration with Existing Apps

```csharp
// Add MCP capabilities to your app
public class DiagnosticsService
{
    private readonly McpClient _client;
    
    public async Task<SystemInfo> GetSystemInfoAsync()
    {
        var result = await _client.CallToolAsync("get_system_info", new { });
        return ParseSystemInfo(result);
    }
}
```

### 3. Testing and CI/CD

```csharp
// Integration test
[Test]
public async Task TestGetSystemInfo()
{
    using var client = new McpClient(serverPath);
    await client.StartAsync();
    await client.InitializeAsync();
    
    var result = await client.CallToolAsync("get_system_info", new { });
    Assert.IsNotNull(result);
}
```

---

## Troubleshooting

### Server Won't Start

**Problem**: `Failed to start server process`

**Solution**:
```powershell
# Test server manually first
dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
```

### No Response

**Problem**: `No response from server`

**Solution**:
- Check that server is sending to `stdout`, not `stderr`
- Verify JSON formatting
- Look for server errors in `StandardError`

### Process Exited

**Problem**: `Server process exited unexpectedly`

**Solution**:
```csharp
// Read error output
var error = await _serverProcess.StandardError.ReadToEndAsync();
Console.WriteLine($"Server error: {error}");
```

---

## Next Steps

### Milestone 2 Features

When you add more tools in Milestone 2, your client can easily call them:

```csharp
// List processes (Milestone 2)
var processes = await client.CallToolAsync("list_processes", new { });

// Get process details (Milestone 2)
var details = await client.CallToolAsync("get_process_details", new
{
    pid = 1234
});
```

### Adding Features

Ideas for enhancement:
- ✅ Async tool calls
- ✅ Batch operations
- ✅ Progress reporting
- ✅ Cancellation support
- ✅ Connection pooling
- ✅ Retry logic

---

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [JSON-RPC 2.0 Spec](https://www.jsonrpc.org/specification)
- [System.Text.Json Docs](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/)
- [Process Class Docs](https://learn.microsoft.com/dotnet/api/system.diagnostics.process)

---

## Complete Example

See `WinDiagMcpClient/Program.cs` for the complete, working implementation that demonstrates:
- ✅ Process management
- ✅ JSON-RPC protocol
- ✅ Tool discovery
- ✅ Tool execution
- ✅ Error handling
- ✅ Clean shutdown

Run it with:
```powershell
.\run-csharp-client.ps1
```

---

**This demonstrates how easy it is to build MCP clients in C#!** 🚀
