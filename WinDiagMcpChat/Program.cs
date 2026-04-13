using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using OpenAI;
using OpenAI.Chat;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

#region Initialization and Server Startup
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

Console.WriteLine("Building MCP Server...");
var buildProcess = Process.Start(new ProcessStartInfo
{
    FileName = dotnetExecutable,
    Arguments = $"build \"{projectPath}\"",
    UseShellExecute = false,
    CreateNoWindow = true
});
if (buildProcess != null)
{
    await buildProcess.WaitForExitAsync();
    if (buildProcess.ExitCode != 0)
    {
        Console.WriteLine("Failed to build MCP Server.");
        return;
    }
}

var serverExePath = Path.Combine(Path.GetDirectoryName(projectPath)!, "bin", "Debug", "net10.0-windows", "WinDiagMcpServer.exe");
if (!File.Exists(serverExePath))
{
    Console.WriteLine($"Server executable not found at: {serverExePath}");
    return;
}

Console.WriteLine("Starting MCP Server...");
var serverProcess = new Process
{
    StartInfo = new ProcessStartInfo
    {
        FileName = serverExePath,
        Arguments = "--urls=http://localhost:5000",
        WorkingDirectory = Path.GetDirectoryName(projectPath) ?? solutionRoot,
        UseShellExecute = true,
        CreateNoWindow = false
    }
};
serverProcess.Start();

AppDomain.CurrentDomain.ProcessExit += (_, _) => {
    if (!serverProcess.HasExited)
    {
        serverProcess.Kill();
    }
};

Console.WriteLine("Waiting for server to start...");
await Task.Delay(5000);
#endregion // Initialization and Server Startup

