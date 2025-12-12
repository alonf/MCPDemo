namespace WinDiagMcpServer.Resources.Registry;

// ReSharper disable once InconsistentNaming

/// <summary>
/// Defines operations for persisting and retrieving serialized registry snapshots.
/// </summary>
public interface IRegistrySnapshotStorage
{
    /// <summary>
    /// Adds or replaces a serialized registry snapshot for the specified registry location.
    /// </summary>
    /// <param name="id">The unique identifier for the snapshot.</param>
    /// <param name="hive">The registry hive name from which the snapshot was captured.</param>
    /// <param name="key">The registry key path associated with the snapshot.</param>
    /// <param name="jsonContent">The serialized registry data.</param>
    void AddSnapshot(string id, string hive, string key, string jsonContent);

    /// <summary>
    /// Attempts to retrieve a snapshot entry by its identifier.
    /// </summary>
    /// <param name="id">The unique identifier for the snapshot to look up.</param>
    /// <param name="entry">When this method returns, contains the snapshot entry if found; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if a snapshot with the specified identifier exists; otherwise, <see langword="false"/>.</returns>
    bool TryGetSnapshot(string id, out RegistrySnapshotEntry entry);
}
