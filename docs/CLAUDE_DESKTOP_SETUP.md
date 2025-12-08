# Claude Desktop Setup Guide

This guide shows you how to configure Claude Desktop to use the WinDiag MCP Server.

## Quick Setup (Automated)

### Windows
```powershell
.\setup-claude-desktop.ps1
```

### macOS/Linux
```bash
./setup-claude-desktop.sh
```

The script will:
1. ✅ Check if Claude Desktop is installed
2. ✅ Build the WinDiag MCP Server
3. ✅ Configure Claude Desktop to use the server
4. ✅ Create backup of existing configuration

---

## Manual Setup

If you prefer to configure manually or the script doesn't work:

### Step 1: Install Claude Desktop

Download from: https://claude.ai/download

### Step 2: Locate Configuration File

**Windows:**
```
%APPDATA%\Claude\claude_desktop_config.json
```

**macOS:**
```
~/Library/Application Support/Claude/claude_desktop_config.json
```

**Linux:**
```
~/.config/Claude/claude_desktop_config.json
```

### Step 3: Edit Configuration

Open the configuration file and add:

```json
{
  "mcpServers": {
    "windiag": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:/Dev/MCPDemo/WinDiagMcpServer/WinDiagMcpServer.csproj"
      ]
    }
  }
}
```

**Important:** 
- Use **forward slashes** (`/`) in the path, even on Windows
- Use the **absolute path** to your project
- If you have existing `mcpServers`, add `windiag` to the existing object

### Step 4: Restart Claude Desktop

Completely close and reopen Claude Desktop for the changes to take effect.

---

## Testing Your Setup

### Step 1: Verify Tool Discovery

In Claude Desktop, start a new conversation and ask:

```
What tools do you have access to?
```

**Expected Response:**
Claude should mention it has access to a `get_system_info` tool that provides Windows system diagnostics.

### Step 2: Test the Tool

Ask Claude:

```
What is my system information?
```

**Expected Response:**
Claude should call the `get_system_info` tool and display your:
- Machine name
- Username
- OS description
- Architecture
- Processor count
- .NET framework version
- System uptime

### Example Conversation

```
You: What tools do you have access to?

Claude: I have access to a tool called get_system_info that can provide 
Windows system diagnostic information including machine name, OS details, 
architecture, processor count, .NET framework version, and system uptime.

You: What is my system information?

Claude: Let me get that information for you.

[Claude calls get_system_info tool]

Here's your system information:
- Machine Name: HOMEALON11
- User: alon
- OS: Microsoft Windows 10.0.22631
- Architecture: X64
- Processors: 44
- .NET Framework: .NET 10.0.0
- System Uptime: 14 days, 10 hours, 10 minutes
- Working Directory: C:\Dev\MCPDemo
```

---

## Troubleshooting

### Tools Don't Appear

**Problem:** Claude doesn't mention any tools when asked.

**Solutions:**
1. ✅ **Restart Claude Desktop completely**
   - Make sure it's fully closed (check system tray)
   - Reopen the application

2. ✅ **Verify .NET SDK is in PATH**
   ```powershell
   dotnet --version
   ```
   Should show .NET 10 or later

3. ✅ **Check configuration file syntax**
   - Use a JSON validator
   - Ensure proper comma placement
   - Use forward slashes in paths

4. ✅ **Check absolute path**
   - Make sure the path to `.csproj` is correct
   - Use `pwd` (Linux/macOS) or `cd` (Windows) to get current directory

### Tool Execution Fails

**Problem:** Claude finds the tool but gets an error when calling it.

**Solutions:**
1. ✅ **Test server manually**
   ```powershell
   dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj
   ```
   Press Ctrl+C to stop

2. ✅ **Check build errors**
   ```powershell
   dotnet build
   ```

3. ✅ **Verify JSON-RPC communication**
   - Use MCP Inspector to test the server first
   ```powershell
   .\launch-inspector.ps1
   ```

### Configuration File Won't Save

**Problem:** Can't edit or save the configuration file.

**Solutions:**
1. ✅ **Run editor as administrator** (Windows)
2. ✅ **Check file permissions**
3. ✅ **Create the directory if missing**
   ```powershell
   # Windows
   mkdir "$env:APPDATA\Claude"
   ```

