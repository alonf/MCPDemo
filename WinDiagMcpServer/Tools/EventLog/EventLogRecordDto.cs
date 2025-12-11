namespace WinDiagMcpServer.Tools.EventLog;

public partial class McpServerEventLogToolType
{
    private sealed record EventLogRecordDto
    {
        // ReSharper disable UnusedAutoPropertyAccessor.Local
        public long? RecordId { get; init; }

        public string? ProviderName { get; init; }

        public string? LevelDisplayName { get; init; }

        public DateTime? TimeCreated { get; init; }

        public string? MachineName { get; init; }

        public string? LogName { get; init; }

        public Guid? ActivityId { get; init; }

        public string? Opcode { get; init; }

        public string? Task { get; init; }

        public string Message { get; init; } = string.Empty;
    }
}
