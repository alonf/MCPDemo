# WinDiagMcpChat - AI-Powered Chat Client

An interactive console chat client that demonstrates MCP client implementation using the Microsoft Agents AI framework with Azure OpenAI.

## Overview

**WinDiagMcpChat** is a .NET 10 console application that:
- ✅ Connects to the WinDiagMcpServer via STDIO transport
- ✅ Automatically discovers MCP tools, resources, and prompts
- ✅ Uses Azure OpenAI for conversational AI
- ✅ Implements client-side tools for reading resources and prompts
- ✅ Manages conversation history
- ✅ Handles resource pagination automatically

## Quick Start

### Prerequisites

1. **Azure OpenAI** account with deployed model
2. **Azure CLI** for authentication
3. **.NET 10 SDK**

### Setup

1. **Configure Azure OpenAI** in `Program.cs`:
   ```csharp
   var endpoint = new Uri("https://your-resource.cognitiveservices.azure.com/");
   var credential = new DefaultAzureCredential();
   var deploymentName = "your-deployment-name";
   ```

2. **Authenticate with Azure**:
   ```powershell
   az login
   ```

3. **Run the client**:
   ```powershell
   cd WinDiagMcpChat
   dotnet run
   ```

## Features

### 1. Automatic Tool Discovery

At startup, the client:
- Starts the WinDiagMcpServer process
- Lists all available tools from the server
- Converts MCP tools to AI function format
- Registers them with the AI agent

**Console Output:**
```
Starting MCP Server...
Fetching tools...
Fetching prompts...
Found 4 prompts
Agent ready. Type 'exit' to quit.
```

### 2. MCP Prompt Support

The client discovers and lists available MCP prompts:
- `AnalyzeRecentApplicationErrors` - Event log error analysis
- `ExplainHighCpu` - CPU usage investigation
- `DetectSecurityAnomalies` - Security anomaly detection
- `DiagnoseSystemHealth` - Comprehensive health check ⭐

**Internal Tool:**
```csharp
get_prompt(promptName, argumentsJson)
```

Retrieves MCP prompts and returns their workflow content to guide the AI agent.

### 3. Resource Reading

Client implements a resource reading tool:

**Internal Tool:**
```csharp
read_resource(resourceUri)
```

Handles:
- URI parsing
- MCP resource protocol
- Pagination query parameters
- Text and binary content

**Example Usage:**
```
resourceUri: eventlog://snapshot/abc123?limit=10&offset=0
```

### 4. Conversation History

Uses Microsoft Agents AI framework for:
- Thread management
- Conversation history
- Context preservation
- Multi-turn interactions

### 5. AI-Guided Workflows

The agent instructions guide it to:
1. Recognize diagnostic requests
2. Retrieve appropriate MCP prompts
3. Follow prompt workflows step-by-step
4. Use tools as directed
5. Handle pagination properly
6. Present results incrementally

## Usage Examples

### System Health Check

```
User: Do a system health check

Agent:
1. Retrieves DiagnoseSystemHealth prompt
2. Calls get_all_processes
3. Creates event log snapshots
4. Reads resources with pagination
5. Analyzes patterns
6. Presents comprehensive report
```

### Event Log Analysis

```
User: Check recent application errors

Agent:
1. Retrieves AnalyzeRecentApplicationErrors prompt
2. Creates event log snapshot with XPath filter
3. Reads snapshot resource (paginated)
4. Identifies error patterns
5. Provides recommendations
```

### CPU Investigation

```
User: Why is my CPU high?

Agent:
1. Retrieves ExplainHighCpu prompt
2. Gets all processes
3. Identifies top CPU consumers
4. Checks related event logs
5. Correlates data
6. Explains findings
```

## Architecture

### Component Diagram

