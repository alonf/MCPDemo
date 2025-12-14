namespace WinDiagMcpServer.Infrastructure;

/// <summary>
/// Provides console UI rendering for the WinDiag MCP Server.
/// </summary>
public static class ConsoleUi
{
    /// <summary>
    /// Renders the application banner to stderr.
    /// </summary>
    public static void RenderBanner()
    {
        // Important: everything goes to stderr, not stdout
        var originalForeground = Console.ForegroundColor;
        var originalBackground = Console.BackgroundColor;

        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Error.WriteLine("┌───────────────────────────────────────────────┐");
            Console.Error.WriteLine("│         WinDiag MCP Server (Windows)          │");
            Console.Error.WriteLine("└───────────────────────────────────────────────┘");

            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Error.Write(" Status: ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Error.WriteLine("Ready, waiting for MCP client over HTTP (Streamable)");

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Error.WriteLine(" Endpoint: http://localhost:5000/mcp");
            Console.Error.WriteLine($" PID    : {Environment.ProcessId}");
            Console.Error.WriteLine($" Machine: {Environment.MachineName}");
            Console.Error.WriteLine();
        }
        finally
        {
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }
    }
}
