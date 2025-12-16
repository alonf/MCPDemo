using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer;

// ReSharper disable UnusedMember.Global

/// <summary>
/// MCP server tool type for Windows diagnostics operations.
/// </summary>
[McpServerToolType]
public partial class WinDiagMcpServerToolType
{
    /// <summary>
    /// Returns basic system information for diagnostics (machine name, OS, processors, framework).
    /// </summary>
    /// <returns>A <see cref="SystemInfoResult"/> containing system diagnostic information.</returns>
    [McpServerTool]
    [Description("Returns basic system information for diagnostics (machine name, OS, processors, framework).")]
    public partial SystemInfoResult GetSystemInfo()
    {
        return new SystemInfoResult
        {
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            CurrentDirectory = Environment.CurrentDirectory,
            SystemUpTime = GetSystemUptime()
        };
    }

    /// <summary>
    /// Gets the system uptime based on the tick count.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing how long the system has been running.</returns>
    private static TimeSpan GetSystemUptime()
    {
        long milliseconds = Environment.TickCount64;
        return TimeSpan.FromMilliseconds(milliseconds);
    }
}
