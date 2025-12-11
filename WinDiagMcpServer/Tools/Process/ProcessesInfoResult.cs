namespace WinDiagMcpServer.Tools.Process;

public sealed record ProcessesInfoResult
{
    public List<ProcessInfo> Processes { get; } = new();

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public bool HasMore { get; set; }

    // ReSharper disable UnusedMember.Global
    public bool HasPageSizeTruncated { get; set; }

    public bool HasError { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public int HttpStatusCode { get; set; }

    public int TotalCount { get; set; }
}
