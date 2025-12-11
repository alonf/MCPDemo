using System.ComponentModel;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Resources.EventLog;

// ReSharper disable UnusedMember.Global
[McpServerResourceType]
public class McpServerEventLogResourceType
{
    private readonly ILogger<McpServerEventLogResourceType> _logger;
    private readonly IEventLogSnapshotStorage _snapshotStorage;

#pragma warning disable IDE0290
    public McpServerEventLogResourceType(
        ILogger<McpServerEventLogResourceType> logger,
        IEventLogSnapshotStorage snapshotStorage)
    {
        _logger = logger;
        _snapshotStorage = snapshotStorage;
    }
#pragma warning restore IDE0290

    /// <summary>
    /// Retrieves the JSON content of a requested event log snapshot resource with pagination support.
    /// </summary>
    /// <param name="context">The request context that contains the resource URI and request metadata.</param>
    /// <param name="id">The identifier of the event log snapshot to retrieve.</param>
    /// <param name="limitParam">The optional page size.</param>
    /// <param name="offsetParam">The optional paging page number.</param>
    /// <returns>The paginated JSON content of the event log snapshot if it exists.</returns>
    /// <exception cref="McpException">Thrown when the specified resource URI cannot be found.</exception>
    [McpServerResource(
        UriTemplate = "eventlog://snapshot/{id}?limit={limitParam}&offset={offsetParam}",
        Name = "eventlog_snapshot",
        MimeType = "application/json")]
    [Description("Gets the content of an event log snapshot resource. Supports pagination via 'limit' (default: 50, max: 500) and 'offset' (default: 0) query parameters. Example: eventlog://snapshot/abc123?limit=10&offset=0")]
    public string GetEventLogSnapshotContent(
        RequestContext<ReadResourceRequestParams> context,
        string id,
        int? limitParam,
        int? offsetParam)
    {
        // Parse query parameters from the URI
        var limit = limitParam ?? 50;  // Default
        var offset = offsetParam ?? 0;  // Default

        switch (limit)
        {
            // Validate pagination parameters
            case <= 0:
                throw new McpException("Parameter 'limit' must be greater than 0");
            case > 500:
                limit = 500; // Cap at max
                _logger.LogWarning("Limit capped at maximum value of 500");
                break;
        }

        if (offset < 0)
        {
            throw new McpException("Parameter 'offset' cannot be negative");
        }

        if (!_snapshotStorage.TryGetSnapshot(id, out var entry))
        {
            _logger.LogWarning(
                "Requested event log snapshot {Id} for URI {Uri} was not found",
                id,
                context.Params?.Uri);

            throw new McpException($"Unknown resource URI: '{context.Params?.Uri}'");
        }

        _logger.LogInformation(
            "Serving event log snapshot {Id} for URI {Uri} with pagination limit={Limit}, offset={Offset}",
            id,
            context.Params?.Uri,
            limit,
            offset);

        // Parse the full JSON using the existing DTO structure
        using var document = JsonDocument.Parse(entry.JsonContent);
        var root = document.RootElement;

        // Extract events array
        var eventsArray = root.GetProperty("Events");
        var totalCount = eventsArray.GetArrayLength();

        // Apply pagination to the JSON elements directly
        var paginatedEvents = eventsArray
            .EnumerateArray()
            .Skip(offset)
            .Take(limit)
            .ToList();

        // Create paginated response with metadata
        var paginatedSnapshot = new
        {
            SnapshotId = id,
            LogName = root.GetProperty("LogName").GetString(),
            XPathQuery = root.GetProperty("XPathQuery").GetString(),
            CreatedAtUtc = root.GetProperty("CreatedAtUtc").GetDateTimeOffset(),
            EventCount = totalCount, // Total count
            Events = paginatedEvents,
            Pagination = new
            {
                TotalCount = totalCount,
                ReturnedCount = paginatedEvents.Count,
                Limit = limit,
                Offset = offset,
                HasMore = (offset + limit) < totalCount,
                NextOffset = (offset + limit) < totalCount ? offset + limit : (int?)null
            }
        };

        return JsonSerializer.Serialize(paginatedSnapshot, new JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
