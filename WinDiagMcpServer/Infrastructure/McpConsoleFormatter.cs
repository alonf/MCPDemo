using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace WinDiagMcpServer.Infrastructure;

public class McpConsoleFormatter : ConsoleFormatter
{
    private const string FormatName = "mcp";
    private static readonly Regex MethodPattern = new(@"method '([^']+)'", RegexOptions.Compiled);

    public McpConsoleFormatter() : base(FormatName) { }

    public override void Write<TState>(in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter(logEntry.State, logEntry.Exception);
        if (message == null) return;

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
        if (scopeProvider != null)
        {
            scopeProvider.ForEachScope((scope, state) =>
            {
                state.Write(" => ");
                state.Write(scope);
            }, textWriter);
        }

        textWriter.Write(" ");

        // Message with highlighting
        var matches = MethodPattern.Matches(message);
        if (matches.Count > 0)
        {
            int lastIndex = 0;
            foreach (Match match in matches)
            {
                textWriter.Write(message.Substring(lastIndex, match.Index - lastIndex));
                textWriter.Write("method '");
                
                var methodName = match.Groups[1].Value;
                var color = GetMethodColor(methodName);
                
                textWriter.Write(color);
                textWriter.Write(methodName);
                textWriter.Write("\x1b[0m"); // Reset
                textWriter.Write("'");
                lastIndex = match.Index + match.Length;
            }
            textWriter.Write(message.Substring(lastIndex));
        }
        else
        {
            textWriter.Write(message);
        }

        textWriter.WriteLine();
    }

    private static string GetMethodColor(string methodName)
    {
        if (methodName.StartsWith("tools/", StringComparison.OrdinalIgnoreCase)) return "\x1b[35m"; // Magenta
        if (methodName.StartsWith("resources/", StringComparison.OrdinalIgnoreCase)) return "\x1b[34m"; // Blue
        if (methodName.StartsWith("prompts/", StringComparison.OrdinalIgnoreCase)) return "\x1b[36m"; // Cyan
        if (methodName.StartsWith("sampling/", StringComparison.OrdinalIgnoreCase)) return "\x1b[33m"; // Yellow
        return "\x1b[32m"; // Green (default)
    }

    private static string GetLevelColor(LogLevel level) => level switch
    {
        LogLevel.Trace => "\x1b[90m", // Gray
        LogLevel.Debug => "\x1b[90m", // Gray
        LogLevel.Information => "\x1b[32m", // Green
        LogLevel.Warning => "\x1b[33m", // Yellow
        LogLevel.Error => "\x1b[31m", // Red
        LogLevel.Critical => "\x1b[41m\x1b[37m", // White on Red
        _ => "\x1b[39m" // Default
    };

    private static string GetLevelString(LogLevel level) => level switch
    {
        LogLevel.Trace => "trce",
        LogLevel.Debug => "dbug",
        LogLevel.Information => "info",
        LogLevel.Warning => "warn",
        LogLevel.Error => "fail",
        LogLevel.Critical => "crit",
        _ => level.ToString().ToLower().Substring(0, 4)
    };
}
