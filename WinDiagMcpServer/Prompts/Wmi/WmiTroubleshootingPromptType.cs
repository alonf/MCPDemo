using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.Wmi;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Provides prompt templates for guiding the AI assistant through WMI-based troubleshooting workflows.
/// </summary>
public class WmiTroubleshootingPromptType
{
    /// <summary>
    /// Generates instructions for diagnosing a reported system issue using the preferred WMI troubleshooting workflow.
    /// </summary>
    /// <param name="problemDescription">A description of the system problem to diagnose.</param>
    /// <returns>The formatted troubleshooting guidance to provide to the AI assistant.</returns>
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
