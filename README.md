# WinDiag MCP Server - Lecture Demo

A comprehensive Model Context Protocol (MCP) server demonstrating Windows diagnostics capabilities with AI-powered workflows. Built with .NET 10 and showcasing MCP tools, resources, and prompts.

## What This Demo Shows

This is a **production-ready MCP server** with:
- ✅ **Tools**: System information, process management, event log snapshots
- ✅ **Resources**: Event log snapshot resources with pagination support
- ✅ **Prompts**: AI-guided diagnostic workflows
- ✅ **AI Chat Client**: Interactive diagnostics with Azure OpenAI
- ✅ STDIO transport (standard input/output)
- ✅ .NET 10 implementation
- ✅ Multiple testing methods
- ✅ C# MCP client implementation

Perfect for learning MCP concepts and building AI-powered diagnostics!

## Quick Start

### 1. Build the Server
```powershell
dotnet build
```

### 2. Test with mcp-cli (Command Line)
```powershell
# List available tools
mcp-cli tools --server windiag --config-file server_config.json

# Execute a tool
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
2. Explore Tools, Resources, and Prompts tabs
3. Test tools and view resources
4. Try MCP prompts for diagnostic workflows

### 4. Use the AI Chat Client
```powershell
cd WinDiagMcpChat
dotnet run
```

Interactive diagnostics with:
- Azure OpenAI integration
- Automatic prompt discovery
- AI-guided workflows
- Resource pagination handling

## The Tools

### System Information
- **`get_system_info`** - Returns comprehensive Windows system diagnostics

### Process Management
- **`get_all_processes`** - Lists all running processes with CPU, memory, threads, handles
- **`get_process_info`** - Gets detailed information for a specific process by ID

### Event Log Management
- **`create_event_log_snapshot`** - Creates a snapshot of Windows Event Log entries
  - Returns an MCP resource URI for pagination support
  - Supports XPath queries for filtering
  - Stores snapshots for later access

### WMI Troubleshooting
- **`troubleshoot_with_wmi`** - AI-assisted WMI diagnostics
  - Uses MCP Sampling to generate safe WMI queries from natural language
  - Executes queries with guardrails
  - Analyzes results automatically

## The Resources

### `eventlog://snapshot/{id}`
MCP resource containing event log snapshot data in JSON format with pagination support.

**Query Parameters**:
- `limit` - Number of events per page (default: 100)
- `offset` - Starting position (default: 0)

**Example**: `eventlog://snapshot/abc123?limit=20&offset=0`

**Returns**:
```json
{
  "SnapshotId": "abc123...",
  "LogName": "Application",
  "CreatedAt": "2025-01-15T10:30:00Z",
  "Pagination": {
    "TotalCount": 150,
    "ReturnedCount": 20,
    "Offset": 0,
    "Limit": 20,
    "HasMore": true,
    "NextOffset": 20
  },
  "Events": [...]
}
```

## The Prompts

MCP Prompts provide AI-guided diagnostic workflows:

### Event Log Analysis
- **`AnalyzeRecentApplicationErrors`** - Analyzes recent application errors from event logs
  - Parameters: `hoursBack` (default: 24)

### System Diagnostics
- **`ExplainHighCpu`** - Investigates processes causing high CPU usage
- **`DetectSecurityAnomalies`** - Detects security issues (requires elevation)
- **`DiagnoseSystemHealth`** - Comprehensive health check without elevation ⭐
  - Analyzes memory usage, crashes, performance issues
  - Correlates processes with event logs
  - Provides actionable recommendations

**Usage Example**:
```
Ask Claude: "Do a system health check"
Claude will:
1. Retrieve the DiagnoseSystemHealth prompt
2. Follow the step-by-step workflow
3. Call get_all_processes
4. Create event log snapshots
5. Read resources with pagination
6. Analyze patterns
7. Present comprehensive health report
```

## Projects

### WinDiagMcpServer
.NET 10 MCP server implementing tools, resources, and prompts for Windows diagnostics.

