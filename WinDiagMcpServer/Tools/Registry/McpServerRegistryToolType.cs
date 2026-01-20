using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using WinDiagMcpServer.Resources.Registry;
using WinDiagMcpServer.Services;
using static ModelContextProtocol.Protocol.ElicitRequestParams;

namespace WinDiagMcpServer.Tools.Registry;

// ReSharper disable UnusedMember.Global

/// <summary>
/// Provides Model Context Protocol tools for inspecting and configuring Windows Registry roots and snapshots.
/// </summary>
/// <param name="rootsService">Service that enforces registry root access restrictions.</param>
/// <param name="snapshotStorage">Snapshot persistence service for storing serialized registry data.</param>
/// <param name="logger">Logger used to record registry traversal warnings.</param>
[McpServerToolType]
public class McpServerRegistryToolType(
    RegistryRootsService rootsService,
    IRegistrySnapshotStorage snapshotStorage,
    ILogger<McpServerRegistryToolType> logger)
{
    /// <summary>
    /// Creates and stores a serialized snapshot of the specified registry key and optional subkeys.
    /// </summary>
    /// <param name="server">MCP server used to emit progress notifications.</param>
    /// <param name="hive">Registry hive identifier (e.g., HKLM).</param>
    /// <param name="key">Path of the registry key to capture.</param>
    /// <param name="recursive">Whether subkeys should be traversed recursively.</param>
    /// <param name="maxDepth">Maximum depth for recursion.</param>
    /// <param name="filter">Filter string to include only matching keys and values (case-insensitive).</param>
    /// <param name="cancellationToken">Token used to cancel the snapshot operation.</param>
    /// <returns>URI for the stored registry snapshot resource or an error string if the key is missing.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the requested path falls outside allowed roots.</exception>
    /// <exception cref="ArgumentException">Thrown when an invalid hive identifier is provided.</exception>
    [McpServerTool]
    [Description("Primary tool for reading Windows Configuration. Creates a snapshot of a registry key and its subkeys. Requires the path to be in the allowed roots (configured via configure_registry_roots). Use this to find: Installed Software versions, Auto-Run/Startup programs, System Configurations, USB Device History, Windows Build info, and much more.")]
    public async Task<string> CreateRegistrySnapshotAsync(
        McpServer server,
        [Description("The registry hive (HKLM, HKCU, HKCR, HKU, HKCC).")] string hive,
        [Description("The registry key path.")] string key,
        [Description("Whether to recurse into subkeys.")] bool recursive = false,
        [Description("Maximum depth for recursion (0 = only current key).")] int? maxDepth = null,
        [Description("Filter string to include only matching keys and values (case-insensitive).")] string? filter = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Validate Roots
            if (!rootsService.IsPathAllowed(hive, key))
            {
                var allowed = string.Join(", ", rootsService.GetAllowedRoots());
                throw new InvalidOperationException($"Access denied. The path '{hive}\\{key}' is not within the allowed roots. Please call the 'request_registry_access' tool to ask the user for permission. Currently allowed: {allowed}");
            }

            // 2. Parse Hive
            var registryHive = ParseHive(hive);

            // 3. Crawl Registry
            var snapshotId = Guid.NewGuid().ToString("N");
            var rootKeyDto = new RegistryKeyDto { Name = key };

            using var baseKey = RegistryKey.OpenBaseKey(registryHive, RegistryView.Default);
            using var subKey = baseKey.OpenSubKey(key);

            if (subKey == null)
            {
                return $"Error: Key '{hive}\\{key}' not found.";
            }

            // If maxDepth is specified and > 0, imply recursion
            if (maxDepth is > 0)
            {
                recursive = true;
            }

            await CrawlKeyAsync(server, subKey, rootKeyDto, recursive, 0, maxDepth, filter, cancellationToken);

            // 4. Save Snapshot
            var json = JsonSerializer.Serialize(rootKeyDto, new JsonSerializerOptions { WriteIndented = true });
            snapshotStorage.AddSnapshot(snapshotId, hive, key, json);

            // 5. Return Resource URI
            return $"registry://snapshot/{snapshotId}";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create registry snapshot for {Hive}\\{Key}", hive, key);
            throw ex.ToMcpException($"Failed to snapshot registry key '{hive}\\{key}'");
        }
    }

    /// <summary>
    /// Requests permission from the user to access a specific registry path.
    /// </summary>
    /// <param name="server">The MCP server instance to handle elicitation.</param>
    /// <param name="path">The registry path to request access for.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A message indicating the result of the request.</returns>
    [McpServerTool]
    [Description("Requests permission from the user to access a specific registry path. Use this when 'create_registry_snapshot' returns Access Denied.")]
    public async Task<string> RequestRegistryAccessAsync(
        McpServer server,
        [Description("The registry path to request access for (e.g., HKLM\\Software).")] string path,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "Invalid path.";
        }

        // Elicit permission from the user
        var prompt = $"The AI Agent is requesting access to the registry path: '{path}'.\n" +
                     "Do you want to allow this? (Type 'YES' to confirm)";

        var schema = new RequestSchema
        {
            Properties =
            {
                ["confirmation"] = new StringSchema
                {
                    Description = "Type 'YES' to allow access, or anything else to deny."
                }
            }
        };

        var result = await server.ElicitAsync(
            new ElicitRequestParams
            {
                Message = prompt,
                RequestedSchema = schema
            },
            cancellationToken);

        var userResponse = result.Content is not null &&
                           result.Content.TryGetValue("confirmation", out var entry) &&
                           entry.ValueKind == JsonValueKind.String
            ? entry.GetString() : null;

        if (string.Equals(userResponse, "YES", StringComparison.OrdinalIgnoreCase))
        {
            rootsService.AddAllowedRoot(path);
            return $"Access granted to '{path}'. You may now retry the operation.";
        }

        return "Access denied by user.";
    }

    /// <summary>
    /// Converts a user-provided hive identifier into the corresponding <see cref="RegistryHive"/> value.
    /// </summary>
    /// <param name="hive">Hive string provided by the caller.</param>
    /// <returns>The parsed <see cref="RegistryHive"/> enumeration value.</returns>
    /// <exception cref="ArgumentException">Thrown when the hive cannot be recognized.</exception>
    private static RegistryHive ParseHive(string hive)
    {
        return hive.ToUpperInvariant() switch
        {
            "HKLM" or "HKEY_LOCAL_MACHINE" => RegistryHive.LocalMachine,
            "HKCU" or "HKEY_CURRENT_USER" => RegistryHive.CurrentUser,
            "HKCR" or "HKEY_CLASSES_ROOT" => RegistryHive.ClassesRoot,
            "HKU" or "HKEY_USERS" => RegistryHive.Users,
            "HKCC" or "HKEY_CURRENT_CONFIG" => RegistryHive.CurrentConfig,
            _ => throw new ArgumentException($"Invalid hive: {hive}")
        };
    }

    /// <summary>
    /// Recursively reads registry values and subkeys into the provided DTO while emitting progress notifications.
    /// </summary>
    /// <param name="server">MCP server for progress notifications.</param>
    /// <param name="currentKey">Registry key currently being inspected.</param>
    /// <param name="dto">DTO that accumulates key metadata, values, and child keys.</param>
    /// <param name="recursive">Indicates whether child keys should be traversed.</param>
    /// <param name="currentDepth">Current depth of recursion.</param>
    /// <param name="maxDepth">Maximum depth of recursion.</param>
    /// <param name="filter">Filter string for keys and values.</param>
    /// <param name="token">Cancellation token to halt traversal.</param>
    /// <returns>A task representing the asynchronous traversal operation.</returns>
    /// <exception cref="OperationCanceledException">Thrown if traversal is canceled.</exception>
    private async Task CrawlKeyAsync(
        McpServer server,
        RegistryKey currentKey,
        RegistryKeyDto dto,
        bool recursive,
        int currentDepth,
        int? maxDepth,
        string? filter,
        CancellationToken token)
    {
        token.ThrowIfCancellationRequested();

        // Notify progress
        await server.SendNotificationAsync("notifications/progress", new { message = $"Scanning: {currentKey.Name}" }, null, token);

        // Read Values
        var valueNames = currentKey.GetValueNames();
        if (!string.IsNullOrEmpty(filter))
        {
            valueNames = valueNames
                .Where(v => v.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        foreach (var valueName in valueNames)
        {
            var value = currentKey.GetValue(valueName);
            dto.Values[valueName] = value?.ToString() ?? "null";
        }

        if (!recursive)
        {
            return;
        }

        if (maxDepth.HasValue && currentDepth >= maxDepth.Value)
        {
            return;
        }

        // Recurse SubKeys
        var subKeyNames = currentKey.GetSubKeyNames();
        if (!string.IsNullOrEmpty(filter))
        {
            subKeyNames = subKeyNames
                .Where(k => k.Contains(filter, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        foreach (var subKeyName in subKeyNames)
        {
            try
            {
                using var subKey = currentKey.OpenSubKey(subKeyName);

                if (subKey == null)
                {
                    continue;
                }

                var subDto = new RegistryKeyDto { Name = subKeyName };
                dto.SubKeys.Add(subDto);
                await CrawlKeyAsync(server, subKey, subDto, true, currentDepth + 1, maxDepth, filter, token);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to open subkey {SubKey}", subKeyName);
                dto.SubKeys.Add(new RegistryKeyDto { Name = subKeyName, Values = { ["Error"] = ex.Message } });
            }
        }
    }
}
