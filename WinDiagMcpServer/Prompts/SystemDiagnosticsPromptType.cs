using System.ComponentModel;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer.Prompts;

/// <summary>
/// Provides prompts for general system diagnostics workflows that span multiple tools.
/// </summary>
// ReSharper disable UnusedMember.Global
[McpServerPromptType]
public class SystemDiagnosticsPromptType(ILogger<SystemDiagnosticsPromptType> logger)
{
    /// <summary>
    /// Provides a structured workflow prompt for investigating high CPU usage.
    /// </summary>
    /// <returns>A prompt guiding the investigation of high CPU usage.</returns>
    // ReSharper disable UnusedMember.Global
    [McpServerPrompt]
    [Description("Investigates processes causing high CPU usage")]
    public string ExplainHighCpu()
    {
        logger.LogInformation("Generating ExplainHighCpu prompt");

        return """
            I need you to investigate high CPU usage on this Windows system.
            
            **WORKFLOW:**
            
            1. **Get Current Process Information:**
               - Use the `get_all_processes` tool to get a snapshot of running processes
               - This will show CPU usage, memory usage, and other process details
            
            2. **Identify Top CPU Consumers:**
               - Sort processes by CPU percentage
               - Focus on the top 5-10 processes consuming the most CPU
               - Note if any processes are using abnormally high CPU
            
            3. **Check for Related Event Log Entries:**
               - Use `create_event_log_snapshot` with these queries:
                 - Application errors: logName="Application", xPathQuery="*[System/Level=2]"
                 - System warnings: logName="System", xPathQuery="*[System/Level=3]"
               - Use `read_resource` to check for errors related to high CPU processes
               - Look for patterns linking events to the CPU spikes
            
            4. **Analyze and Report:**
               - For each high CPU process:
                 - Process name and ID
                 - CPU and memory usage
                 - Whether it's a system or user process
                 - Any associated event log errors
               - Identify if this is:
                 - Normal expected behavior
                 - A runaway process
                 - A system service issue
                 - Potential malware
            
            5. **Provide Recommendations:**
               - Suggest actions to address each high CPU process
               - Indicate severity (normal/warning/critical)
               - Provide next steps for investigation if needed
            
            Please begin by getting the current process information.
            """;
    }

    /// <summary>
    /// Provides a structured workflow prompt for detecting security anomalies.
    /// </summary>
    /// <param name="hoursBack">Number of hours to look back for security events (default: 24).</param>
    /// <returns>A prompt guiding the detection of security anomalies.</returns>
    [McpServerPrompt]
    [Description("Detects potential security anomalies in processes and event logs")]
    public string DetectSecurityAnomalies(
        [Description("Number of hours to look back for security events (default: 24)")] int hoursBack = 24)
    {
        logger.LogInformation("Generating DetectSecurityAnomalies prompt for last {HoursBack} hours", hoursBack);

        var timestamp = DateTimeOffset.UtcNow.AddHours(-hoursBack).ToString("yyyy-MM-ddTHH:mm:ss.000Z");

        return $"""
            I need you to check for potential security anomalies on this Windows system.
            
            **WORKFLOW:**
            
            1. **Review Running Processes:**
               - Use the `get_all_processes` tool
               - Look for suspicious processes:
                 - Unusual process names
                 - Processes running from temporary directories
                 - High resource usage from unknown processes
                 - Multiple instances of the same process
            
            2. **Check Security Event Log:**
               - Use `create_event_log_snapshot` with:
                 - logName: "Security"
                 - xPathQuery: "*[System/TimeCreated[@SystemTime >= '{timestamp}']]"
               - Use `read_resource` to examine security events with pagination
               - Look for:
                 - Failed login attempts
                 - Account lockouts
                 - Permission changes
                 - Unusual access patterns
            
            3. **Check System Event Log:**
               - Use `create_event_log_snapshot` with:
                 - logName: "System"
                 - xPathQuery: "*[System/Level=2 and System/TimeCreated[@SystemTime >= '{timestamp}']]"
               - Look for:
                 - Service failures
                 - Driver issues
                 - System crashes
            
            4. **Analyze Patterns:**
               - Correlate process information with event log entries
               - Identify any suspicious patterns or anomalies
               - Determine if issues are related or isolated
            
            5. **Security Assessment:**
               - Rate the severity of findings (Normal/Low/Medium/High/Critical)
               - Explain the potential security implications
               - Provide specific recommendations for each finding
               - Suggest immediate actions if critical issues are found
            
            **IMPORTANT:**
            - Use pagination when reading large result sets
            - Focus on anomalies, not routine events
            - Prioritize findings by security impact
            
            Please begin by reviewing the running processes.
            """;
    }

