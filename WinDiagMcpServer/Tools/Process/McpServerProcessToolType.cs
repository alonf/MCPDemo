using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using DiagnosticsProcess = System.Diagnostics.Process;

namespace WinDiagMcpServer.Tools.Process;

/// <summary>
/// MCP server tool type for Windows diagnostics operations.
/// </summary>
// ReSharper disable UnusedMember.Global
[McpServerToolType]
public partial class McpServerProcessToolType
{
    private readonly ILogger<McpServerProcessToolType> _logger;

    public McpServerProcessToolType(ILogger<McpServerProcessToolType> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Returns basic system information for diagnostics (machine name, OS, processors, framework).
    /// </summary>
    /// <returns>A <see cref="SystemInfoResult"/> containing system diagnostic information.</returns>
    [McpServerTool]
    [Description("Returns basic system information for diagnostics (machine name, OS, processors, framework).")]
    public SystemInfoResult GetSystemInfo()
    {
        return new SystemInfoResult
        {
            MachineName = Environment.MachineName,
            UserName = Environment.UserName,
            OSDescription = RuntimeInformation.OSDescription,
            OSArchitecture = RuntimeInformation.OSArchitecture.ToString(),
            ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
            ProcessorCount = Environment.ProcessorCount,
            FrameworkDescription = RuntimeInformation.FrameworkDescription,
            CurrentDirectory = Environment.CurrentDirectory,
            SystemUpTime = GetSystemUptime()
        };
    }

    [McpServerTool]
    [Description("Get the list of processes. Foreach process: Name and Process Id")]
    public List<BasicProcessInfo> GetProcessList()
    {
        try
        {
            _logger.LogDebug("Retrieving process list");
            var result = DiagnosticsProcess.GetProcesses()
                .Select(p => new BasicProcessInfo { Name = p.ProcessName, Id = p.Id })
                .ToList();
            _logger.LogInformation("Successfully retrieved process list ({Count} processes)", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve process list");
            throw ex.ToMcpException("Failed to retrieve process list");
        }
    }

    [McpServerTool]
    [Description("Retrieves detailed information about processes with a specific name. Note that multiple processes can have the same name.")]
    public ProcessesInfoResult GetProcessByName(
        [Description("The name of the process to retrieve information about.")] string processName,
        [Description("Optional: The page number for pagination.")] int? pageNumber = null,
        [Description("Optional: The number of process entries to include per page.")] int? pageSize = null)
    {
        var simpleProcessName = Path.GetFileNameWithoutExtension(processName);
        return GetProcesses(() => DiagnosticsProcess.GetProcessesByName(simpleProcessName), pageNumber, pageSize, $"name '{processName}'");
    }

    [McpServerTool]
    [Description("Retrieves detailed information about a single process identified by its unique process ID.")]
    public ProcessInfo GetProcessById(
        [Description("The unique identifier of the process to retrieve information about.")] int processId)
    {
        try
        {
            _logger.LogDebug("Retrieving process by ID {ProcessId}", processId);
            var process = DiagnosticsProcess.GetProcessById(processId);
            var result = GetProcessInfo(process);

            _logger.LogInformation("Successfully retrieved process info for ID {ProcessId}", processId);
            return result;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Process ID {ProcessId} not found", processId);
            throw ex.ToMcpException($"No process found with the ID '{processId}'");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving process info for ID {ProcessId}", processId);
            throw ex.ToMcpException($"Error retrieving process information for ID '{processId}'");
        }
    }

    /// <summary>
    /// Gets the system uptime based on the tick count.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing how long the system has been running.</returns>
    private static TimeSpan GetSystemUptime()
    {
        var milliseconds = Environment.TickCount64;
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private ProcessesInfoResult GetProcesses(Func<DiagnosticsProcess[]> getProcessesFunc, int? pageNumber, int? pageSize, string context = "processes")
    {
        var result = new ProcessesInfoResult();

        if (pageSize is null or < 1)
        {
            pageSize = 5;
        }

        if (pageNumber is null or < 1)
        {
            pageNumber = 1;
        }

        var actualPageSize = pageSize.Value;
        var actualPageNumber = pageNumber.Value;

        result.PageNumber = actualPageNumber;
        result.PageSize = actualPageSize;

        try
        {
            var processes = getProcessesFunc();
            result.TotalCount = processes.Length;
            var startIndex = (actualPageNumber - 1) * actualPageSize;
            var endIndex = Math.Min(startIndex + actualPageSize, processes.Length);

            for (var i = startIndex; i < endIndex; i++)
            {
                var processInfo = GetProcessInfo(processes[i]);
                result.Processes.Add(processInfo);
            }

            result.HasMore = endIndex < processes.Length;
            result.HttpStatusCode = result.HasMore ? 206 : 200;
        }
        catch (Exception ex)
        {
            result.HasError = true;
            result.ErrorMessage = $"Error retrieving processes: {ex.Message}";
            result.HttpStatusCode = 500;
        }

        return result;
    }

    private ProcessInfo GetProcessInfo(DiagnosticsProcess process)
    {
        try
        {
            var guiResourceCounts = Win32Api.GetGuiResourcesCounts(process);
            var processInfo = new ProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName,
                WorkingSetMemory = process.WorkingSet64,
                MainWindowTitle = process.MainWindowTitle,
                BasePriority = process.BasePriority,
                HandleCount = process.HandleCount,
                UserObjectCount = guiResourceCounts.UserObjects,
                GdiObjectCount = guiResourceCounts.GdiObjects,
                ThreadCount = process.Threads.Count,
                StartTime = GetStartTime() ?? DateTime.MinValue,
                TotalProcessorTime = GetTotalProcessorTime() ?? TimeSpan.Zero,
                ParentProcessId = GetParentProcessId(process.Id),
                MemoryUsage = new MemoryUsage
                {
                    WorkingSet = process.WorkingSet64,
                    PrivateBytes = process.PrivateMemorySize64,
                    VirtualMemorySize = process.VirtualMemorySize64,
                    PagedMemorySize = process.PagedMemorySize64,
                    PagedSystemMemory = process.PagedSystemMemorySize64,
                    NonPagedSystemMemory = process.NonpagedSystemMemorySize64,
                    PeakPagedMemorySize = process.PeakPagedMemorySize64,
                    PeakVirtualMemorySize = process.PeakVirtualMemorySize64,
                    PeakWorkingSet = process.PeakWorkingSet64
                }
            };
            return processInfo;

            DateTime? GetStartTime()
            {
                try
                {
                    return process.StartTime;
                }
                catch (Exception)
                {
                    return null;
                }
            }

            TimeSpan? GetTotalProcessorTime()
            {
                try
                {
                    return process.TotalProcessorTime;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }
        catch (Exception)
        {
            // fallback on unknown exception
            return new ProcessInfo
            {
                ProcessId = process.Id,
                ProcessName = process.ProcessName
            };
        }
    }

    private int GetParentProcessId(int processId)
    {
        var parentProcessId = 0;
        var snapshotHandle = Win32Api.CreateToolhelp32Snapshot(Win32Api.SnapshotOptions.Process, 0);
        if (snapshotHandle != IntPtr.Zero)
        {
            var processEntry = new Win32Api.ProcessEntry32
            {
                DwSize = (uint)Marshal.SizeOf(typeof(Win32Api.ProcessEntry32))
            };

            if (Win32Api.Process32First(snapshotHandle, ref processEntry))
            {
                do
                {
                    if (processEntry.Th32ProcessId == (uint)processId)
                    {
                        parentProcessId = (int)processEntry.Th32ParentProcessId;
                        break;
                    }
                }
                while (Win32Api.Process32Next(snapshotHandle, ref processEntry));
            }

            Win32Api.CloseHandle(snapshotHandle);
        }

        return parentProcessId;
    }
}
