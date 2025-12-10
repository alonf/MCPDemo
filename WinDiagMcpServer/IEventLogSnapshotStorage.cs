namespace WinDiagMcpServer;

/// <summary>
/// Stores event log snapshot data in memory.
/// </summary>
public interface IEventLogSnapshotStorage
{
    /// <summary>
    /// Stores a new event log snapshot.
    /// </summary>
    /// <param name="id">The unique identifier for the snapshot.</param>
    /// <param name="xPathQuery">The XPath query used to generate the snapshot.</param>
    /// <param name="jsonContent">The JSON content of the snapshot.</param>
    void AddSnapshot(string id, string xPathQuery, string jsonContent);

    /// <summary>
    /// Retrieves a snapshot by its ID.
    /// </summary>
    /// <param name="id">The unique identifier for the snapshot.</param>
    /// <param name="entry">The snapshot entry if found.</param>
    /// <returns>True if the snapshot exists, false otherwise.</returns>
    bool TryGetSnapshot(string id, out EventLogSnapshotEntry entry);

    /// <summary>
    /// Gets all stored snapshot identifiers and their queries.
    /// </summary>
    /// <returns>A collection of all snapshot entries.</returns>
    IReadOnlyCollection<KeyValuePair<string, EventLogSnapshotEntry>> GetAllSnapshots();
}
