using System.ComponentModel;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
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
    /// <param name="context">Request context containing the resource URI and request metadata.</param>
    /// <param name="id">Identifier of the snapshot stored within the registry snapshot storage.</param>
    /// <param name="limitParam">Optional page size query parameter (ignored).</param>
    /// <param name="offsetParam">Optional offset query parameter (ignored).</param>
    /// <returns>The JSON representation of the requested registry snapshot.</returns>
    [McpServerResource(
        UriTemplate = "registry://snapshot/{id}?limit={limitParam}&offset={offsetParam}",
        Name = "registry_snapshot",
        MimeType = "application/json")]
    [Description("Gets the content of a registry snapshot resource. Accepts optional 'limit' and 'offset' query parameters (ignored). Example: registry://snapshot/abc123?limit=50&offset=0")]
    public string GetRegistrySnapshotWithPaging(
        RequestContext<ReadResourceRequestParams> context,
        string id,
        int? limitParam,
        int? offsetParam)
    {
        _ = limitParam;
        _ = offsetParam;

        return GetRegistrySnapshotCore(context, id);
    }

    /// <summary>
    /// Retrieves the JSON content for the registry snapshot identified by the specified <paramref name="id" />.
    /// </summary>
    /// <param name="context">Request context containing the resource URI and request metadata.</param>
    /// <param name="id">Identifier of the snapshot stored within the registry snapshot storage.</param>
    /// <returns>The JSON representation of the requested registry snapshot.</returns>
    [McpServerResource(
        UriTemplate = "registry://snapshot/{id}",
        Name = "registry_snapshot",
        MimeType = "application/json")]
    [Description("Gets the content of a registry snapshot resource.")]
    public string GetRegistrySnapshot(
        RequestContext<ReadResourceRequestParams> context,
        string id)
        => GetRegistrySnapshotCore(context, id);

    private string GetRegistrySnapshotCore(
        RequestContext<ReadResourceRequestParams> context,
        string id)
    {
        if (!snapshotStorage.TryGetSnapshot(id, out var entry))
        {
            throw new McpException($"Unknown resource URI: '{context.Params?.Uri}'");
        }

        return entry.JsonContent;
    }
}
