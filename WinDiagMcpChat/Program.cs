using System.ComponentModel;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Windows Diagnostics MCP Chat Client v1.0               ║");
Console.WriteLine("║         Interactive Chat with System Diagnostics               ║");
Console.WriteLine("╚════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

var endpoint = new Uri("https://alonlecturedemo-resource.cognitiveservices.azure.com/");
var credential = new DefaultAzureCredential();
var deploymentName = "model-router";

var solutionRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../.."));
var projectPath = Path.Combine(solutionRoot, "WinDiagMcpServer", "WinDiagMcpServer.csproj");
var dotnetExecutable = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
    "dotnet",
    OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet");

Console.WriteLine("Starting MCP Server...");
var mcpClient = await McpClient.CreateAsync(
    new StdioClientTransport(new()
    {
        Command = dotnetExecutable,
        Arguments =
        [
            "run",
            "--project",
            projectPath
        ],
        Name = "WinDiagMcpServer",
        WorkingDirectory = Path.GetDirectoryName(projectPath) ?? solutionRoot
    }));

Console.WriteLine("Fetching tools...");
var mcpTools = await mcpClient.ListToolsAsync();
var allTools = mcpTools.Cast<AITool>().ToList();

// Define the internal tool for reading resources
async Task<string> ReadMcpResource(
    [Description("The URI of the resource to read (e.g. eventlog://snapshot/abc123?limit=10&offset=0)")] string resourceUri)
{
    try
    {
        Console.WriteLine($"[Internal Tool] Reading resource: {resourceUri}");
        var uri = new Uri(resourceUri);
        var result = await mcpClient.ReadResourceAsync(uri);
        
        // Combine all contents
        var contentList = new List<string>();
        foreach (var content in result.Contents)
        {
            if (content is TextResourceContents textContent)
            {
                contentList.Add(textContent.Text);
                Console.WriteLine($"[Internal Tool] Successfully read {textContent.Text.Length} bytes");
            }
            else
            {
                contentList.Add($"[Binary Content: {content.MimeType}]");
            }
        }
        return string.Join("\n", contentList);
    }
    catch (Exception ex)
    {
        var errorMsg = $"Error reading resource '{resourceUri}': {ex.Message}";
        Console.WriteLine($"[Internal Tool] {errorMsg}");
        if (ex.InnerException != null)
        {
            Console.WriteLine($"[Internal Tool] Inner exception: {ex.InnerException.Message}");
        }
        return errorMsg;
    }
}

var readResourceFunction = AIFunctionFactory.Create(
    ReadMcpResource, 
    "read_resource", 
    "Reads the content of an MCP resource. Resources support pagination via query parameters (e.g., ?limit=10&offset=0).");
allTools.Add(readResourceFunction);

// Fetch available prompts
if (mcpClient.ServerCapabilities.Prompts is not null)
{
    Console.WriteLine("Fetching prompts...");
    var mcpPrompts = await mcpClient.ListPromptsAsync();
    Console.WriteLine($"Found {mcpPrompts.Count()} prompts");
}

// Create AI Agent
AIAgent agent = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        instructions: @"You are a helpful system diagnostics assistant.
                        You have access to Windows diagnostics tools via MCP.
                        
                        EVENT LOG QUERIES:
                        When creating event log snapshots, use these XPath query patterns:
                        - All recent errors: ""*[System/Level=2]""
                        - All recent warnings: ""*[System/Level=3]""
                        - Last N hours: ""*[System/TimeCreated[@SystemTime >= '2025-01-01T00:00:00.000Z']]""
                        - Specific event ID: ""*[System/EventID=1000]""
                        - All events: ""*""
                        
                        RESOURCE READING STRATEGY:
                        1. When you receive a resourceUri from a tool (e.g., eventlog://snapshot/abc123), you MUST use the 'read_resource' tool to read it.
                        2. Event log resources support pagination. ALWAYS start with a small page:
                           - First read: resourceUri?limit=10&offset=0
                           - Check the 'Pagination' section in the response for TotalCount
                           - If TotalCount > 50, read incrementally and summarize patterns
                           - DO NOT try to read all events at once if there are many
                        3. The response includes a 'Pagination' object with:
                           - TotalCount: total events in snapshot
                           - ReturnedCount: events in current page
                           - HasMore: whether more pages exist
                           - NextOffset: offset for next page (if HasMore=true)
                        4. For large snapshots (>100 events):
                           - Analyze first page for patterns
                           - Only read more pages if specific information is needed
                           - Summarize findings rather than listing every event
                        5. Parse the Events array from the response JSON and extract relevant information like:
                           - TimeCreated
                           - ProviderName
                           - LevelDisplayName (Error, Warning, Information)
                           - Message
                        6. If you get an error reading a resource, report the exact error message to the user.
                        
                        RESPONSE FORMAT:
                        When presenting event log data:
                        - Show the timestamp, level, and message for each event
                        - Group similar events if there are patterns
                        - Summarize total counts by severity level
                        
                        Be concise and focus on answering the user's specific question.
                        Maintain context from previous messages in the conversation.",
        name: "WinDiagAgent",
        tools: allTools);

// Create a new agent thread with history management
var thread = agent.GetNewThread();

Console.WriteLine("Agent ready. Type 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("User: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input))
    {
        continue;
    }

    if (input.Trim().ToLower() == "exit")
    {
        break;
    }

    try
    {
        // Run agent with thread - framework handles history automatically
        var response = await agent.RunAsync(input, thread);
        Console.WriteLine($"Agent: {response.Text}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
        
        // If token limit exceeded, provide guidance
        if (ex.Message.Contains("context_length_exceeded") || 
            ex.Message.Contains("tokens") ||
            ex.Message.Contains("maximum context length"))
        {
            Console.WriteLine("[System] Token limit reached. The agent should use pagination to read resources in smaller chunks.");
            Console.WriteLine("[System] Try rephrasing your question to be more specific.");
        }
    }
    Console.WriteLine();
}
