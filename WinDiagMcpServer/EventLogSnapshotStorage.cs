using System.Collections.Concurrent;

namespace WinDiagMcpServer;

/// <summary>
/// Default implementation of event log snapshot storage using a concurrent dictionary.
/// </summary>
public sealed class EventLogSnapshotStorage : IEventLogSnapshotStorage
{
    private readonly ConcurrentDictionary<string, EventLogSnapshotEntry> _snapshots = new();

    public void AddSnapshot(string id, string xPathQuery, string jsonContent)
    {
        _snapshots[id] = new EventLogSnapshotEntry(xPathQuery, jsonContent);
    }

    public bool TryGetSnapshot(string id, out EventLogSnapshotEntry entry)
    {
        if (_snapshots.TryGetValue(id, out var snapshotEntry))
        {
            entry = snapshotEntry;
            return true;
        }

        entry = null!;
        return false;
    }

    public IReadOnlyCollection<KeyValuePair<string, EventLogSnapshotEntry>> GetAllSnapshots()
    {
        return _snapshots.ToArray();
    }
}
