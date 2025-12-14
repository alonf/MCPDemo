# Testing Guide

This repo supports two reliable ways to test the WinDiag MCP Server (Streamable HTTP at `/mcp` with API key):

1. **Automated smoke test script** (fast, automatable)
2. **MCP Inspector** (visual UI, best for development/debugging)

## 1) Automated Smoke Test Script ⚡

Fastest way to verify the server works end-to-end (no LLM required):

```powershell
./test-mcp-server.ps1
```

This script:
1. Builds the server
2. Starts it locally
3. Performs an MCP handshake
4. Discovers tools
5. Executes `get_system_info`

Use this for quick verification and automation.

## 2) MCP Inspector (Visual) 🔍

Inspector is the best way to explore tools/resources/prompts and to debug protocol issues visually.

### Install (one-time)

```powershell
npm install -g @modelcontextprotocol/inspector
```

### Launch

```powershell
./launch-inspector.ps1
```

### In the browser

1. Click **Connect**
2. Open the **Tools** tab
3. Select **get_system_info**
4. Click **Call Tool**

Tip: The **Logs** tab is great for inspecting raw JSON-RPC requests/responses.

## Comparison

| Method | Setup | Visual UI | Real-time | Best For |
|--------|-------|-----------|-----------|----------|
| **test-mcp-server.ps1** | ⭐ Easy | ❌ No | ❌ No | Quick tests |
| **Inspector** | ⭐⭐ Medium | ✅ Yes | ✅ Yes | Learning |

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

### Tools Not Showing Up
- Rebuild the server: `dotnet clean; dotnet build`
- Re-run `./test-mcp-server.ps1` to confirm the server responds
- In Inspector, reconnect and check the **Logs** tab for errors

### Inspector Can't Connect
- Make sure you clicked "Connect" button
- Check server is building successfully
- Look at "Server Notifications" for errors
- Try restarting: Ctrl+C and run `.\launch-inspector.ps1` again

### Inspector Won't Start
- Confirm Node.js is installed: `node --version` (16+)
- Reinstall Inspector: `npm install -g @modelcontextprotocol/inspector`

### Browser Doesn't Open
- Manually open: `http://localhost:5173`

### Port Already In Use
- Inspector uses port `5173` by default
- Stop the other process using that port, then re-run `./launch-inspector.ps1`

## For CI/CD

```yaml
# Example GitHub Actions
- name: Test MCP Server
  run: |
    pwsh -File ./test-mcp-server.ps1
```

## Resources

- [MCP Specification](https://modelcontextprotocol.io/) - Official protocol docs
