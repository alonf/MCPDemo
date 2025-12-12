namespace WinDiagMcpServer.Resources.Registry;

/// <summary>
/// Represents a captured registry snapshot entry for a specific hive and key with serialized content.
/// </summary>
/// <param name="Hive">The registry hive where the snapshot was taken.</param>
/// <param name="Key">The registry key path captured in the snapshot.</param>
/// <param name="JsonContent">The serialized JSON content representing the key value.</param>
public record RegistrySnapshotEntry(string Hive, string Key, string JsonContent);
