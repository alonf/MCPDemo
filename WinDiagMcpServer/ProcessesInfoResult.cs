using System.Collections.Generic;

namespace WinDiagMcpServer;

public sealed record ProcessesInfoResult
{
    public List<ProcessInfo> Processes { get; } = new();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public bool HasMore { get; set; }

    public bool HasPageSizeTruncated { get; set; }

    public int TotalCount { get; set; }
}
