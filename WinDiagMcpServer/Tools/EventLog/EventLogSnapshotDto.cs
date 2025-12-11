namespace WinDiagMcpServer.Tools.EventLog;

public partial class McpServerEventLogToolType
{
    private sealed record EventLogSnapshotDto
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public string LogName { get; init; } = string.Empty;

        public string XPathQuery { get; init; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; init; }

        public int EventCount { get; init; }

        public List<EventLogRecordDto> Events { get; init; } = new();
    }
}
