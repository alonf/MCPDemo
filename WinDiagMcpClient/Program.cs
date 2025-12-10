//Add a console message about this MCP client
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

Console.WriteLine("╔════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║         Windows Diagnostics MCP Client v1.0                    ║");
Console.WriteLine("║         Model Context Protocol Client for System Diagnostics   ║");
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

// List all available tools from the MCP server.
Console.WriteLine("Available tools:");
var tools = await mcpClient.ListToolsAsync();
foreach (var tool in tools)
{
    Console.WriteLine($"{tool}");
}
Console.WriteLine();

// Create a snapshot first
Console.WriteLine("Creating event log snapshot...");
try 
{
    var toolResult = await mcpClient.CallToolAsync("create_event_log_snapshot", new Dictionary<string, object?>
    {
        ["logName"] = "System",
        ["xPathQuery"] = "*[System/EventID=6005]" // Event Log service started (indicates startup)
    });

    string? resourceUriString = null;
    foreach (var content in toolResult.Content)
    {
        if (content.Type == "text")
        {
            var text = ((dynamic)content).Text;
            Console.WriteLine($"Tool output: {text}");
            
            try
            {
                using var doc = JsonDocument.Parse(text);
                resourceUriString = doc.RootElement.TryGetProperty("resourceUri", out JsonElement uriProp) ? uriProp.GetString() : (string?)text;
            }
            catch
            {
                resourceUriString = text;
            }

            Console.WriteLine($"Snapshot created. URI: {resourceUriString}");
            break;
        }
    }

    if (!string.IsNullOrEmpty(resourceUriString))
    {
        // Read the specific resource
        try 
        {
            var resourceUri = new Uri(resourceUriString);
            Console.WriteLine($"Reading resource: {resourceUri}");
            var resources = await mcpClient.ReadResourceAsync(resourceUri);
            foreach (var resource in resources.Contents)
            {
                Console.WriteLine($"Resource Content ({resource.MimeType}):");
                if (resource is TextResourceContents textResource)
                {
                    var text = textResource.Text;
                    Console.WriteLine(text.Length > 1000 ? text.Substring(0, 1000) + "..." : text);
                }
                else
                {
                    Console.WriteLine("Binary content");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to read resource: {ex.Message}");
        }
    }
    else
    {
        Console.WriteLine("Failed to get resource URI from tool result.");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Failed to create snapshot: {ex.Message}");
}

Console.WriteLine();
Console.WriteLine("================================================");

// Create AI Agent with MCP tools (after status)
AIAgent agent = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        instructions: @"You are a helpful computer analysis and problem solving assistant.
                        You have access to Windows diagnostics tools through the MCP servers.
                        Be concise and helpful in your responses.",
        name: "ComputerAnalyzer",
        tools: [.. tools]);

var prompt = "What is the system information?";

var agentResponse = await agent.RunAsync(prompt);

Console.WriteLine(agentResponse.Text);
Console.WriteLine();
Console.WriteLine("================================================");

prompt = "Do not ask questions, just fulfill the following request: List the processes running on the system. Return the process list by process name groups, for example: Notepad.exe: 1515, 2048, 5001.";
agentResponse = await agent.RunAsync(prompt);
Console.WriteLine(agentResponse.Text);
Console.WriteLine();
Console.WriteLine("================================================");

prompt = "Do not ask questions, just fulfill the following request: Get detailed information of the dotnet process that execute the WinDiagMcpServer. Provide all the information that you can get!";
agentResponse = await agent.RunAsync(prompt);
Console.WriteLine(agentResponse.Text);


Console.WriteLine();
