namespace WinDiagMcpServer.Tools.Process;

public sealed record ProcessInfo
{
    public int ProcessId { get; set; }

    public string ProcessName { get; set; } = string.Empty;

    public long WorkingSetMemory { get; set; }

    public string MainWindowTitle { get; set; } = string.Empty;

    public int BasePriority { get; set; }

    public int HandleCount { get; set; }

    public uint GdiObjectCount { get; set; }

    public uint UserObjectCount { get; set; }

    public int ThreadCount { get; set; }

    public DateTime StartTime { get; set; }

    public TimeSpan TotalProcessorTime { get; set; }

    public int ParentProcessId { get; set; }

    public MemoryUsage MemoryUsage { get; set; } = new();
}