#region Elicitation Handler
async ValueTask<ElicitResult> HandleElicitationAsync(ElicitRequestParams? requestParams, CancellationToken token)
{
    await Task.CompletedTask;

    if (requestParams?.RequestedSchema?.Properties is null || requestParams.RequestedSchema.Properties.Count == 0)
    {
        return new ElicitResult { Action = "reject" };
    }

    Console.WriteLine();
    Console.WriteLine("══════════ HUMAN CONFIRMATION REQUIRED ══════════");
    if (!string.IsNullOrWhiteSpace(requestParams.Message))
    {
        Console.WriteLine(requestParams.Message);
    }

    Console.WriteLine("Type 'cancel' anytime to abort the operation.");

    var content = new Dictionary<string, JsonElement>();

    foreach (var property in requestParams.RequestedSchema.Properties)
    {
        var value = PromptForValue(property.Key, property.Value, token);
        if (value is null)
        {
            Console.WriteLine("[Elicitation] User cancelled input.");
            return new ElicitResult { Action = "reject" };
        }

        content[property.Key] = value.Value;
    }

    Console.WriteLine("[Elicitation] Confirmation captured.");
    return new ElicitResult { Action = "accept", Content = content };

    #region Elicitation Helpers

    JsonElement? PromptForValue(string propertyName, object schema, CancellationToken cancellationToken)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            switch (schema)
            {
                case StringSchema stringSchema:
                    Console.Write($"{stringSchema.Description ?? propertyName}: ");
                    var stringInput = Console.ReadLine();
                    if (IsCancelled(stringInput))
                    {
                        return null;
                    }

                    if (!string.IsNullOrWhiteSpace(stringInput))
                    {
                        return JsonSerializer.SerializeToElement(stringInput.Trim());
                    }

                    Console.WriteLine("A value is required. Type 'cancel' to abort.");
                    break;

                case BooleanSchema booleanSchema:
                    Console.Write($"{booleanSchema.Description ?? propertyName} (y/n): ");
                    var boolInput = Console.ReadLine();
                    if (IsCancelled(boolInput))
                    {
                        return null;
                    }

                    if (TryParseBoolean(boolInput, out var boolResult))
                    {
                        return JsonSerializer.SerializeToElement(boolResult);
                    }

                    Console.WriteLine("Please respond with 'y' or 'n'.");
                    break;

                case NumberSchema numberSchema:
                    Console.Write($"{numberSchema.Description ?? propertyName}: ");
                    var numberInput = Console.ReadLine();
                    if (IsCancelled(numberInput))
                    {
                        return null;
                    }

                    if (double.TryParse(numberInput, out var numericValue))
                    {
                        if (numberSchema.Type == "integer")
                        {
                            return JsonSerializer.SerializeToElement(Convert.ToInt32(numericValue));
                        }

                        return JsonSerializer.SerializeToElement(numericValue);
                    }

                    Console.WriteLine("Enter a numeric value (type 'cancel' to abort).");
                    break;

                case TitledSingleSelectEnumSchema titledEnumSchema:
                    return PromptForEnum(propertyName, titledEnumSchema.OneOf.ToList(), cancellationToken);

                case UntitledSingleSelectEnumSchema untitledEnumSchema:
                    var options = untitledEnumSchema.Enum
                        .Select(value => new EnumSchemaOption { Const = value, Title = value })
                        .ToList();
                    return PromptForEnum(propertyName, options, cancellationToken);

                default:
                    Console.WriteLine($"Unsupported schema type for '{propertyName}'. Rejecting.");
                    return null;
            }
        }
    }

    JsonElement? PromptForEnum(string propertyName, IList<EnumSchemaOption> options, CancellationToken cancellationToken)
    {
        if (options.Count == 0)
        {
            Console.WriteLine($"No selectable options supplied for '{propertyName}'.");
            return null;
        }

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Console.WriteLine($"{propertyName} - choose one of the following:");
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                Console.WriteLine($"  {i + 1}. {option.Title}");
            }

            Console.Write("Selection (number or value, 'cancel' to abort): ");
            var selection = Console.ReadLine();
            if (IsCancelled(selection))
            {
                return null;
            }

            if (int.TryParse(selection, out var choiceIndex) && choiceIndex > 0 && choiceIndex <= options.Count)
            {
                return JsonSerializer.SerializeToElement(options[choiceIndex - 1].Const);
            }

            var optionMatch = options.FirstOrDefault(opt => string.Equals(opt.Const, selection, StringComparison.OrdinalIgnoreCase));
            if (optionMatch is not null)
            {
                return JsonSerializer.SerializeToElement(optionMatch.Const);
            }

            Console.WriteLine("Invalid selection. Please try again.");
        }
    }

    static bool IsCancelled(string? input) => string.Equals(input?.Trim(), "cancel", StringComparison.OrdinalIgnoreCase);

    static bool TryParseBoolean(string? input, out bool value)
    {
        if (bool.TryParse(input, out value))
        {
            return true;
        }

        if (string.Equals(input, "y", StringComparison.OrdinalIgnoreCase) || string.Equals(input, "yes", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (string.Equals(input, "n", StringComparison.OrdinalIgnoreCase) || string.Equals(input, "no", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    #endregion // Elicitation Helpers
}

#endregion // Elicitation Handler

#region Sampling Handler & Azure Client

var azureClient = new AzureOpenAIClient(endpoint, credential);
var chatClient = azureClient.GetChatClient(deploymentName);

async ValueTask<CreateMessageResult> HandleSamplingAsync(
    CreateMessageRequestParams? requestParams, 
    CancellationToken token)
{
    try
    {
        if (requestParams is null)
        {
            throw new ArgumentNullException(nameof(requestParams));
        }

        // IMPORTANT: Sampling is the server asking the client to run an LLM on its behalf.
        // The client must remain generic: do not add domain knowledge or validate/transform the result.
        // If the server wants retries or stricter formatting, it should call sampling again.

        token.ThrowIfCancellationRequested();

        var samplingAgent = chatClient.AsAIAgent(
            instructions: string.IsNullOrWhiteSpace(requestParams.SystemPrompt)
                ? "You are a sampling runner. Respond as the assistant to the provided transcript. Do not call tools unless explicitly instructed."
                : requestParams.SystemPrompt,
            name: "SamplingRunner",
            tools: Array.Empty<AITool>());

        var samplingSession = await samplingAgent.CreateSessionAsync();
        var transcript = BuildSamplingTranscript(requestParams);
        var response = await samplingAgent.RunAsync(transcript, samplingSession);

        var generatedText = response.Text;

        if (string.IsNullOrWhiteSpace(generatedText))
        {
            Console.WriteLine("[Warning] Sampling response contained no text content. Returning empty string.");
            generatedText = string.Empty;
        }

        return new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = generatedText }
            },
            Model = deploymentName,
            StopReason = "endTurn"
        };
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Error] Sampling handler failed: {ex.GetType().Name}: {ex.Message}");
        return new CreateMessageResult
        {
            Role = Role.Assistant,
            Content = new List<ContentBlock>
            {
                new TextContentBlock { Text = $"[SamplingError] {ex.GetType().Name}: {ex.Message}" }
            },
            Model = deploymentName,
            StopReason = "error"
        };
    }
}

