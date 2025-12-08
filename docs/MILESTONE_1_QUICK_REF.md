# Milestone 1 - Quick Reference Card

## 🎯 Goal
Minimal diagnostics tool with STDIO transport

## ✅ Status
**COMPLETE**

---

## Quick Commands

### Build & Test
```powershell
dotnet build
.\test-mcp-server.ps1
```

### Visual Testing
```powershell
.\launch-inspector.ps1
```

### Claude Desktop Setup
```powershell
.\setup-claude-desktop.ps1
```

---

## The Tool

**Name:** `get_system_info`

**Description:** Returns basic system information for diagnostics

**Parameters:** None

**Returns:**
- Machine name
- Username  
- OS description
- Architecture
- Processor count
- .NET version
- System uptime
- Working directory

---

## Test It

### With mcp-cli
```powershell
mcp-cli tools --server windiag --config-file server_config.json
mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info
```

### With MCP Inspector
1. Run `.\launch-inspector.ps1`
2. Click "Connect"
3. Click "Tools" → "get_system_info"
4. Click "Call Tool"

### With Claude Desktop
1. Run `.\setup-claude-desktop.ps1`
2. Restart Claude Desktop
3. Ask: "What is my system information?"

---

## Project Structure
```
WinDiagMcpServer/
├── Program.cs                    # Server setup
├── WinDiagMcpServerToolType.cs  # Tool implementation
├── SystemInfoResult.cs          # Data model
└── ConsoleUi.cs                 # UI helpers

Scripts/
├── setup-claude-desktop.ps1     # Claude setup (Windows)
├── setup-claude-desktop.sh      # Claude setup (Unix)
├── launch-inspector.ps1         # Inspector launcher
└── test-mcp-server.ps1          # Test automation

Docs/
├── MILESTONE_1_SUMMARY.md       # Complete summary
├── CLAUDE_DESKTOP_SETUP.md      # Claude guide
└── TESTING.md                   # Testing guide
```

---

## Key Learnings

✅ **STDIO Transport** - Process-based communication  
✅ **Tool Declaration** - Attribute-based registration  
✅ **JSON-RPC Protocol** - Request/response flow  
✅ **Testing Methods** - CLI, Visual, LLM  
✅ **Type Safety** - Compile-time checking  

---

## Next: Milestone 2

**Branch:**
```bash
git checkout -b milestone-2-process-inspection
```

**Features:**
- `listProcesses()` tool
- `getProcessDetails(pid)` tool with parameters
- First **Resource**: process snapshot
- First **Prompt**: `explainProcessList()`

---

## Troubleshooting

| Problem | Solution |
|---------|----------|
| Build fails | `dotnet clean && dotnet build` |
| Tools not in Claude | Restart Claude Desktop completely |
| Inspector won't start | Check Node.js: `node --version` |
| Server won't start | `dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj` |

---

## Resources

📚 [Full Summary](MILESTONE_1_SUMMARY.md)  
🔧 [Testing Guide](TESTING.md)  
🖥️ [Claude Setup](CLAUDE_DESKTOP_SETUP.md)  
🗺️ [Roadmap](MCP_DEMO_Roadmap.md)  

---

**Branch:** `milestone-1-minimal-stdio`  
**Status:** ✅ COMPLETE  
**Date:** 2024
