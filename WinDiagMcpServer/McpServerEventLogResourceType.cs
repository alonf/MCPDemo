using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer;

[McpServerResourceType]
public partial class McpServerEventLogResourceType
{
    private readonly ILogger<McpServerEventLogResourceType> _logger;

    public McpServerEventLogResourceType(ILogger<McpServerEventLogResourceType> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Retrieves the JSON content of a requested event log snapshot resource.
    /// </summary>
    /// <param name="context">The request context that contains the resource URI and request metadata.</param>
    /// <param name="id">The identifier of the event log snapshot to retrieve.</param>
    /// <returns>The JSON content of the event log snapshot if it exists.</returns>
    /// <exception cref="McpException">Thrown when the specified resource URI cannot be found.</exception>
    [McpServerResource(
    UriTemplate = "eventlog://snapshot/{id}",
    Name = "eventlog_snapshot",
    MimeType = "application/json")]
    [Description("Gets the content of an event log snapshot resource.")]
    public string GetEventLogSnapshotContent(
    RequestContext<ReadResourceRequestParams> context, string id)
    {
        if (McpServerEventLogToolType.EventLogSnapshots.TryGetValue(id, out var entry))
        {
            _logger.LogInformation(
                "Serving event log snapshot {Id} for URI {Uri} with query {XPathQuery}",
                id,
                context.Params?.Uri,
                entry.XPathQuery);

            return entry.JsonContent;
        }

        _logger.LogWarning(
            "Requested event log snapshot {Id} for URI {Uri} was not found",
            id,
            context.Params?.Uri);

        // Important: surface a proper MCP error if you want the client to see a failure
        throw new McpException($"Unknown resource URI: '{context.Params?.Uri}'");
    }
}
