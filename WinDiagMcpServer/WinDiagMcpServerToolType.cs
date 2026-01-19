using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace WinDiagMcpServer;

/// <summary>
/// MCP server tool type for Windows diagnostics operations.
/// </summary>
[McpServerToolType]
public partial class WinDiagMcpServerToolType
{
    /// <summary>
    /// Returns basic system information for diagnostics (machine name, OS, processors, framework).
    /// </summary>
    /// <returns>A <see cref="SystemInfoResult"/> containing system diagnostic information.</returns>
    [McpServerTool]
    [Description("Returns basic system information for diagnostics (machine name, OS, processors, framework).")]
    public partial SystemInfoResult GetSystemInfo()
    {
        try
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
        catch (Exception ex)
        {
            throw new McpException("Failed to retrieve system information.", ex);
        }
    }

    [McpServerTool]
    [Description("Get the list of processes. Foreach process: Name and Process Id")]
    public partial List<BasicProcessInfo> GetProcessList()
    {
        try
        {
            return Process.GetProcesses()
                .Select(p => new BasicProcessInfo { Name = p.ProcessName, Id = p.Id })
                .ToList();
        }
        catch (Exception ex)
        {
            throw new McpException("Failed to retrieve process list.", ex);
        }
    }

    [McpServerTool]
    [Description("Retrieves detailed information about processes with a specific name. Note that multiple processes can have the same name.")]
    public ProcessesInfoResult GetProcessByName(
        [Description("The name of the process to retrieve information about.")] string processName,
        [Description("Optional: The page number for pagination.")] int? pageNumber = null,
        [Description("Optional: The number of process entries to include per page.")] int? pageSize = null)
    {
        try
        {
            var simpleProcessName = Path.GetFileNameWithoutExtension(processName);
            return GetProcesses(() => Process.GetProcessesByName(simpleProcessName), pageNumber, pageSize);
        }
        catch (Exception ex)
        {
            throw new McpException($"Failed to retrieve process by name '{processName}'.", ex);
        }
    }

    [McpServerTool]
    [Description("Retrieves detailed information about a single process identified by its unique process ID.")]
    public ProcessInfoResult GetProcessById(
        [Description("The unique identifier of the process to retrieve information about.")] int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return new ProcessInfoResult
            {
                Process = GetProcessInfo(process)
            };
        }
        catch (ArgumentException ex)
        {
            throw new McpException($"No process found with the ID '{processId}'.", ex);
        }
        catch (Exception ex)
        {
            throw new McpException($"Error retrieving process information for ID '{processId}'.", ex);
        }
    }

    /// <summary>
    /// Gets the system uptime based on the tick count.
    /// </summary>
    /// <returns>A <see cref="TimeSpan"/> representing how long the system has been running.</returns>
    private static TimeSpan GetSystemUptime()
    {
        long milliseconds = Environment.TickCount64;
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private ProcessesInfoResult GetProcesses(Func<Process[]> getProcessesFunc, int? pageNumber = null, int? pageSize = null)
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

        int actualPageSize = pageSize.Value;
        int actualPageNumber = pageNumber.Value;

        result.PageNumber = actualPageNumber;
        result.PageSize = actualPageSize;

        try
        {
            var processes = getProcessesFunc();
            result.TotalCount = processes.Length;
            int startIndex = (actualPageNumber - 1) * actualPageSize;
            int endIndex = Math.Min(startIndex + actualPageSize, processes.Length);

            for (int i = startIndex; i < endIndex; i++)
            {
                var processInfo = GetProcessInfo(processes[i]);
                result.Processes.Add(processInfo);
            }

            result.HasMore = endIndex < processes.Length;
        }
        catch (Exception ex)
        {
            throw new McpException($"Failed to retrieve processes: {ex.Message}", ex);
        }

        return result;
    }

    private ProcessInfo GetProcessInfo(Process process)
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
        int parentProcessId = 0;
        IntPtr snapshotHandle = Win32Api.CreateToolhelp32Snapshot(Win32Api.SnapshotOptions.Process, 0);
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