#endregion // Sampling Handler & Azure Client

#region MCP Client Configuration
var clientOptions = new McpClientOptions
{
    Capabilities = new ClientCapabilities
    {
        Elicitation = new ElicitationCapability
        {
            Form = new FormElicitationCapability()
        },
        Sampling = new SamplingCapability()
    },
    Handlers = new McpClientHandlers
    {
        ElicitationHandler = HandleElicitationAsync,
        SamplingHandler = async (requestParams, _, token) => await HandleSamplingAsync(requestParams, token)
    }
};

var mcpClient = await McpClient.CreateAsync(
    new HttpClientTransport(new HttpClientTransportOptions
    {
        Endpoint = new Uri("http://localhost:5000/mcp?apiKey=secure-mcp-key")
    }),
    clientOptions);

// Register the Handler for 'sampling/createMessage' - REMOVED (Using McpClientHandlers instead)

#endregion // MCP Client Configuration


#region Internal Tools
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

// Define the internal tool for retrieving MCP prompts
async Task<string> GetMcpPromptContentAsync(
    [Description("The name of the MCP prompt to retrieve")] string promptName,
    [Description("Optional JSON object of arguments for the prompt")] string? argumentsJson = null)
{
    try
    {
        Console.WriteLine($"[Internal Tool] Getting MCP prompt: {promptName}");

        var prompts = await mcpClient.ListPromptsAsync();
        var prompt = prompts.FirstOrDefault(p => p.Name == promptName);

        if (prompt is null)
        {
            return $"Prompt '{promptName}' not found on MCP server.";
        }

        IDictionary<string, object?> argsDict;

        if (string.IsNullOrWhiteSpace(argumentsJson))
        {
            argsDict = new Dictionary<string, object?>();
        }
        else
        {
            // Simple JSON -> dictionary parsing
            argsDict = JsonSerializer
                .Deserialize<Dictionary<string, object?>>(argumentsJson)
                ?? new Dictionary<string, object?>();
        }

        var promptResult = await prompt.GetAsync(argsDict);

        // Build a formatted string from the prompt messages
        var sb = new StringBuilder();
        sb.AppendLine($"[MCP PROMPT: {promptName}]");
        sb.AppendLine();

        foreach (var msg in promptResult.Messages)
        {
            sb.AppendLine($"[{msg.Role}]");
            
            // Serialize the ContentBlock to get its content
            var contentJson = JsonSerializer.Serialize(msg.Content);
            var contentObj = JsonSerializer.Deserialize<JsonElement>(contentJson);
            
            if (contentObj.TryGetProperty("text", out var textElement))
            {
                sb.AppendLine(textElement.GetString());
            }
            else
            {
                sb.AppendLine(msg.Content.ToString() ?? "[No content]");
            }
            
            sb.AppendLine();
        }

        var resultText = sb.ToString();
        Console.WriteLine($"[Internal Tool] Retrieved prompt '{promptName}' with {promptResult.Messages.Count} message(s)");
        return resultText;
    }
    catch (Exception ex)
    {
        var errorMsg = $"Error retrieving prompt '{promptName}': {ex.Message}";
        Console.WriteLine($"[Internal Tool] {errorMsg}");
        if (ex.InnerException is not null)
        {
            Console.WriteLine($"[Internal Tool] Inner exception: {ex.InnerException.Message}");
        }
        return errorMsg;
    }
}

