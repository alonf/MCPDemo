namespace WinDiagMcpServer.Tools.Process;

/// <summary>
/// Represents the outcome of a kill process request.
/// </summary>
public sealed record KillProcessResult
{
    /// <summary>
    /// Gets the process identifier for the targeted process.
    /// </summary>
    public int ProcessId { get; init; }

    /// <summary>
    /// Gets the process name for additional context.
    /// </summary>
    public string ProcessName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the status string (terminated, cancelled, failed, not-found).
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Gets a human-readable message describing the result.
    /// </summary>
    public string Message { get; init; } = string.Empty;

    /// <summary>
    /// Gets the optional user-provided reason for terminating the process.
    /// </summary>
    public string? Reason { get; init; }

    public static KillProcessResult Success(int processId, string processName, string? reason) => new()
    {
        ProcessId = processId,
        ProcessName = processName,
        Status = "terminated",
        Message = $"Process {processName} (PID {processId}) was terminated successfully.",
        Reason = reason
    };

    public static KillProcessResult Cancelled(string message) => new()
    {
        ProcessId = -1,
        ProcessName = string.Empty,
        Status = "cancelled",
        Message = message
    };

    public static KillProcessResult NotFound(int processId) => new()
    {
        ProcessId = processId,
        ProcessName = string.Empty,
        Status = "not-found",
        Message = $"No process found with PID {processId}."
    };

    public static KillProcessResult Failed(int processId, string processName, string errorMessage) => new()
    {
        ProcessId = processId,
        ProcessName = processName,
        Status = "failed",
        Message = $"Failed to terminate {processName} (PID {processId}): {errorMessage}"
    };
}
