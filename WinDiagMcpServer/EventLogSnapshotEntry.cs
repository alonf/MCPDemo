namespace WinDiagMcpServer;

/// <summary>
/// Represents a stored event log snapshot entry.
/// </summary>
/// <param name="XPathQuery">The XPath query used to generate the snapshot.</param>
/// <param name="JsonContent">The JSON content of the snapshot.</param>
public record EventLogSnapshotEntry(string XPathQuery, string JsonContent);