```
┌─────────────────────────────────────┐
│      WinDiagMcpChat                 │
│  (Console Application)              │
│                                     │
│  ┌─────────────────────────────┐   │
│  │  Azure OpenAI Agent         │   │
│  │  - Conversation handling    │   │
│  │  - Tool orchestration       │   │
│  │  - Response generation      │   │
│  └─────────────────────────────┘   │
│              │                      │
│              ▼                      │
│  ┌─────────────────────────────┐   │
│  │  MCP Client Tools           │   │
│  │  - read_resource()          │   │
│  │  - get_prompt()             │   │
│  └─────────────────────────────┘   │
│              │                      │
└──────────────┼──────────────────────┘
               │ STDIO
               ▼
┌──────────────────────────────────────┐
│     WinDiagMcpServer                 │
│  - Tools: system_info, processes,    │
│           event_log_snapshot         │
│  - Resources: eventlog://snapshot/*  │
│  - Prompts: Diagnostic workflows     │
└──────────────────────────────────────┘
```

### Data Flow

```
User Input
    │
    ▼
AI Agent (decides action based on instructions)
    │
    ├─→ Call MCP Tool (via McpClient)
    │       │
    │       ▼
    │   WinDiagMcpServer executes tool
    │       │
    │       ▼
    │   Returns result (possibly resourceUri)
    │
    ├─→ Call read_resource (client-side tool)
    │       │
    │       ▼
    │   McpClient.ReadResourceAsync(uri)
    │       │
    │       ▼
    │   Returns resource content
    │
    ├─→ Call get_prompt (client-side tool)
    │       │
    │       ▼
    │   McpClient.ListPromptsAsync()
    │       │
    │       ▼
    │   Returns prompt workflow
    │
    ▼
AI Agent synthesizes response
    │
    ▼
User receives answer
```

## Agent Instructions

The AI agent is configured with comprehensive instructions that include:

### MCP Prompts Workflow
- When to call `get_prompt`
- How to match user requests to prompts
- How to follow prompt workflows step-by-step
- Parameter passing for prompts

### Resource Reading Strategy
- When to use `read_resource`
- Pagination best practices
- How to parse pagination metadata
- Handling large result sets

### Response Format
- How to present event log data
- Grouping and summarization techniques
- Error handling and reporting

## Key Code Sections

### 1. MCP Server Connection

```csharp
var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = dotnetExecutable,
        Arguments = ["run", "--project", projectPath],
        Name = "WinDiagMcpServer",
        WorkingDirectory = Path.GetDirectoryName(projectPath) ?? solutionRoot
    }));
```

### 2. Resource Reading Tool

```csharp
async Task<string> ReadMcpResource(string resourceUri)
{
    var uri = new Uri(resourceUri);
    var result = await mcpClient.ReadResourceAsync(uri);
    
    var contentList = new List<string>();
    foreach (var content in result.Contents)
    {
        if (content is TextResourceContents textContent)
        {
            contentList.Add(textContent.Text);
        }
    }
    return string.Join("\n", contentList);
}
```

### 3. Prompt Retrieval Tool

```csharp
async Task<string> GetMcpPromptContentAsync(
    string promptName,
    string? argumentsJson = null)
{
    var prompts = await mcpClient.ListPromptsAsync();
    var prompt = prompts.FirstOrDefault(p => p.Name == promptName);
    
    var argsDict = string.IsNullOrWhiteSpace(argumentsJson)
        ? new Dictionary<string, object?>()
        : JsonSerializer.Deserialize<Dictionary<string, object?>>(argumentsJson);
    
    var promptResult = await prompt.GetAsync(argsDict);
    
    // Format prompt content for AI agent
    var sb = new StringBuilder();
    sb.AppendLine($"[MCP PROMPT: {promptName}]");
    foreach (var msg in promptResult.Messages)
    {
        sb.AppendLine($"[{msg.Role}]");
        // Extract text content...
    }
    return sb.ToString();
}
```

### 4. AI Agent Creation

```csharp
AIAgent agent = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        instructions: "You are a helpful system diagnostics assistant...",
        name: "WinDiagAgent",
        tools: allTools);

var thread = agent.GetNewThread();
```

### 5. Conversation Loop

