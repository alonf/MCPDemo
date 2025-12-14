using System.ComponentModel;
using System.Text.Json;
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
    /// <param name="limitParam">Optional page size query parameter (default: 50, max: 500).</param>
    /// <param name="offsetParam">Optional offset query parameter (default: 0).</param>
    /// <returns>The JSON representation of the requested registry snapshot.</returns>
    [McpServerResource(
        UriTemplate = "registry://snapshot/{id}?limit={limitParam}&offset={offsetParam}",
        Name = "registry_snapshot",
        MimeType = "application/json")]
    [Description("Gets the content of a registry snapshot resource with pagination via 'limit' (default: 50, max: 500) and 'offset' (default: 0). Returns a flattened list of keys under the snapshot root so paging is deterministic. Example: registry://snapshot/abc123?limit=50&offset=0")]
    public string GetRegistrySnapshotWithPaging(
        RequestContext<ReadResourceRequestParams> context,
        string id,
        int? limitParam,
        int? offsetParam)
    {
        var limit = limitParam ?? 50;
        var offset = offsetParam ?? 0;

        limit = limit switch
        {
            <= 0 => throw new McpException("Parameter 'limit' must be greater than 0"),
            > 500 => 500,
            _ => limit
        };

        if (offset < 0)
        {
            throw new McpException("Parameter 'offset' cannot be negative");
        }

        if (!snapshotStorage.TryGetSnapshot(id, out var entry))
        {
            throw new McpException($"Unknown resource URI: '{context.Params?.Uri}'");
        }

        RegistryKeyDto root;
        try
        {
            root = JsonSerializer.Deserialize<RegistryKeyDto>(entry.JsonContent)
                   ?? throw new McpException("Registry snapshot JSON could not be parsed.");
        }
        catch (JsonException ex)
        {
            throw new McpException($"Registry snapshot JSON is invalid: {ex.Message}");
        }

        var allKeys = new List<RegistryKeyPageItem>();
        FlattenRegistryTree(root, entry.Hive, entry.Key, allKeys);

        var totalCount = allKeys.Count;
        var pagedKeys = allKeys
            .Skip(offset)
            .Take(limit)
            .ToList();

        var pagedSnapshot = new
        {
            SnapshotId = id,
            entry.Hive,
            entry.Key,
            KeyCount = totalCount,
            Keys = pagedKeys,
            Pagination = new
            {
                TotalCount = totalCount,
                ReturnedCount = pagedKeys.Count,
                Limit = limit,
                Offset = offset,
                HasMore = offset + limit < totalCount,
                NextOffset = offset + limit < totalCount ? offset + limit : (int?)null
            }
        };

        return JsonSerializer.Serialize(pagedSnapshot, new JsonSerializerOptions
        {
            WriteIndented = true
        });
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

    private void FlattenRegistryTree(
        RegistryKeyDto node,
        string hive,
        string rootKey,
        List<RegistryKeyPageItem> results)
    {
        // The snapshot JSON's root node uses Name = <key path>.
        // We'll produce an absolute registry path so clients can reliably identify items.
        var basePath = $"{hive}\\{rootKey}";
        FlattenRegistryTreeCore(node, basePath, results, 0);
    }

    private void FlattenRegistryTreeCore(
        RegistryKeyDto node,
        string currentPath,
        List<RegistryKeyPageItem> results,
        int depth)
    {
        var item = new RegistryKeyPageItem
        {
            Path = currentPath,
            Name = node.Name,
            Depth = depth,
            Values = node.Values,
            SubKeyCount = node.SubKeys.Count
        };

        results.Add(item);

        if (node.SubKeys.Count == 0)
        {
            return;
        }

        foreach (var child in node.SubKeys)
        {
            var childPath = string.IsNullOrWhiteSpace(child.Name)
                ? currentPath
                : $"{currentPath}\\{child.Name}";

            FlattenRegistryTreeCore(child, childPath, results, depth + 1);
        }
    }

    private sealed class RegistryKeyDto
    {
        public string Name { get; init; } = string.Empty;

        public Dictionary<string, string> Values { get; init; } = new();

        public List<RegistryKeyDto> SubKeys { get; init; } = new();
    }

    private sealed class RegistryKeyPageItem
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string Path { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public int Depth { get; set; }

        public Dictionary<string, string> Values { get; set; } = new();

        public int SubKeyCount { get; set; }
    }
}
