namespace WinDiagMcpServer;

public sealed record BasicProcessInfo
{
    /// <summary>
    /// Gets the name of the process.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the process identifier (ID).
    /// </summary>
    public int Id { get; init; }
}