```csharp
while (true)
{
    Console.Write("User: ");
    var input = Console.ReadLine();
    
    if (input.Trim().ToLower() == "exit") break;
    
    var response = await agent.RunAsync(input, thread);
    Console.WriteLine($"Agent: {response.Text}");
}
```

## Token Management

The client handles token limit errors gracefully:

```csharp
catch (Exception ex)
{
    if (ex.Message.Contains("context_length_exceeded") || 
        ex.Message.Contains("tokens"))
    {
        Console.WriteLine("[System] Token limit reached.");
        Console.WriteLine("[System] The agent should use pagination...");
    }
}
```

## Best Practices Demonstrated

1. **Tool Separation**
   - Server tools for system operations
   - Client tools for MCP protocol operations

2. **Pagination Handling**
   - Start with small pages (limit=10)
   - Check TotalCount before reading more
   - Summarize patterns, don't list everything

3. **Prompt-Driven Workflows**
   - Use prompts for complex diagnostics
   - Follow workflows step-by-step
   - Present results incrementally

4. **Error Handling**
   - Catch and report tool errors
   - Handle token limits
   - Provide actionable guidance

5. **User Experience**
   - Clear console output
   - Progress indicators
   - Contextual error messages

## Comparison with Other Clients

| Feature | WinDiagMcpChat | Claude Desktop | MCP Inspector |
|---------|----------------|----------------|---------------|
| **LLM Integration** | ✅ Azure OpenAI | ✅ Claude | ❌ No |
| **Custom Tools** | ✅ Yes | ❌ No | ❌ No |
| **Prompt Support** | ✅ Full | ✅ Full | ✅ View only |
| **Resource Reading** | ✅ Custom impl | ✅ Built-in | ✅ Built-in |
| **Programmable** | ✅ Yes | ❌ No | ❌ No |
| **Best For** | Custom clients | Production use | Development |

## Extending the Client

### Adding Custom Client Tools

```csharp
async Task<string> MyCustomTool(string param)
{
    // Your custom logic
    return result;
}

var myTool = AIFunctionFactory.Create(
    MyCustomTool,
    "my_tool",
    "Description of my custom tool");

allTools.Add(myTool);
```

### Modifying Agent Instructions

Edit the `instructions` parameter to:
- Change agent personality
- Add domain-specific knowledge
- Modify workflow preferences
- Adjust response formatting

### Using Different LLM Providers

The Microsoft Agents AI framework supports:
- Azure OpenAI
- OpenAI
- Local models (via Microsoft.Extensions.AI)
- Custom providers

## Troubleshooting

### Server Won't Start

**Issue:** MCP server fails to start

**Solutions:**
- Check .NET SDK is installed: `dotnet --version`
- Verify project path is correct
- Build the server first: `dotnet build`

### Azure Authentication Failed

**Issue:** Can't authenticate with Azure OpenAI

**Solutions:**
- Run `az login`
- Check Azure subscription is active
- Verify endpoint and deployment name
- Ensure proper RBAC roles

### Tools Not Discovered

**Issue:** Agent doesn't have access to tools

**Solutions:**
- Check server starts successfully
- Verify STDIO communication
- Look for exceptions in console output

### Token Limits

**Issue:** Context length exceeded errors

**Solutions:**
- Use pagination (smaller limits)
- Summarize large results
- Clear conversation history
- Be more specific in queries

## Future Enhancements

Potential improvements:
- [ ] Streaming responses
- [ ] Multi-server support
- [ ] Persistent conversation storage
- [ ] Web UI frontend
- [ ] Custom prompt editor
- [ ] Tool usage analytics
- [ ] Performance metrics

## Resources

- [Microsoft Agents AI Framework](https://github.com/microsoft/agents)
- [Azure OpenAI Documentation](https://learn.microsoft.com/en-us/azure/ai-services/openai/)
- [MCP Specification](https://modelcontextprotocol.io/)
- [Model Context Protocol Client](https://github.com/modelcontextprotocol/dotnet-sdk)

## License

MIT License - See [LICENSE](../LICENSE) for details
