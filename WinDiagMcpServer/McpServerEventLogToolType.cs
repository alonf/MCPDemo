using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Eventing.Reader;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer;

/// <summary>
/// Provides tools for capturing diagnostics data from Windows event logs.
/// </summary>
[McpServerToolType]
public partial class McpServerEventLogToolType
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<McpServerEventLogToolType> _logger;

    public McpServerEventLogToolType(ILogger<McpServerEventLogToolType> logger)
    {
        _logger = logger;
    }

    public static ConcurrentDictionary<string, (string XPathQuery, string JsonContent)> EventLogSnapshots { get; } = new();

    /// <summary>
    /// Takes a snapshot of the specified Windows event log using an XPath query and returns a resource URI for the snapshot.
    /// </summary>
    /// <param name="logName">The name of the event log to query.</param>
    /// <param name="xPathQuery">The XPath query string used to filter events.</param>
    /// <returns>The resource URI of the created snapshot or an error message if validation fails.</returns>
    [McpServerTool]
    [Description("Take a snapshot of the event log, and create a resource. Return the resource URI")]
    public partial string CreateEventLogSnapshot(
        [Description("The name of the event log to query (e.g., 'Application', 'Security', 'Setup', 'System').")] string logName,
        [Description("The XPath query string used to filter the events.")] string xPathQuery)
    {
        var requestId = Guid.NewGuid();
        _logger.LogInformation("EventLogSnapshot request {RequestId} started for {LogName} with query {XPathQuery}", requestId, logName, xPathQuery);

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            ValidateLogName(logName);

            var eventsQuery = new EventLogQuery(logName, PathType.LogName, xPathQuery);

            using var logReader = new EventLogReader(eventsQuery);
            var records = new List<EventLogRecordDto>();

            EventRecord? record;
            while ((record = logReader.ReadEvent()) is not null)
            {
                using (record)
                {
                    records.Add(new EventLogRecordDto
                    {
                        RecordId = record.RecordId,
                        ProviderName = record.ProviderName,
                        LevelDisplayName = record.LevelDisplayName,
                        TimeCreated = record.TimeCreated,
                        MachineName = record.MachineName,
                        LogName = record.LogName,
                        ActivityId = record.ActivityId,
                        Opcode = record.OpcodeDisplayName,
                        Task = record.TaskDisplayName,
                        Message = SafeGetDescription(record)
                    });
                }
            }

            var snapshot = new EventLogSnapshotDto
            {
                LogName = logName,
                XPathQuery = xPathQuery,
                CreatedAtUtc = DateTimeOffset.UtcNow,
                EventCount = records.Count,
                Events = records
            };

            var payload = JsonSerializer.Serialize(snapshot, _jsonSerializerOptions);
            var resourceUri = $"resource://eventlogs/{Guid.NewGuid():N}.json";
            EventLogSnapshots[resourceUri] = (xPathQuery, payload);

            stopwatch.Stop();
            _logger.LogInformation("EventLogSnapshot request {RequestId} completed. Resource {ResourceUri} with {EventCount} events in {Duration}ms", requestId, resourceUri, records.Count, stopwatch.ElapsedMilliseconds);

            return resourceUri;
        }
        catch (ArgumentException ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "EventLogSnapshot request {RequestId} failed validation for log {LogName}", requestId, logName);
            return ex.Message;
        }
        catch (EventLogException ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex, "EventLogSnapshot request {RequestId} encountered EventLog error for log {LogName}", requestId, logName);
            return $"Event log error: {ex.Message}";
        }
    }

    /// <summary>
    /// Retrieves all stored event log snapshot resource URIs and their corresponding XPath queries.
    /// </summary>
    /// <returns>A list of snapshot resource descriptors containing the resource URI and XPath query.</returns>
    [McpServerTool]
    [Description("Get the list of all event log snapshot resource URIs and their XPath queries.")]
    public List<EventLogSnapshotResourceInfo> GetAllEventLogSnapshotResources()
    {
        var resources = EventLogSnapshots
            .Select(kvp => new EventLogSnapshotResourceInfo
            {
                // Build the actual resource URI that matches your UriTemplate
                ResourceUri = $"eventlog://snapshot/{kvp.Key}",
                XPathQuery = kvp.Value.XPathQuery
            })
            .ToList();

        _logger.LogInformation("Listing {ResourceCount} event log snapshot resources", resources.Count);
        return resources;
    }

    private static void ValidateLogName(string logName)
    {
        switch (logName)
        {
            case "Application":
            case "Security":
            case "Setup":
            case "System":
            case "ForwardedEvents":
                return;
            default:
                throw new ArgumentException("Invalid log name. Allowed values: Application, Security, Setup, System, ForwardedEvents.", nameof(logName));
        }
    }

    private static string SafeGetDescription(EventRecord record)
    {
        try
        {
            return record.FormatDescription() ?? string.Empty;
        }
        catch (EventLogException)
        {
            return string.Empty;
        }
    }
}
