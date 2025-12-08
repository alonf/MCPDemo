# Testing MCP Servers - Practical Guide

## Overview

This guide shows you how to test your WinDiag MCP Server using various tools. We'll focus on what actually works and is practical for development and testing.

## Quick Test - No LLM Required ✅

The simplest way to verify your MCP server works:

```powershell
# Build your server
dotnet build

# Test tool discovery
mcp-cli tools --server windiag --config-file server_config.json

# Execute a tool directly
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
```

**This works perfectly** and is great for:
- ✅ Verifying your server starts correctly
- ✅ Testing tool registration
- ✅ Debugging tool implementations
- ✅ CI/CD pipelines
- ✅ Quick development iterations

## Testing with LLM Integration

For testing MCP servers with actual LLM interaction, you have several options:

### Option 1: Claude Desktop ⭐ Recommended

**Best for**: Production use, full MCP features, best user experience

**Setup**:
1. Download: https://claude.ai/download
2. Configure (`%APPDATA%\Claude\claude_desktop_config.json`):
   ```json
   {
     "mcpServers": {
       "windiag": {
         "command": "dotnet",
         "args": ["run", "--project", "C:\\Dev\\MCPDemo\\WinDiagMcpServer\\WinDiagMcpServer.csproj"]
       }
     }
   }
   ```
3. Restart Claude Desktop
4. Ask: "What tools do you have access to?"
5. Test: "What is my system information?"

**Pros**:
- ✅ Official Anthropic implementation
- ✅ Best MCP support
- ✅ Works out of the box
- ✅ Great UI/UX

**Cons**:
- ❌ Requires Claude subscription for unlimited use
- ❌ Desktop app (not command line)

### Option 2: Cline (VS Code Extension) 🔧

**Best for**: Developers who live in VS Code

**Setup**:
1. Install Cline extension in VS Code
2. Configure MCP server in Cline settings:
   ```json
   {
     "mcpServers": {
       "windiag": {
         "command": "dotnet",
         "args": ["run", "--project", "${workspaceFolder}/WinDiagMcpServer/WinDiagMcpServer.csproj"]
       }
     }
   }
   ```
3. Open Cline panel
4. Configure to use local Ollama or cloud provider

**Pros**:
- ✅ Integrated with VS Code
- ✅ Supports multiple LLM providers (Ollama, OpenAI, etc.)
- ✅ Good for development workflow

**Cons**:
- ❌ Another VS Code extension to manage
- ❌ Setup complexity

### Option 3: Continue (VS Code Extension) 🔧

**Best for**: Developers wanting AI coding assistant with MCP

**Setup**:
1. Install Continue extension in VS Code
2. Configure in Continue settings (`~/.continue/config.json`):
   ```json
   {
     "mcpServers": {
       "windiag": {
         "command": "dotnet",
         "args": ["run", "--project", "WinDiagMcpServer/WinDiagMcpServer.csproj"]
       }
     },
     "models": [
       {
         "title": "Ollama",
         "provider": "ollama",
         "model": "llama3.2"
       }
     ]
   }
   ```

**Pros**:
- ✅ AI coding assistant features
- ✅ Works with local Ollama
- ✅ Free and open source

**Cons**:
- ❌ Focused more on coding assistance than general chat

### Option 4: MCP Inspector 🔍

**Best for**: Debugging and development

**Setup**:
```powershell
# Install
npm install -g @modelcontextprotocol/inspector

# Run
mcp-inspector dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
```

**What it provides**:
- Interactive web UI
- Tool exploration
- Real-time testing
- Request/response inspection
- No LLM required

**Pros**:
- ✅ Official MCP debugging tool
- ✅ Great for development
- ✅ Visual interface
- ✅ No LLM needed

**Cons**:
- ❌ Requires Node.js
- ❌ Not for production use

### Option 5: mcp-cli (Limited) ⚠️

**Best for**: Simple tool execution, CI/CD

**What works**:
```powershell
# List tools
mcp-cli tools --server windiag --config-file server_config.json

# Execute specific tool
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info

# Ping server
mcp-cli ping --server windiag --config-file server_config.json
```

**What doesn't work reliably**:
- ❌ LLM provider integration (broken in current version)
- ❌ Interactive chat mode
- ❌ Azure OpenAI provider

**Use for**:
- ✅ Automated testing
- ✅ CI/CD pipelines
- ✅ Quick tool execution
- ✅ Development verification

