using System.ComponentModel;
using System.Runtime.InteropServices;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Tools.SystemInfo;

/// <summary>
/// Represents the result of system information diagnostics.
/// </summary>
public sealed record SystemInfoResult
{
    /// <summary>
    /// Gets the machine name.
    /// </summary>
    public string MachineName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current user name.
    /// </summary>
    public string UserName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operating system description.
    /// </summary>
    public string OSDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the operating system architecture.
    /// </summary>
    public string OSArchitecture { get; init; } = string.Empty;

    /// <summary>
    /// Gets the process architecture.
    /// </summary>
    public string ProcessArchitecture { get; init; } = string.Empty;

    /// <summary>
    /// Gets the number of processors.
    /// </summary>
    public int ProcessorCount { get; init; }

    /// <summary>
    /// Gets the .NET framework description.
    /// </summary>
    public string FrameworkDescription { get; init; } = string.Empty;

    /// <summary>
    /// Gets the current working directory.
    /// </summary>
    public string CurrentDirectory { get; init; } = string.Empty;

    /// <summary>
    /// Gets the system uptime.
    /// </summary>
    public TimeSpan SystemUpTime { get; init; }
}
