using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.Wmi;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Provides MCP prompt templates for troubleshooting Windows components using WMI.
/// </summary>
[McpServerPromptType]
public class WmiTroubleshootingPromptType
{
    /// <summary>
    /// Generates a prompt instructing the agent to troubleshoot a specific component via the WMI troubleshooting tool.
    /// </summary>
    /// <param name="component">The component to analyze (for example, "Logical Disks" or "Network Adapters").</param>
    /// <returns>A prompt string to be used by the MCP server.</returns>
    [McpServerPrompt]
    [Description("Deep dive analysis of a specific system component (Disk, Network, etc.) using WMI.")]
    public string TroubleshootComponent(
        [Description("The component to analyze (e.g., 'Logical Disks', 'Network Adapters')")] string component = "the requested component")
    {
        var targetComponent = string.IsNullOrWhiteSpace(component) ? "the requested component" : component;

        return $"""
            You are a Windows Internals Specialist.
            The user wants a deep inspection of: {targetComponent}.

            **WORKFLOW:**
            1. **DO NOT** use the generic `diagnose_system_health` prompt.
            2. **DO NOT** use standard process or event log tools.
            3. You **MUST** use the `TroubleshootWithWmiAsync` tool.
            
            **INSTRUCTIONS:**
            - Call `TroubleshootWithWmiAsync` immediately.
            - Pass a description like: "Get detailed status of {targetComponent} including capacity and free space."
            - Let the tool choose the safest single-table WQL query.
            
            This tool will handle the query generation for you. Rely on it.
            """;
    }
}
