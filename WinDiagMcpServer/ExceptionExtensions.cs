using System.Runtime.CompilerServices;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer;

#pragma warning disable S2325 // Make 'ToMcpException' a static method.
/// <summary>
/// Provides extension methods for exception handling.
/// </summary>
public static class ExceptionExtensions
{
    extension(Exception exception)
    {
        /// <summary>
        /// Wraps an exception into an <see cref="McpException"/> with a contextual message and caller name.
        /// </summary>
        /// <param name="message">A context message to prepend to the exception message.</param>
        /// <returns>A new <see cref="McpException"/>.</returns>
        public McpException ToMcpException(string message)
        {
            return new McpException($"{message}: {exception.Message}", exception);
        }
    }
}
