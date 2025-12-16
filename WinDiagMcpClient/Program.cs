//Add a console message about this MCP client
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using OpenAI;
using ModelContextProtocol.Client;

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
// ReSharper disable SuggestVarOrType_Elsewhere
IList<McpClientTool> tools = await mcpClient.ListToolsAsync();
// ReSharper disable SuggestVarOrType_SimpleTypes
foreach (McpClientTool tool in tools)
{
    Console.WriteLine($"{tool}");
}
Console.WriteLine();

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
