using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts.EventLog;

/// <summary>
/// Provides prompts for analyzing Windows event logs.
/// </summary>
// ReSharper disable UnusedMember.Global
[McpServerPromptType]
public class EventLogAnalysisPromptType
{
    private readonly ILogger<EventLogAnalysisPromptType> _logger;

#pragma warning disable IDE0290
    public EventLogAnalysisPromptType(ILogger<EventLogAnalysisPromptType> logger)
#pragma warning restore IDE0290
    {
        _logger = logger;
    }

    /// <summary>
    /// Provides a structured workflow prompt for analyzing recent application errors in the event log.
    /// </summary>
    /// <param name="hoursBack">Number of hours to look back for errors (default: 24).</param>
    /// <returns>A prompt guiding the analysis of recent application errors.</returns>
    [McpServerPrompt]
    [Description("Analyzes recent application errors from Windows Event Log")]
    public string AnalyzeRecentApplicationErrors(
        [Description("Number of hours to look back for errors (default: 24)")] int hoursBack = 24)
    {
        _logger.LogInformation("Generating AnalyzeRecentApplicationErrors prompt for last {HoursBack} hours", hoursBack);

        var timestamp = DateTimeOffset.UtcNow.AddHours(-hoursBack).ToString("yyyy-MM-ddTHH:mm:ss.000Z");
        var xpathQuery = $"*[System/Level=2 and System/TimeCreated[@SystemTime >= '{timestamp}']]";

        return $"""
            I need you to analyze recent application errors on this Windows system.
            
            **WORKFLOW:**
            
            1. **Create Event Log Snapshot:**
               - Use the `create_event_log_snapshot` tool
               - Parameters:
                 - logName: "Application"
                 - xPathQuery: "{xpathQuery}"
               - This captures all errors (Level=2) from the last {hoursBack} hours
               - The tool will return a resourceUri like "eventlog://snapshot/abc123"
            
            2. **Read the Snapshot Resource:**
               - Use the `read_resource` tool with the resourceUri you received
               - Start with pagination: add "?limit=10&offset=0" to the resourceUri
               - Example: "eventlog://snapshot/abc123?limit=10&offset=0"
               - Check the Pagination.TotalCount in the response
            
            3. **Analyze Patterns:**
               - Group errors by ProviderName and Message patterns
               - Identify the most frequent error types
               - Look for critical issues that need immediate attention
               - Note any error cascades or related events
            
            4. **Provide Recommendations:**
               - Summarize the key findings
               - For each major error pattern:
                 - Severity assessment
                 - Possible causes
                 - Recommended actions
               - Prioritize issues by impact
            
            **IMPORTANT:**
            - Only read more pages if you need specific details
            - For large result sets (>100 events), analyze patterns rather than listing every event
            - Focus on actionable insights
            
            Please begin by creating the event log snapshot.
            """;
    }
}
