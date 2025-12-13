using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Defines prompts related to system optimization and resource management.
/// </summary>
[McpServerPromptType]
public class SystemOptimizationPromptType(ILogger<SystemOptimizationPromptType> logger)
{
    /// <summary>
    /// Generates a prompt that guides the user through identifying high-CPU processes and terminating them using the elicitation workflow.
    /// </summary>
    /// <returns>The prompt content.</returns>
    [McpServerPrompt]
    [Description("Guides the user through identifying and terminating high-CPU processes to reduce system load.")]
    public string ReduceSystemLoad()
    {
        logger.LogInformation("Generating ReduceSystemLoad prompt");

        return """
            You are a System Optimization Assistant. The user wants to reduce system load.

            **WORKFLOW:**

            1. **Identify the Heavy Lifters:**
               - Immediately call `get_top_cpu_processes` (default count is 5).
               - This tool samples CPU usage over a short window to give accurate results.

            2. **Present the Evidence:**
               - Display a concise table or list of these top processes.
               - Include Name, PID, and CPU %.
               - Ask the user: *"Would you like to terminate any of these to free up resources?"*

            3. **The "Magic" Kill (Crucial Step):**
               - If the user agrees to terminate a process (e.g., "Yes", "Kill the top one", "Let me choose"):
               - **YOU MUST TRIGGER THE ELICITATION FLOW.**
               - **Call the `kill_process` tool WITHOUT providing the `processId` argument.**
               - **DO NOT** ask the user for a PID in the chat.
               - **DO NOT** guess a PID.
               - The server will handle the selection interactively.
               
            **REASONING:**
            - Calling `kill_process` without a PID triggers the **Interactive Selection Menu** on the server.
            - This is safer and allows the user to pick the exact process from a validated list.

            Begin by listing the high-CPU processes now.
            """;
    }
}
