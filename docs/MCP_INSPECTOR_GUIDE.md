# MCP Inspector Setup Guide

## Installation

### Prerequisites
- Node.js (version 16 or higher)
- npm (comes with Node.js)

### Install MCP Inspector

```powershell
# Install globally
npm install -g @modelcontextprotocol/inspector

# Verify installation
mcp-inspector --version
```

## Using MCP Inspector with WinDiag Server

### Method 1: Direct Launch (Recommended)

```powershell
# Navigate to your project root
cd C:\Dev\MCPDemo

# Launch inspector with your server
mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
```

This will:
1. Start your .NET MCP server
2. Launch MCP Inspector
3. Open a browser with the Inspector UI
4. Connect automatically to your server

### Method 2: Using Server Config

```powershell
# If you have server_config.json
mcp-inspector --config server_config.json
```

## What You'll See

### Inspector UI Features

1. **Server Info**
   - Connection status
   - Server capabilities
   - Protocol version

2. **Tools Tab**
   - List of all available tools
   - Tool descriptions
   - Input schemas
   - Try tools directly

3. **Resources Tab** (if your server provides resources)
   - Available resources
   - Resource URIs
   - Content types

4. **Prompts Tab** (if your server provides prompts)
   - Available prompts
   - Prompt templates

5. **Logs Tab**
   - Request/response history
   - JSON-RPC messages
   - Error details

## Testing Your WinDiag Server

### Step-by-Step Testing

1. **Launch Inspector**:
   ```powershell
   mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
   ```

2. **Verify Connection**:
   - Check status indicator (should be green/connected)
   - See server name: "WinDiag MCP Server"

3. **Explore Tools**:
   - Click "Tools" tab
   - See `get_system_info` tool
   - View tool description and schema

4. **Execute Tool**:
   - Click on `get_system_info`
   - Click "Call Tool" button
   - See the JSON response with system information

5. **Inspect Messages**:
   - Go to "Logs" tab
   - See the JSON-RPC request
   - See the JSON-RPC response
   - Useful for debugging

## Example Session

```
1. Start Inspector:
   PS> mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
   
   Output:
   Starting MCP Inspector...
   Server started
   Opening browser at http://localhost:5173
   
2. Browser opens showing:
   
   ┌─────────────────────────────────────┐
   │   MCP Inspector                     │
   ├─────────────────────────────────────┤
   │ Status: ● Connected                 │
   │ Server: WinDiag MCP Server          │
   │                                     │
   │ [Tools] [Resources] [Prompts] [Logs]│
   └─────────────────────────────────────┘

3. Click Tools tab:
   
   Available Tools (1):
   
   ┌──────────────────────────────────────┐
   │ get_system_info                      │
   ├──────────────────────────────────────┤
   │ Returns basic system information     │
   │ for diagnostics                      │
   │                                      │
   │ Parameters: (none)                   │
   │                                      │
   │ [Call Tool]                          │
   └──────────────────────────────────────┘

4. Click "Call Tool":
   
   Response:
   {
     "machineName": "HOMEALON11",
     "userName": "alon",
     "osDescription": "Microsoft Windows 10.0.22631",
     "osArchitecture": "X64",
     "processArchitecture": "X64",
     "processorCount": 44,
     "frameworkDescription": ".NET 10.0.0",
     "currentDirectory": "C:\\Dev\\MCPDemo",
     "systemUpTime": "14.10:10:59.7340000"
   }

5. Check Logs tab:
   
   Request:
   {
     "jsonrpc": "2.0",
     "method": "tools/call",
     "params": {
       "name": "get_system_info",
       "arguments": {}
     },
     "id": 1
   }
   
   Response:
   {
     "jsonrpc": "2.0",
     "result": {
       "content": [...]
     },
     "id": 1
   }
```

## Advanced Features

### Hot Reload
- Modify your server code
- Rebuild with `dotnet build`
- Inspector automatically reconnects
- Great for rapid development

### Schema Validation
- Inspector validates tool schemas
- Shows errors if schema is malformed
- Helps catch bugs early

### Multiple Tools
- Test all tools in sequence
- Compare responses
- Build test scenarios

### Error Handling
- See error messages clearly
- Inspect error stack traces
- Debug protocol issues

## Troubleshooting

### Inspector Won't Start
```powershell
# Check Node.js version
node --version  # Should be 16+

# Reinstall if needed
npm install -g @modelcontextprotocol/inspector
```

### Can't Connect to Server
```powershell
# Verify server starts manually first
dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj

# Check for build errors
dotnet build
```

### Port Already in Use
```powershell
# Inspector uses port 5173 by default
# Stop other services using that port
# Or specify different port (if supported)
```

### Browser Doesn't Open
```powershell
# Manually open: http://localhost:5173
# After starting inspector
```

## Comparison with Other Tools

| Feature | mcp-cli | MCP Inspector | Claude Desktop |
|---------|---------|---------------|----------------|
| Visual UI | ❌ No | ✅ Yes | ✅ Yes |
| Tool Execution | ✅ Yes | ✅ Yes | ✅ Yes |
| Schema Viewing | ❌ No | ✅ Yes | ❌ No |
| Message Logs | ❌ No | ✅ Yes | ❌ No |
| LLM Integration | ⚠️ Broken | ❌ No | ✅ Yes |
| Development Focus | ✅ Yes | ✅ Yes | ❌ No |

## Best Practices

### For Development
1. Use Inspector for initial development
2. Test each new tool immediately
3. Verify schemas are correct
4. Check error handling

### For Debugging
1. Open Logs tab first
2. Execute failing tool
3. Inspect request/response
4. Look for protocol errors

### For Documentation
1. Use Inspector to explore tools
2. Screenshot the UI
3. Document tool schemas
4. Share with team

## Integration with VS Code

### Quick Launch Task
Add to `.vscode/tasks.json`:
```json
{
  "version": "2.0.0",
  "tasks": [
    {
      "label": "MCP Inspector",
      "type": "shell",
      "command": "mcp-inspector",
      "args": [
        "dotnet",
        "run",
        "--project",
        "WinDiagMcpServer/WinDiagMcpServer.csproj"
      ],
      "problemMatcher": []
    }
  ]
}
```

Then run: `Terminal` → `Run Task` → `MCP Inspector`

## Resources

- [MCP Inspector GitHub](https://github.com/modelcontextprotocol/inspector)
- [MCP Specification](https://modelcontextprotocol.io/)
- [Inspector Documentation](https://modelcontextprotocol.io/docs/tools/inspector)

## Next Steps

1. **Install Inspector**:
   ```powershell
   npm install -g @modelcontextprotocol/inspector
   ```

2. **Test Your Server**:
   ```powershell
   mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
   ```

3. **Explore the UI**:
   - Check connection status
   - View tools
   - Execute get_system_info
   - Inspect logs

4. **Develop New Tools**:
   - Add tool to Program.cs
   - Rebuild
   - Inspector reconnects automatically
   - Test immediately

Inspector is the perfect tool for MCP development - it's like having Postman specifically designed for MCP servers!
