# WinDiag MCP Server - Lecture Demo

A simple Model Context Protocol (MCP) server demonstrating Windows diagnostics capabilities. Built with .NET 10 and showcasing the fundamentals of MCP server development.

## What This Demo Shows

This is a **basic MCP server** with:
- ✅ System information tool: `get_system_info`
- ✅ Event log snapshot tool: `create_event_log_snapshot`
- ✅ Event log snapshot resources: `eventlog://snapshot/{id}`
- ✅ STDIO transport (standard input/output)
- ✅ .NET 10 implementation
- ✅ Multiple testing methods
- ✅ Claude Desktop integration
- ✅ C# MCP client implementation

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

### 4. Test with Claude Desktop (LLM Integration) ⭐ Recommended
```powershell
# Automated setup
.\setup-claude-desktop.ps1
```

Then in Claude Desktop:
1. Restart Claude Desktop
2. Ask: "What tools do you have access to?"
3. Ask: "What is my system information?"
4. Claude uses the MCP server to get your system info!

See [CLAUDE_DESKTOP_SETUP.md](docs/CLAUDE_DESKTOP_SETUP.md) for detailed instructions.

## The Tools

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

### `create_event_log_snapshot`
Creates a snapshot of Windows Event Log entries and returns an MCP resource URI.

**Parameters**:
- `logName`: Name of the event log (Application, Security, Setup, System, ForwardedEvents)
- `xPathQuery`: XPath query to filter events

**Returns**:
```json
{
  "resourceUri": "eventlog://snapshot/abc123...",
  "snapshotId": "abc123...",
  "eventCount": 42
}
```

**Example Usage**:
```
Ask Claude: "Create a snapshot of Application log errors from the last 24 hours"
Claude will:
1. Call create_event_log_snapshot with appropriate XPath query
2. Receive the resource URI
3. Use the resource URI to fetch the snapshot data
4. Analyze and present the findings
```

## Resources

### `eventlog://snapshot/{id}`
MCP resource containing event log snapshot data in JSON format.

Accessed via the MCP resource protocol when Claude or another client needs to fetch the actual snapshot data.

## Testing Methods

| Method | Visual | Interactive | LLM | Programmatic | Best For |
|--------|--------|-------------|-----|--------------|----------|
| **mcp-cli** | ❌ No | ❌ No | ❌ No | ⚠️ Limited | Quick tests, CI/CD |
| **MCP Inspector** | ✅ Yes | ✅ Yes | ❌ No | ❌ No | Development, debugging |
| **Claude Desktop** | ✅ Yes | ✅ Yes | ✅ Yes | ❌ No | Demos, production |

## Project Structure

```
MCPDemo/
├── WinDiagMcpServer/            # .NET 10 MCP server
│   ├── Program.cs               # Server setup
│   ├── McpServerEventLogToolType.cs    # Event log tools
│   ├── McpServerEventLogResourceType.cs # Event log resources
│   ├── EventLogSnapshotStorage.cs      # In-memory storage
│   └── ConsoleUi.cs             # UI formatting
├── server_config.json           # MCP configuration
├── setup-claude-desktop.ps1     # Claude Desktop setup (Windows)
├── setup-claude-desktop.sh      # Claude Desktop setup (macOS/Linux)
├── launch-inspector.ps1         # Launch Inspector
└── docs/                        # Documentation
    ├── TESTING.md               # Testing guide
    ├── MCP_INSPECTOR_GUIDE.md   # Inspector guide
    └── CLAUDE_DESKTOP_SETUP.md  # Claude Desktop guide
```

## Requirements

- .NET 10 SDK
- Node.js 16+ (for Inspector)
- Python 3.8+ (for mcp-cli)
- Claude Desktop (for LLM integration)

## Documentation

- **[CLAUDE_DESKTOP_SETUP.md](docs/CLAUDE_DESKTOP_SETUP.md)** - Claude Desktop integration ⭐
- **[TESTING.md](docs/TESTING.md)** - Complete testing guide
- **[MCP_INSPECTOR_GUIDE.md](docs/MCP_INSPECTOR_GUIDE.md)** - Inspector guide
- **[QUICKSTART.md](QUICKSTART.md)** - Step-by-step tutorial
- **[MCP_DEMO_Roadmap.md](docs/MCP_DEMO_Roadmap.md)** - Development roadmap

## License

MIT License

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
