namespace WinDiagMcpServer.Tools.Process;

public sealed record ProcessInfoResult
{
    public ProcessInfo Process { get; set; } = new();

    public bool HasError { get; set; }

    public string ErrorMessage { get; set; } = string.Empty;

    public int HttpStatusCode { get; set; }
}
