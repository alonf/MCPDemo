namespace WinDiagMcpServer.Tools.Process;

/// <summary>
/// Represents snapshot values for the memory usage of a process.
/// </summary>
public sealed record MemoryUsage
{
    /// <summary>
    /// Gets or sets the number of private bytes allocated by the process.
    /// </summary>
    public long PrivateBytes { get; set; }

    /// <summary>
    /// Gets or sets the total virtual memory size reserved by the process.
    /// </summary>
    public long VirtualMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the current working set size, in bytes.
    /// </summary>
    public long WorkingSet { get; set; }

    /// <summary>
    /// Gets or sets the amount of memory paged to disk for the process.
    /// </summary>
    public long PagedMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the amount of nonpaged system memory allocated to the process.
    /// </summary>
    public long NonPagedSystemMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the peak virtual memory size reached by the process.
    /// </summary>
    public long PeakVirtualMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the peak working set size reached by the process.
    /// </summary>
    public long PeakWorkingSet { get; set; }

    /// <summary>
    /// Gets or sets the peak paged memory size reached by the process.
    /// </summary>
    public long PeakPagedMemorySize { get; set; }

    /// <summary>
    /// Gets or sets the paged system memory currently used by the process.
    /// </summary>
    public long PagedSystemMemory { get; set; }

    /// <summary>
    /// Gets or sets the nonpaged system memory currently used by the process.
    /// </summary>
    public long NonPagedSystemMemory { get; set; }
}
