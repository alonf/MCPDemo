using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.Registry;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Provides prompt definitions for registry troubleshooting scenarios.
/// </summary>
[McpServerPromptType]
public class RegistryTroubleshootingPromptType
{
    /// <summary>
    /// Builds the registry troubleshooting instructions for an AI assistant.
    /// </summary>
    /// <param name="taskDescription">The user-provided description of the registry task.</param>
    /// <returns>The formatted prompt that guides registry troubleshooting.</returns>
    [McpServerPrompt]
    [Description("Guides the AI to use Registry tools. Use this for: Windows component information, Product Versions, Software Installations, Auto-Run/Startup programs, and System/Software configuration settings.")]
    public string TroubleshootRegistry(
        [Description("A description of the registry task (e.g., 'check startup apps', 'find windows version', 'list installed software').")] string taskDescription)
    {
        return $"""
            You are an expert Windows System Administrator specializing in the Registry.
            The user wants to perform the following task: "{taskDescription}".

            **WHEN TO USE REGISTRY:**
            The Registry is the source of truth for static configuration. Use it for:
            - **Windows Build & Version Info** (CurrentVersion)
            - **Installed Software & Versions** (Uninstall keys)
            - **Startup Programs** (Run/RunOnce keys)
            - **System Services Configuration** (Start modes, paths)
            - **Hardware History** (Mounted USB devices)

            **KEY MAP (Where to look):**
            - **Startup Apps:** `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
            - **Installed Software:** `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall` (and `WOW6432Node`)
            - **Windows Version:** `HKLM\SOFTWARE\Microsoft\Windows NT\CurrentVersion`
            - **USB History:** `HKLM\SYSTEM\CurrentControlSet\Enum\USBSTOR`
            - **Services:** `HKLM\SYSTEM\CurrentControlSet\Services`

            **WORKFLOW:**
            1. **Select the Tool:** Use `create_registry_snapshot` to capture the key.
            2. **Roots & Security:**
                    - Access is restricted by `RegistryRootsService` (allowed registry roots).
                    - Do NOT wait for an "Access Denied" failure first. If the key you need is outside the default allowed roots, proactively ask the user to authorize it.
                    - Default allowed roots commonly include:
                      - `HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall`
                      - `HKCU\Software\Microsoft\Windows\CurrentVersion\Run`
                    - For other keys (e.g., Windows Version, USB history, Services), call `request_registry_access` for the specific path you intend to read, confirm approval, then proceed with `create_registry_snapshot`.
                    - If the user declines, stop and explain what cannot be accessed and why.
            3. **Snapshot & Read:**
               - The snapshot tool returns a `registry://` URI.
               - You MUST use the `read_resource` tool to inspect the content of that URI.

            **INSTRUCTIONS:**
            - Identify the most likely Registry Key from the map above.
            - If the key is likely outside the default allowed roots, call `request_registry_access` for that path before attempting to snapshot.
            - Invoke `create_registry_snapshot` on that path.
            - Read the resource and summarize the findings.

            Begin the analysis now.
            """;
    }
}
