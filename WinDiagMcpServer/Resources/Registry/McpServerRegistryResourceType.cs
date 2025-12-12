using System.ComponentModel;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Resources.Registry;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Provides MCP registry resources that expose stored registry snapshots via resource endpoints.
/// </summary>
/// <param name="snapshotStorage">Storage abstraction used to retrieve registry snapshot entries.</param>
[McpServerResourceType]
public class McpServerRegistryResourceType(IRegistrySnapshotStorage snapshotStorage)
{
    /// <summary>
    /// Retrieves the JSON content for the registry snapshot identified by the specified <paramref name="id" />.
    /// </summary>
    /// <param name="id">Identifier of the snapshot stored within the registry snapshot storage.</param>
    /// <returns>The JSON representation of the requested registry snapshot.</returns>
    [McpServerResource(
        UriTemplate = "registry://snapshot/{id}",
        Name = "Registry Snapshot",
        MimeType = "application/json")]
    [Description("Gets the content of a registry snapshot.")]
    public string GetRegistrySnapshot(string id)
    {
        if (!snapshotStorage.TryGetSnapshot(id, out var entry))
        {
            throw new KeyNotFoundException($"Snapshot {id} not found.");
        }

        return entry.JsonContent;
    }
}
