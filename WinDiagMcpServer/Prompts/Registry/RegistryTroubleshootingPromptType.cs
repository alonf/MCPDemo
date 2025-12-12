using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.Registry;

public class RegistryTroubleshootingPromptType
{
    [McpServerPrompt]
    [Description("Guides the AI assistant to use the Registry troubleshooting tool.")]
    public string TroubleshootRegistry(
        [Description("A description of the registry task.")] string taskDescription)
    {
        return $"""
            You are an expert Windows system administrator.
            The user wants to perform the following registry task: "{taskDescription}".

            **IMPORTANT WORKFLOW**
            1. Use `CreateRegistrySnapshot` to inspect registry keys.
            2. **ROOTS & SECURITY**:
               - Access is restricted to specific allowed roots.
               - If you get an "Access denied" error, explain to the user that the path is restricted and ask them to configure roots if necessary.
               - You can use `ConfigureRegistryRoots` to add new allowed paths if the user explicitly requests it.
            3. When creating a snapshot, start with `recursive=false` if you are unsure of the size, or target a specific subkey.
            4. The snapshot returns a resource URI (registry://snapshot/...). Read it using `read_resource`.
            
            Begin by analyzing the request and deciding which key to inspect.
            """;
    }
}
