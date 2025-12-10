using System.ComponentModel;
using System.Text.Json;
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
string deploymentName = "model-router";

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
IList<McpClientTool> mcpTools = await mcpClient.ListToolsAsync();
var allTools = mcpTools.Cast<AITool>().ToList();

// Define the internal tool for reading resources
async Task<string> ReadMcpResource(
    [Description("The URI of the resource to read (e.g. eventlog://snapshot/...)")] string resourceUri)
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
        return $"Error reading resource: {ex.Message}";
    }
}

// Create the internal tool
// Note: Assuming AITool can be created from AIFunction or similar. 
// Since I don't have the exact API, I'll try to use AIFunctionFactory and wrap it if possible.
// If AITool is a base class, I might need a wrapper.
// Let's try to use AIFunctionFactory.Create and see if we can add it.
// The Microsoft.Agents.AI library usually works with AIFunctions.
// However, the existing code casts McpClientTool to AITool.
// Let's try to create a generic AITool wrapper if needed, or use a provided one.
// For now, I will assume there is a way to convert.
// Actually, looking at the library patterns, AITool might just be a wrapper around AIFunction.
// Let's try: new AITool(AIFunctionFactory.Create(...))

var readResourceFunction = AIFunctionFactory.Create(ReadMcpResource, "read_resource", "Reads the content of an MCP resource provided by other tools.");
allTools.Add(readResourceFunction);

// Create AI Agent
AIAgent agent = new AzureOpenAIClient(endpoint, credential)
    .GetChatClient(deploymentName)
    .CreateAIAgent(
        instructions: @"You are a helpful system diagnostics assistant.
                        You have access to Windows diagnostics tools via MCP.
                        If a tool returns a 'resourceUri', you MUST use the 'read_resource' tool to read its content before answering the user.
                        Do not ask the user to read it. Read it yourself.
                        Be concise.",
        name: "WinDiagAgent",
        tools: allTools);

Console.WriteLine("Agent ready. Type 'exit' to quit.");
Console.WriteLine();

while (true)
{
    Console.Write("User: ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Trim().ToLower() == "exit") break;

    try
    {
        var response = await agent.RunAsync(input);
        Console.WriteLine($"Agent: {response.Text}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error: {ex.Message}");
    }
    Console.WriteLine();
}

public class MyFunctionTool : AITool
{
}
