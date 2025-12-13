namespace WinDiagMcpServer.Tools.Process;

/// <summary>
/// Represents the CPU usage information for a specific process.
/// </summary>
public class ProcessCpuUsage
{
    /// <summary>
    /// Gets or sets the unique identifier of the process.
    /// </summary>
    public int ProcessId { get; set; }

    /// <summary>
    /// Gets or sets the name of the process.
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current working set memory usage in bytes.
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Gets or sets the calculated CPU usage percentage.
    /// </summary>
    public double CpuPercent { get; set; }
}
