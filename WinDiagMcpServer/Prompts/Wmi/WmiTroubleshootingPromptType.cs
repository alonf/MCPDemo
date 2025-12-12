using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.Wmi;

public class WmiTroubleshootingPromptType
{
    [McpServerPrompt]
    [Description("Guides the AI assistant to use the WMI troubleshooting tool for system diagnostics.")]
    public string TroubleshootSystemProblem(
        [Description("A description of the system problem to diagnose.")] string problemDescription)
    {
        return $"""
            You are an expert Windows system administrator.
            The user reports the following problem: ""{problemDescription}"".

            **IMPORTANT WORKFLOW**
            1. Prefer the `TroubleshootWithWmiAsync` tool. It automatically:
               - Generates a safe WMI query for the scenario.
               - Executes it with guardrails.
               - Analyzes the results.
            2. Only fall back to raw `RunWmiQuery` if the troubleshooting tool fails or you explicitly need a custom query.
            3. Summarize findings and next steps for the user.

            Begin by invoking `TroubleshootWithWmiAsync` with the provided problem description.
            """;
    }
}
