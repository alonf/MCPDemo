using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace WinDiagMcpServer.Infrastructure;

/// <summary>
/// A custom console formatter for MCP server logging that provides color-coded output for different log levels and MCP method calls.
/// </summary>
public class McpConsoleFormatter() : ConsoleFormatter(_formatName)
{
    private const string _formatName = "mcp";
    private static readonly Regex _methodPattern = new("method '([^']+)'", RegexOptions.Compiled);

    /// <summary>
    /// Writes the log entry to the console with custom formatting.
    /// </summary>
    /// <typeparam name="TState">The type of the state object.</typeparam>
    /// <param name="logEntry">The log entry to write.</param>
    /// <param name="scopeProvider">The provider for scope information.</param>
    /// <param name="textWriter">The writer to write the output to.</param>
    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);

        // Timestamp
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        textWriter.Write($"{timestamp} ");

        // Level
        var levelColor = GetLevelColor(logEntry.LogLevel);
        textWriter.Write(levelColor);
        textWriter.Write(GetLevelString(logEntry.LogLevel));
        textWriter.Write("\x1b[0m: "); // Reset

        // Category
        textWriter.Write($"{logEntry.Category}[{logEntry.EventId}]");

        // Scopes
        scopeProvider?.ForEachScope(
            (scope, state) =>
            {
                state.Write(" => ");
                state.Write(scope);
            },
            textWriter);

        textWriter.Write(": ");

        // Highlighting method names in MCP requests
        if (logEntry.Category == "ModelContextProtocol.Server.McpServer" && message.Contains("method '"))
        {
            var match = _methodPattern.Match(message);
            if (match.Success)
            {
                var preMatch = message.Substring(0, match.Index);
                var methodPart = match.Value;
                var postMatch = message.Substring(match.Index + match.Length);

                textWriter.Write(preMatch);
                textWriter.Write("\x1b[36m"); // Cyan
                textWriter.Write(methodPart);
                textWriter.Write("\x1b[0m");
                textWriter.Write(postMatch);
                textWriter.WriteLine();
            }
            else
            {
                textWriter.WriteLine(message);
            }
        }
        else
        {
            textWriter.WriteLine(message);
        }

        if (logEntry.Exception != null)
        {
            textWriter.WriteLine(logEntry.Exception);
        }
    }

    private static string GetLevelString(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "[TRC]",
        LogLevel.Debug => "[DBG]",
        LogLevel.Information => "[INF]",
        LogLevel.Warning => "[WRN]",
        LogLevel.Error => "[ERR]",
        LogLevel.Critical => "[CRT]",
        _ => "[UNK]"
    };

    private static string GetLevelColor(LogLevel logLevel) => logLevel switch
    {
        LogLevel.Trace => "\x1b[90m", // Gray
        LogLevel.Debug => "\x1b[37m", // White
        LogLevel.Information => "\x1b[32m", // Green
        LogLevel.Warning => "\x1b[33m", // Yellow
        LogLevel.Error => "\x1b[31m", // Red
        LogLevel.Critical => "\x1b[41m\x1b[37m", // White on Red
        _ => "\x1b[39m" // Default
    };
}
