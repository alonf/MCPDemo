namespace WinDiagMcpServer;

public sealed record MemoryUsage
{
    public long PrivateBytes { get; set; }

    public long VirtualMemorySize { get; set; }

    public long WorkingSet { get; set; }

    public long PagedMemorySize { get; set; }

    public long NonPagedSystemMemorySize { get; set; }

    public long PeakVirtualMemorySize { get; set; }

    public long PeakWorkingSet { get; set; }

    public long PeakPagedMemorySize { get; set; }

    public long PagedSystemMemory { get; set; }

    public long NonPagedSystemMemory { get; set; }
}