## Comparison Table

| Tool | LLM Integration | Setup Complexity | Best For | Cost |
|------|----------------|------------------|----------|------|
| **mcp-cli** (tool only) | ❌ No | ⭐ Easy | Quick testing, CI/CD | Free |
| **MCP Inspector** | ❌ No | ⭐⭐ Medium | Development, debugging | Free |
| **Claude Desktop** | ✅ Yes | ⭐ Easy | Production, full features | Paid/Free tier |
| **Cline (VS Code)** | ✅ Yes | ⭐⭐ Medium | VS Code developers | Free |
| **Continue (VS Code)** | ✅ Yes | ⭐⭐ Medium | Coding assistance | Free |

## Recommended Workflow

### For Development
1. **Use mcp-cli for quick tests**:
   ```powershell
   mcp-cli tools --server windiag --config-file server_config.json
   mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
   ```

2. **Use MCP Inspector for debugging**:
   ```powershell
   mcp-inspector dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
   ```

### For End-to-End Testing with LLM
3. **Use Claude Desktop** (easiest):
   - One-time setup
   - Best user experience
   - Full MCP support

### For CI/CD
4. **Use mcp-cli in scripts**:
   ```powershell
   # In your build pipeline
   dotnet build
   mcp-cli tools --server windiag --config-file server_config.json
   if ($LASTEXITCODE -ne 0) { exit 1 }
   ```

## Quick Start Guide

### Step 1: Verify Server Works
```powershell
# Build
dotnet build

# Test with mcp-cli (no LLM)
mcp-cli tools --server windiag --config-file server_config.json
```

### Step 2: Test Tool Execution
```powershell
# Execute tool directly
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info

# Expected output:
# {
#   "isError": false,
#   "content": "...system info JSON..."
# }
```

### Step 3: (Optional) Test with LLM

Choose one based on your needs:

**Quick & Easy**: Claude Desktop
**VS Code User**: Cline or Continue extension
**Debugging**: MCP Inspector

## Testing Checklist

Use this checklist to verify your MCP server:

### Basic Functionality
- [ ] Server starts without errors
- [ ] `mcp-cli tools` lists your tools
- [ ] `mcp-cli cmd` can execute tools
- [ ] Tool returns expected output format

### LLM Integration (Optional)
- [ ] Claude Desktop can discover tools
- [ ] LLM can successfully call tools
- [ ] Tool results are properly formatted
- [ ] Error handling works correctly

### Production Readiness
- [ ] Tool descriptions are clear
- [ ] Input validation works
- [ ] Error messages are helpful
- [ ] Performance is acceptable

## Example: Complete Test Session

```powershell
# 1. Build
PS> dotnet build
Build succeeded.

# 2. List tools
PS> mcp-cli tools --server windiag --config-file server_config.json
✓ Total tools available: 1
Tool: get_system_info
Description: Returns basic system information

# 3. Execute tool
PS> mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
{
  "isError": false,
  "content": {
    "machineName": "HOMEALON11",
    "osDescription": "Microsoft Windows 10.0.22631",
    "processorCount": 44,
    "frameworkDescription": ".NET 10.0.0"
  }
}

# 4. Test in Claude Desktop
Open Claude Desktop → Ask "What is my system info?"
Claude responds with formatted system information
```

## Troubleshooting

### Server won't start
```powershell
# Check build errors
dotnet build

# Run server directly to see errors
dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
```

### mcp-cli can't find server
- Verify `server_config.json` paths are correct
- Use absolute paths if needed
- Check that .NET SDK is in PATH

### Tools not showing up
- Check tool registration in `Program.cs`
- Verify JSON schema is valid
- Look for exceptions in server output

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
- [Claude Desktop](https://claude.ai/download)
- [Cline Extension](https://marketplace.visualstudio.com/items?itemName=saoudrizwan.claude-dev)
- [Continue Extension](https://marketplace.visualstudio.com/items?itemName=Continue.continue)

## Conclusion

**For teaching MCP development**:
- Use `mcp-cli` for basic testing (no LLM needed)
- Use MCP Inspector for interactive debugging
- Use Claude Desktop for demonstration of full functionality

**Don't rely on mcp-cli for LLM integration** - it's unreliable and broken in many installations. Focus on what works: tool execution and testing with proper MCP clients.
