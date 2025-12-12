using System.Collections.Concurrent;

namespace WinDiagMcpServer.Resources.Registry;

public sealed class RegistrySnapshotStorage : IRegistrySnapshotStorage
{
    private readonly ConcurrentDictionary<string, RegistrySnapshotEntry> _snapshots = new();

    public void AddSnapshot(string id, string hive, string key, string jsonContent)
    {
        _snapshots[id] = new RegistrySnapshotEntry(hive, key, jsonContent);
    }

    public bool TryGetSnapshot(string id, out RegistrySnapshotEntry entry)
    {
        if (_snapshots.TryGetValue(id, out var snapshotEntry))
        {
            entry = snapshotEntry;
            return true;
        }

        entry = null!;
        return false;
    }
}