### Claude Desktop Not Installed

**Problem:** Configuration directory doesn't exist.

**Solution:**
1. Download Claude Desktop: https://claude.ai/download
2. Install and run it at least once
3. Close it
4. Run the setup script again

---

## Advanced Configuration

### Multiple MCP Servers

You can configure multiple MCP servers:

```json
{
  "mcpServers": {
    "windiag": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/Dev/MCPDemo/WinDiagMcpServer/WinDiagMcpServer.csproj"]
    },
    "another-server": {
      "command": "node",
      "args": ["server.js"]
    }
  }
}
```

### Environment Variables

Pass environment variables to your server:

```json
{
  "mcpServers": {
    "windiag": {
      "command": "dotnet",
      "args": ["run", "--project", "C:/Dev/MCPDemo/WinDiagMcpServer/WinDiagMcpServer.csproj"],
      "env": {
        "DEBUG": "true",
        "LOG_LEVEL": "verbose"
      }
    }
  }
}
```

### Working Directory

Specify a working directory:

```json
{
  "mcpServers": {
    "windiag": {
      "command": "dotnet",
      "args": ["run", "--project", "WinDiagMcpServer/WinDiagMcpServer.csproj"],
      "cwd": "C:/Dev/MCPDemo"
    }
  }
}
```

---

## Comparison with Other Testing Methods

| Method | LLM Integration | Setup | Best For |
|--------|----------------|-------|----------|
| **Claude Desktop** | ✅ Yes | ⭐ Easy | Production, demos, end-users |
| **MCP Inspector** | ❌ No | ⭐⭐ Medium | Development, debugging |
| **mcp-cli** | ❌ No | ⭐ Easy | CI/CD, automated testing |
| **VS Code Extensions** | ✅ Yes | ⭐⭐ Medium | Developer workflow |

**Use Claude Desktop when:**
- ✅ Demonstrating MCP to an audience
- ✅ Testing end-to-end LLM integration
- ✅ Using MCP servers in production
- ✅ Wanting the best user experience

**Use MCP Inspector when:**
- ✅ Developing new tools
- ✅ Debugging protocol issues
- ✅ Learning MCP concepts
- ✅ Testing without LLM

---

## Configuration File Reference

### Full Schema

```json
{
  "$schema": "https://modelcontextprotocol.io/schemas/claude_desktop_config.json",
  "mcpServers": {
    "server-name": {
      "command": "string",
      "args": ["array", "of", "strings"],
      "env": {
        "KEY": "value"
      },
      "cwd": "string"
    }
  },
  "globalShortcut": "string"
}
```

### Field Descriptions

- **`command`**: Executable to run (e.g., `dotnet`, `node`, `python`)
- **`args`**: Command-line arguments as array of strings
- **`env`** (optional): Environment variables
- **`cwd`** (optional): Working directory

---

## Milestone 1 Complete! 🎉

With Claude Desktop configured, you've completed **Milestone 1 – Minimal diagnostics tool (STDIO)**:

✅ MCP server over STDIO  
✅ Tool: `getSystemInfo()`  
✅ JSON-RPC flow demonstrated  
✅ Tool list and execution working  
✅ Real LLM client integration  

**Next:** [Milestone 2 – Process Inspection](MCP_DEMO_Roadmap.md#milestone-2--process-inspection)

---

## Resources

- [Claude Desktop Download](https://claude.ai/download)
- [MCP Specification](https://modelcontextprotocol.io/)
- [MCP Testing Guide](MCP_TESTING_GUIDE.md)
- [MCP Inspector Guide](MCP_INSPECTOR_GUIDE.md)

---

## Uninstalling

To remove the MCP server from Claude Desktop:

### Option 1: Remove Configuration

Edit the configuration file and remove the `windiag` entry.

### Option 2: Run Uninstall Script

```powershell
# Windows
.\uninstall-claude-desktop.ps1
```

```bash
# macOS/Linux
./uninstall-claude-desktop.sh
```

---

## Support

For issues or questions:

1. Check [Troubleshooting](#troubleshooting) section
2. Review [MCP Testing Guide](MCP_TESTING_GUIDE.md)
3. Verify server works with MCP Inspector first
4. Check Claude Desktop's developer console (if available)

---

**Happy testing with Claude Desktop!** 🚀
