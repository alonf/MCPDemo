# MCP Inspector - The Visual Way to Test MCP Servers

## What is MCP Inspector?

**MCP Inspector is like Postman for MCP servers** - a visual, interactive tool for exploring and testing MCP server capabilities.

## Why Use MCP Inspector?

### The Problem with CLI-Only Testing

Using only `mcp-cli`:
```powershell
PS> mcp-cli tools --server windiag --config-file server_config.json
# Get text output...

PS> mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
# Get JSON response...
# Copy/paste to verify...
# No visual feedback...
```

❌ Hard to see what's available
❌ Can't explore interactively
❌ No schema visualization
❌ Difficult to debug
❌ Not beginner-friendly

### The Inspector Solution

```powershell
PS> mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
# Browser opens with visual UI!
```

✅ See all tools at a glance
✅ Click to execute
✅ Visual schema display
✅ Real-time request/response
✅ Perfect for learning

## Visual Comparison

### CLI Testing (Old Way)
```
Terminal Only                          Result
┌──────────────────────────────┐      ┌──────────────────────────┐
│ PS> mcp-cli tools            │      │ Text list of tools       │
│ PS> mcp-cli cmd --tool X     │ -->  │ JSON dump to console     │
│ PS> # copy paste to verify   │      │ Hard to read             │
└──────────────────────────────┘      └──────────────────────────┘
```

### Inspector Testing (Modern Way)
```
Browser Interface                      Interactive
┌──────────────────────────────┐      ┌──────────────────────────┐
│ [Tools] [Logs] [Schema]      │      │ Click "Execute"          │
│                              │      │ See formatted response   │
│ ● get_system_info            │ -->  │ Explore schema visually  │
│   [Execute]                  │      │ Debug with logs          │
└──────────────────────────────┘      └──────────────────────────┘
```

## Inspector Capabilities

### 1. Tool Discovery
**What you see**:
- List of all available tools
- Tool names and descriptions
- Input parameter schemas
- Return value schemas

**Why it matters**:
- Understand what your server offers
- Verify tool registration
- Check schema correctness

### 2. Interactive Execution
**What you can do**:
- Click a button to execute tools
- See results immediately
- No command-line syntax needed
- Perfect for beginners

**Why it matters**:
- Faster testing cycles
- Lower barrier to entry
- Great for demos

### 3. Schema Visualization
**What you see**:
```json
{
  "name": "get_system_info",
  "description": "Returns system information",
  "inputSchema": {
    "type": "object",
    "properties": {},
    "required": []
  }
}
```

**Why it matters**:
- Verify schema correctness
- Understand tool contracts
- Catch errors early

### 4. Request/Response Logs
**What you see**:
```
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

**Why it matters**:
- Debug protocol issues
- Understand MCP communication
- Learn JSON-RPC format

### 5. Hot Reload
**What happens**:
1. Modify your server code
2. Rebuild with `dotnet build`
3. Inspector automatically reconnects
4. Test immediately

**Why it matters**:
- Rapid development cycles
- Instant feedback
- No manual restarts

### 6. Error Visualization
**What you see**:
- Clear error messages
- Stack traces (if available)
- Protocol error details
- Validation failures

**Why it matters**:
- Faster debugging
- Better error understanding
- Learn from mistakes

## Use Cases

### For Beginners
✅ **Learn MCP visually**
- See what tools look like
- Understand schemas
- Practice executing tools
- Build confidence

### For Developers
✅ **Rapid development**
- Test new tools immediately
- Verify schemas
- Debug protocol issues
- Iterate quickly

### For Teachers
✅ **Teach MCP effectively**
- Show visual interface
- Demonstrate tool execution
- Explain schemas clearly
- Engage students

### For Debugging
✅ **Find issues faster**
- See request/response
- Check schema validation
- Inspect error messages
- Understand protocol

## Comparison: Inspector vs Other Tools

### vs mcp-cli
| Feature | mcp-cli | Inspector |
|---------|---------|-----------|
| Visual Interface | ❌ No | ✅ Yes |
| Interactive | ❌ No | ✅ Yes |
| Schema View | ❌ No | ✅ Yes |
| Logs | ❌ No | ✅ Yes |
| Easy for Beginners | ❌ No | ✅ Yes |
| CI/CD Friendly | ✅ Yes | ❌ No |

**Use mcp-cli for**: Automated testing, CI/CD
**Use Inspector for**: Development, learning, debugging

### vs Claude Desktop
| Feature | Claude Desktop | Inspector |
|---------|----------------|-----------|
| Visual Interface | ✅ Yes | ✅ Yes |
| Tool Execution | ✅ Yes | ✅ Yes |
| LLM Integration | ✅ Yes | ❌ No |
| Schema View | ❌ No | ✅ Yes |
| Request/Response Logs | ❌ No | ✅ Yes |
| Development Focus | ❌ No | ✅ Yes |

**Use Claude Desktop for**: Production, end-user experience
**Use Inspector for**: Development, debugging, testing

## Getting Started

### Installation
```powershell
# One-time installation
npm install -g @modelcontextprotocol/inspector
```

### Launch
```powershell
# Easy way
.\launch-inspector.ps1

# Manual way
mcp-inspector dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj
```

### First Steps
1. Browser opens at http://localhost:5173
2. See connection status (green = connected)
3. Click "Tools" tab
4. See your tools listed
5. Click on `get_system_info`
6. Click "Execute" button
7. See the response

### Explore
- Try the "Logs" tab to see JSON-RPC messages
- Modify your server code
- Rebuild (`dotnet build`)
- See Inspector reconnect automatically
- Test your changes immediately

## Best Practices

### For Development
1. **Start Inspector first**: Launch before coding
2. **Keep it open**: Use for continuous testing
3. **Check logs**: Understand protocol details
4. **Verify schemas**: Catch errors early

### For Learning
1. **Explore visually**: Click around the UI
2. **Read schemas**: Understand tool contracts
3. **Watch logs**: Learn JSON-RPC
4. **Experiment**: Try different tools

### For Teaching
1. **Show first**: Demonstrate the UI
2. **Explain schemas**: Use visual display
3. **Live coding**: Build tools, test immediately
4. **Compare**: Show vs CLI and vs Claude

## Troubleshooting

### Inspector Won't Start
```powershell
# Check Node.js
node --version  # Need 16+

# Reinstall
npm install -g @modelcontextprotocol/inspector
```

### Can't Connect
```powershell
# Test server manually
dotnet run --project WinDiagMcpServer\WinDiagMcpServer.csproj

# Check for errors
dotnet build
```

### Port Conflict
- Inspector uses port 5173
- Stop other services on that port
- Or manually open http://localhost:5173

## Resources

- [MCP Inspector Guide](MCP_INSPECTOR_GUIDE.md) - Complete documentation
- [Official Inspector Repo](https://github.com/modelcontextprotocol/inspector)
- [MCP Specification](https://modelcontextprotocol.io/)

## Conclusion

**MCP Inspector is the best tool for MCP development** because:

✅ **Visual** - See everything at a glance
✅ **Interactive** - Click to test
✅ **Educational** - Learn by exploring
✅ **Fast** - Rapid iteration
✅ **Comprehensive** - Logs, schemas, errors

**Think of it as**:
- Postman for MCP
- Visual Studio for protocols
- DevTools for MCP servers

**Perfect for**:
- 🎓 Learning MCP
- 💻 Developing servers
- 🐛 Debugging issues
- 👨‍🏫 Teaching MCP

Start using it today:
```powershell
npm install -g @modelcontextprotocol/inspector
.\launch-inspector.ps1
```
