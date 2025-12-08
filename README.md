# WinDiag MCP Server - Lecture Demo

A simple Model Context Protocol (MCP) server demonstrating Windows diagnostics capabilities. Built with .NET 10 and showcasing the fundamentals of MCP server development.

## What This Demo Shows

This is a **basic MCP server** with:
- ✅ Single tool: `get_system_info`
- ✅ STDIO transport (standard input/output)
- ✅ .NET 10 implementation
- ✅ Multiple testing methods

Perfect for learning MCP basics!

## Quick Start

### 1. Build the Server
```powershell
dotnet build
```

### 2. Test with mcp-cli (Command Line)
```powershell
# List available tools
mcp-cli tools --server windiag --config-file server_config.json

# Execute the tool
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
```

### 3. Test with MCP Inspector (Visual Interface)
```powershell
# Install (one-time)
npm install -g @modelcontextprotocol/inspector

# Launch visual testing UI
.\launch-inspector.ps1
```

Then in the browser:
1. Click "Connect"
2. Click "Tools" tab
3. Click "get_system_info"
4. Click "Call Tool"
5. See your system information!

## The Tool

### `get_system_info`
Returns comprehensive Windows system diagnostics.

**Parameters**: None

**Returns**:
```json
{
  "machineName": "COMPUTER-NAME",
  "userName": "username",
  "osDescription": "Microsoft Windows 10.0.22631",
  "osArchitecture": "X64",
  "processArchitecture": "X64",
  "processorCount": 44,
  "frameworkDescription": ".NET 10.0.0",
  "currentDirectory": "C:\\path\\to\\project",
  "systemUpTime": "14.10:10:59.7340000"
}
```

## Testing Methods

| Method | Visual | Interactive | Best For |
|--------|--------|-------------|----------|
| **mcp-cli** | ❌ No | ❌ No | Quick tests, CI/CD |
| **MCP Inspector** | ✅ Yes | ✅ Yes | Development, learning |
| **HTTP/REST** | ❌ No | ❌ No | Protocol understanding |

## Project Structure

```
MCPDemo/
├── WinDiagMcpServer/           # .NET 10 MCP server
│   ├── Program.cs              # Server setup
│   ├── SystemInfoResult.cs     # Data model
│   └── ConsoleUi.cs            # UI formatting
├── server_config.json          # MCP configuration
├── launch-inspector.ps1        # Launch Inspector
├── test-mcp-server.ps1         # Automated tests
└── docs/                       # Documentation
    ├── TESTING.md              # Testing guide
    └── MCP_INSPECTOR_GUIDE.md  # Inspector guide
```

## Requirements

- .NET 10 SDK
- Node.js 16+ (for Inspector)
- Python 3.8+ (for mcp-cli)

## Documentation

- **[TESTING.md](docs/TESTING.md)** - Complete testing guide
- **[MCP_INSPECTOR_GUIDE.md](docs/MCP_INSPECTOR_GUIDE.md)** - Inspector guide  
- **[QUICKSTART.md](QUICKSTART.md)** - Step-by-step tutorial

## License

MIT License

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
