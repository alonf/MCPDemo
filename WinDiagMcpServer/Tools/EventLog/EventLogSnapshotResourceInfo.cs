namespace WinDiagMcpServer.Tools.EventLog;

public record EventLogSnapshotResourceInfo
{
    /// <summary>
    /// Gets the resource URI of the event log snapshot.
    /// </summary>
    public string ResourceUri { get; init; } = string.Empty;

    /// <summary>
    /// Gets the XPath query used to create the snapshot.
    /// </summary>
    public string XPathQuery { get; init; } = string.Empty;
}