### WinDiagMcpChat
Interactive console chat client with:
- Azure OpenAI integration
- Automatic MCP tool discovery
- MCP prompt support
- Resource pagination handling
- Conversation history

## Testing Methods

| Method | Visual | Interactive | LLM | Prompts | Best For |
|--------|--------|-------------|-----|---------|----------|
| **mcp-cli** | ❌ No | ❌ No | ❌ No | ❌ No | Quick tests, CI/CD |
| **MCP Inspector** | ✅ Yes | ✅ Yes | ❌ No | ✅ Yes | Development, debugging |
| **Claude Desktop** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes | Demos, production |
| **WinDiagMcpChat** | ❌ No | ✅ Yes | ✅ Yes | ✅ Yes | Custom client dev |

## Project Structure

```
MCPDemo/
├── WinDiagMcpServer/                    # .NET 10 MCP server
│   ├── Program.cs                       # Server setup
│   ├── Tools/
│   │   ├── SystemInfo/                  # System info tool
│   │   ├── Process/                     # Process management tools
│   │   └── EventLog/                    # Event log tools
│   ├── Resources/
│   │   └── EventLog/                    # Event log resources with pagination
│   ├── Prompts/
│   │   ├── EventLog/                    # Event log analysis prompts
│   │   └── SystemDiagnosticsPromptType.cs  # System diagnostic prompts
│   └── Infrastructure/                  # Console UI, Win32 API
├── WinDiagMcpChat/                      # AI-powered chat client
│   └── Program.cs                       # Azure OpenAI integration
├── server_config.json                   # MCP configuration
├── setup-claude-desktop.ps1             # Claude Desktop setup
├── launch-inspector.ps1                 # Launch Inspector
└── docs/                                # Documentation
    ├── MCP_DEMO_Roadmap.md             # Development roadmap
    ├── CLAUDE_DESKTOP_SETUP.md         # Claude Desktop guide
    └── MCP_INSPECTOR_GUIDE.md          # Inspector guide
```

## Requirements

- .NET 10 SDK
- Node.js 16+ (for Inspector)
- Python 3.8+ (for mcp-cli)
- Claude Desktop (for LLM integration)
- Azure OpenAI (for WinDiagMcpChat)

## Milestones Completed

✅ **Milestone 1** - Minimal diagnostics tool (STDIO)  
✅ **Milestone 2** - Process inspection  
✅ **Milestone 3** - Event log analysis (Resources & Prompts)
  - Event log snapshot resources
  - Resource pagination
  - MCP prompts for diagnostic workflows
  - AI chat client integration

**Next**: Milestone 4 - Move to HTTP + security

## Documentation

- **[MCP_DEMO_Roadmap.md](docs/MCP_DEMO_Roadmap.md)** - Development roadmap
- **[CLAUDE_DESKTOP_SETUP.md](docs/CLAUDE_DESKTOP_SETUP.md)** - Claude Desktop integration ⭐
- **[MCP_INSPECTOR_GUIDE.md](docs/MCP_INSPECTOR_GUIDE.md)** - Inspector guide
- **[MCP_TESTING_GUIDE.md](docs/MCP_TESTING_GUIDE.md)** - Complete testing guide

## Key Features

### Tools
- System information retrieval
- Process listing and detailed inspection
- Event log snapshot creation with XPath filtering

### Resources  
- Event log snapshots with pagination
- Query parameter support (limit, offset)
- Efficient handling of large result sets

### Prompts
- AI-guided diagnostic workflows
- Parameterized prompts
- Step-by-step instructions for complex diagnostics
- Event log analysis, CPU investigation, health checks

### AI Integration
- Chat client with Azure OpenAI
- Automatic tool and prompt discovery
- Resource pagination handling
- Conversation history management

## License

MIT License

## Resources

- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Inspector](https://github.com/modelcontextprotocol/inspector)
- [Azure OpenAI](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