    /// <summary>
    /// Provides a comprehensive system health diagnostic workflow without requiring elevation.
    /// Analyzes processes, memory usage, and correlates with application/system event logs.
    /// </summary>
    /// <param name="hoursBack">Number of hours to look back for event log analysis (default: 24).</param>
    /// <returns>A prompt guiding comprehensive system health diagnosis.</returns>
    [McpServerPrompt]
    [Description("Performs comprehensive system health diagnosis without requiring elevated permissions")]
    public string DiagnoseSystemHealth(
        [Description("Number of hours to look back for event log analysis (default: 24)")] int hoursBack = 24)
    {
        logger.LogInformation("Generating DiagnoseSystemHealth prompt for last {HoursBack} hours", hoursBack);

        var timestamp = DateTimeOffset.UtcNow.AddHours(-hoursBack).ToString("yyyy-MM-ddTHH:mm:ss.000Z");

        return $"""
            I need you to perform a comprehensive system health diagnostic on this Windows system.
            This analysis does not require elevated permissions but provides crucial insights.
            
            **WORKFLOW:**
            
            1. **Process Analysis - Resource Usage:**
               - Use the `get_all_processes` tool to get current process snapshot
               - Identify processes with:
                 - High memory usage (>500 MB working set)
                 - High private bytes (potential memory leaks)
                 - High virtual memory (address space issues)
                 - Excessive thread count (>100 threads per process)
                 - High handle count (>1000 handles - resource leaks)
                 - High page file usage
               - Note the top 10 memory consumers
               - Flag any processes showing signs of resource leaks
            
            2. **Application Stability - Recent Crashes:**
               - Use `create_event_log_snapshot` with:
                 - logName: "Application"
                 - xPathQuery: "*[System/Level=2 and System/TimeCreated[@SystemTime >= '{timestamp}']]"
               - Use `read_resource` with pagination (limit=20, offset=0)
               - Look for:
                 - Application crashes (Event ID 1000, 1001)
                 - Application hangs (Event ID 1002)
                 - .NET runtime errors (Event ID 1026)
                 - Windows Error Reporting events
               - Correlate crashed applications with current running processes
               - Identify recurring crash patterns
            
            3. **System Warnings - Performance Issues:**
               - Use `create_event_log_snapshot` with:
                 - logName: "System"
                 - xPathQuery: "*[System/Level=3 and System/TimeCreated[@SystemTime >= '{timestamp}']]"
               - Use `read_resource` with pagination
               - Look for:
                 - Disk warnings (Event ID 51, 153)
                 - Memory warnings (Event ID 2004, 2019)
                 - Service control warnings (Event ID 7000, 7009, 7031)
                 - Time service warnings (system clock issues)
                 - Driver warnings
               - Note any patterns or recurring warnings
            
            4. **Process Health Indicators:**
               - For key processes, analyze:
                 - **Memory patterns**: Is memory usage stable or growing?
                 - **Handle leaks**: Processes with >5000 handles
                 - **Thread count**: Normal ranges vs. excessive threading
                 - **Responsive state**: Check for "Not Responding" indicators
                 - **CPU time**: Total CPU time vs. start time (efficiency)
               - Identify processes that might benefit from restart
            
            5. **Correlation Analysis:**
               - Match event log errors with process names
               - Identify if crashed applications are still running (zombie processes)
               - Find processes that have frequent warnings in event logs
               - Determine if issues are:
                 - Isolated to specific applications
                 - System-wide (affecting multiple processes)
                 - Time-based patterns (specific times of day)
            
            6. **System Health Report:**
               - **Overall Health Score**: Good/Fair/Poor/Critical
               - **Memory Status**: 
                 - Total system memory usage
                 - Top memory consumers
                 - Available memory vs. committed memory
                 - Page file usage trends
               - **Stability Issues**:
                 - Recent crashes (count and applications)
                 - Recurring error patterns
                 - Problematic processes
               - **Performance Concerns**:
                 - Potential memory leaks (growing process memory)
                 - Resource exhaustion (handles, threads)
                 - Disk I/O bottlenecks
                 - Service failures
               - **Recommendations**:
                 - Processes to restart
                 - Applications to update or reinstall
                 - System maintenance needed
                 - Resource monitoring to continue
            
            **ANALYSIS TIPS:**
            - Use pagination for large event logs (start with 20 events)
            - Focus on patterns, not individual events
            - Prioritize actionable findings
            - Group related issues together
            - Provide context for non-technical users
            
            **COMMON PATTERNS TO IDENTIFY:**
            - Memory leak: Process memory continuously growing
            - Handle leak: Handle count continuously growing
            - Crash loop: Same application crashing repeatedly
            - Service instability: Services stopping and restarting
            - Disk thrashing: High page file usage + disk warnings
            - Thread explosion: Process creating excessive threads
            
            Please begin by getting the current process information.
            """;
    }
}
