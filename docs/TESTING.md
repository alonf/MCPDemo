# Testing Guide

## Quick Start - Three Testing Methods

### 1. mcp-cli (Command Line) ⚡

Fastest way to verify your MCP server works:

```powershell
# Build
dotnet build

# List tools
mcp-cli tools --server windiag --config-file server_config.json

# Execute tool
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
```

**Perfect for**: Development, CI/CD, quick verification

### 2. MCP Inspector (Visual) 🔍

Interactive web interface for exploring and testing:

```powershell
# Install (one-time)
npm install -g @modelcontextprotocol/inspector

# Launch
.\launch-inspector.ps1
```

**In the browser:**
1. Click **"Connect"** in left panel
2. Click **"Tools"** tab at top
3. Select **"get_system_info"**
4. Click **"Call Tool"**
5. See formatted JSON response!

**Perfect for**: Development, debugging, learning MCP

### 3. HTTP/SSE (Direct Protocol) 🔧

Test the HTTP/SSE protocol directly:

```powershell
# Start server manually
dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj -- --urls=http://localhost:5000

# In another terminal, use curl or similar to connect to SSE endpoint
curl "http://localhost:5000/sse?apiKey=secure-mcp-key"
```

**Perfect for**: Understanding the protocol, debugging, remote access

## Comparison

| Method | Setup | Visual UI | Real-time | Best For |
|--------|-------|-----------|-----------|----------|
| **mcp-cli** | ⭐ Easy | ❌ No | ❌ No | Quick tests |
| **Inspector** | ⭐⭐ Medium | ✅ Yes | ✅ Yes | Learning |
| **HTTP/SSE** | ⭐⭐ Medium | ❌ No | ✅ Yes | Protocol study |

## Automated Testing

Run the full test suite:

```powershell
.\test-mcp-server.ps1
```

This script:
1. Builds the server
2. Tests connectivity
3. Lists tools
4. Executes `get_system_info`
5. Validates output

## Example Output

```json
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
```

## Troubleshooting

### Server Won't Start
```powershell
# Check build errors
dotnet build

# Run directly to see output
dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
```

### Tool Not Found
- Verify `server_config.json` path is correct
- Check .NET SDK is in PATH
- Rebuild: `dotnet clean && dotnet build`

### Inspector Can't Connect
- Make sure you clicked "Connect" button
- Check server is building successfully
- Look at "Server Notifications" for errors
- Try restarting: Ctrl+C and run `.\launch-inspector.ps1` again

## For CI/CD

```yaml
# Example GitHub Actions
- name: Test MCP Server
  run: |
    dotnet build
    mcp-cli tools --server windiag --config-file server_config.json
    mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
```

## Resources

- [MCP Inspector Guide](MCP_INSPECTOR_GUIDE.md) - Detailed Inspector usage
- [MCP Specification](https://modelcontextprotocol.io/) - Official protocol docs