var getPromptFunction = AIFunctionFactory.Create(
    GetMcpPromptContentAsync,
    "get_prompt",
    "Retrieves and expands a named MCP prompt from the diagnostics MCP server.");
allTools.Add(getPromptFunction);

#endregion // Internal Tools

#region Agent Initialization
// Fetch available prompts and build the prompt list for instructions
var availablePromptsList = "";
if (mcpClient.ServerCapabilities.Prompts is not null)
{
    Console.WriteLine("Fetching prompts...");
    var mcpPrompts = await mcpClient.ListPromptsAsync();
    Console.WriteLine($"Found {mcpPrompts.Count} prompts");
    
    if (mcpPrompts.Any())
    {
        var promptDescriptions = mcpPrompts.Select(p => 
            $"  - {p.Name}: {p.Description ?? "No description"}");
        availablePromptsList = "\n\nAVAILABLE MCP PROMPTS:\n" + 
            string.Join("\n", promptDescriptions);
    }
}

// Create AI Agent
AIAgent agent = chatClient.AsAIAgent(
    instructions:
    #region Agent Instructions
    $@"You are a helpful system diagnostics assistant.
                        You have access to Windows diagnostics tools via MCP.
                        
                        MCP PROMPTS - IMPORTANT WORKFLOW:
                        - The MCP server provides named prompts that contain step-by-step workflows for complex diagnostics.
                        - When the user asks for diagnostics like 'health check', 'diagnose system', 'check for issues', etc., 
                          YOU MUST FIRST call the 'get_prompt' tool to get the appropriate workflow.
                        - Available prompts:{availablePromptsList}
                        
                        HOW TO USE PROMPTS:
                        1. When user asks for diagnostics, identify the matching prompt name from the list above
                        2. Call get_prompt with the prompt name (and any required parameters in argumentsJson)
                        3. The prompt will return a detailed workflow - FOLLOW IT STEP BY STEP
                        4. Execute each step in the workflow using the appropriate tools
                        5. Present results to the user as you complete each major step
                        
                        PROMPT MATCHING EXAMPLES:
                        - User says 'health check' or 'diagnose system' → use 'DiagnoseSystemHealth' prompt
                        - User says 'check cpu' or 'high cpu' → use 'ExplainHighCpu' prompt
                        - User says 'security check' or 'security scan' → use 'DetectSecurityAnomalies' prompt
                        - User asks about 'application errors' or 'recent errors' → use 'AnalyzeRecentApplicationErrors' prompt
                        
                        PROMPT PARAMETERS:
                        - If a prompt requires parameters (like hoursBack), pass them as JSON in argumentsJson
                        - Example: get_prompt('DiagnoseSystemHealth', '{{""hoursBack"":24}}')
                        - Default values are usually fine if user doesn't specify
                        
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
                #endregion // Agent Instructions
        name: "WinDiagAgent",
        tools: allTools);

// Create a new agent session with history management
var session = await agent.CreateSessionAsync();

#endregion // Agent Initialization

#region Chat Loop
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
        // Run agent with session - framework handles history automatically
        var response = await agent.RunAsync(input, session);
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

#endregion // Chat Loop

#region Helpers
static string BuildSamplingTranscript(CreateMessageRequestParams requestParams)
{
    var sb = new StringBuilder();

    if (!string.IsNullOrWhiteSpace(requestParams.SystemPrompt))
    {
        sb.AppendLine("[SYSTEM]");
        sb.AppendLine(requestParams.SystemPrompt);
        sb.AppendLine();
    }

    foreach (var msg in requestParams.Messages)
    {
        sb.AppendLine($"[{msg.Role}]");
        foreach (var block in msg.Content)
        {
            if (block is TextContentBlock text)
            {
                sb.AppendLine(text.Text);
            }
            else
            {
                sb.AppendLine(block.ToString());
            }
        }

        sb.AppendLine();
    }

    return sb.ToString();
}
#endregion // Helpers

