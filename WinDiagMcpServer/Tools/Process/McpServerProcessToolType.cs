using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using static ModelContextProtocol.Protocol.ElicitRequestParams;
using DiagnosticsProcess = System.Diagnostics.Process;

namespace WinDiagMcpServer.Tools.Process;

/// <summary>
/// MCP server tool type for Windows diagnostics operations.
/// </summary>
// ReSharper disable UnusedMember.Global
[McpServerToolType]
public class McpServerProcessToolType(ILogger<McpServerProcessToolType> logger)
{
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
        return DiagnosticsProcess.GetProcesses()
            .Select(p => new BasicProcessInfo { Name = p.ProcessName, Id = p.Id })
            .ToList();
    }

    [McpServerTool]
    [Description("Retrieves detailed information about processes with a specific name. Note that multiple processes can have the same name.")]
    public ProcessesInfoResult GetProcessByName(
        [Description("The name of the process to retrieve information about.")] string processName,
        [Description("Optional: The page number for pagination.")] int? pageNumber = null,
        [Description("Optional: The number of process entries to include per page.")] int? pageSize = null)
    {
        var simpleProcessName = Path.GetFileNameWithoutExtension(processName);
        return GetProcesses(() => DiagnosticsProcess.GetProcessesByName(simpleProcessName), pageNumber, pageSize);
    }

    [McpServerTool]
    [Description("Retrieves detailed information about a single process identified by its unique process ID.")]
    public ProcessInfoResult GetProcessById(
        [Description("The unique identifier of the process to retrieve information about.")] int processId)
    {
        var result = new ProcessInfoResult();
        try
        {
            var process = DiagnosticsProcess.GetProcessById(processId);
            result.Process = GetProcessInfo(process);
        }
        catch (ArgumentException)
        {
            result.HasError = true;
            result.ErrorMessage = $"No process found with the ID '{processId}'.";
            result.HttpStatusCode = 404;
        }
        catch (Exception ex)
        {
            result.HasError = true;
            result.ErrorMessage = $"Error retrieving process information for ID '{processId}': {ex.Message}";
            result.HttpStatusCode = 500;
        }

        return result;
    }

    [McpServerTool]
    [Description("Terminates a running process after explicit confirmation from the user.")]
    public async Task<KillProcessResult> KillProcessAsync(
        McpServer server,
        [Description("The process ID to terminate. If omitted, you will be asked to choose a process.")] int? processId = null,
        [Description("Optional reason for terminating the process (for auditing in the response).")] string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (server.ClientCapabilities?.Elicitation?.Form is null)
        {
            throw new McpException(
                "Client does not support elicitation. A client that can fulfill form elicitation is required for killProcess.");
        }

        ProcessCandidate? processDetails;

        if (processId is null)
        {
            processDetails = await ElicitProcessSelectionAsync(server, cancellationToken);
            if (processDetails is null)
            {
                return KillProcessResult.Cancelled("Process selection was cancelled by the user.");
            }
        }
        else
        {
            processDetails = GetProcessCandidateById(processId.Value);
            if (processDetails is null)
            {
                return KillProcessResult.NotFound(processId.Value);
            }
        }

        var confirmed = await ElicitConfirmationAsync(server, processDetails, cancellationToken);
        if (!confirmed)
        {
            return KillProcessResult.Cancelled("User declined to confirm the termination phrase.");
        }

        try
        {
            using var process = DiagnosticsProcess.GetProcessById(processDetails.ProcessId);
            var processName = process.ProcessName;
            logger.LogWarning(
                "Terminating process {ProcessName} (PID {Pid}). Reason: {Reason}",
                processName,
                processDetails.ProcessId,
                string.IsNullOrWhiteSpace(reason) ? "not provided" : reason);

            process.Kill(true);
            await WaitForExitAsync(process, TimeSpan.FromSeconds(5), cancellationToken);

            return KillProcessResult.Success(processDetails.ProcessId, processName, reason);
        }
        catch (ArgumentException)
        {
            return KillProcessResult.NotFound(processDetails.ProcessId);
        }
        catch (Win32Exception ex)
        {
            return KillProcessResult.Failed(processDetails.ProcessId, processDetails.ProcessName, ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return KillProcessResult.Failed(processDetails.ProcessId, processDetails.ProcessName, ex.Message);
        }
    }

    private static TimeSpan GetSystemUptime()
    {
        var milliseconds = Environment.TickCount64;
        return TimeSpan.FromMilliseconds(milliseconds);
    }

    private static async Task WaitForExitAsync(DiagnosticsProcess process, TimeSpan timeout, CancellationToken cancellationToken)
    {
        var start = DateTime.UtcNow;
        while (!process.HasExited && DateTime.UtcNow - start < timeout)
        {
            await Task.Delay(100, cancellationToken);
        }
    }

    private static string FormatCandidate(ProcessCandidate candidate)
    {
        return $"{candidate.ProcessName} (PID {candidate.ProcessId}) • CPU {candidate.CpuPercent:F1}% • RAM {FormatBytes(candidate.WorkingSet)}";
    }

    private static string FormatBytes(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.0} {sizes[order]}";
    }

    private static ProcessCandidate? GetProcessCandidateById(int processId)
    {
        try
        {
            using var process = DiagnosticsProcess.GetProcessById(processId);
            return new ProcessCandidate(
                process.Id,
                process.ProcessName,
                process.WorkingSet64,
                0);
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static async Task<List<ProcessCandidate>> SampleTopCpuProcessesAsync(int take, CancellationToken cancellationToken)
    {
        const int sampleMilliseconds = 750;
        var initial = CaptureSnapshot();

        try
        {
            await Task.Delay(sampleMilliseconds, cancellationToken);
        }
        catch (TaskCanceledException)
        {
            return [];
        }

        var later = CaptureSnapshot();
        var interval = TimeSpan.FromMilliseconds(sampleMilliseconds);
        var processorCount = Math.Max(1, Environment.ProcessorCount);

        var candidates = later.Values
            .Where(sample => initial.TryGetValue(sample.ProcessId, out _))
            .Select(sample =>
            {
                var previous = initial[sample.ProcessId];
                var cpuDelta = sample.TotalProcessorTime - previous.TotalProcessorTime;
                var cpuPercent = cpuDelta.TotalMilliseconds <= 0
                    ? 0
                    : cpuDelta.TotalMilliseconds / (interval.TotalMilliseconds * processorCount) * 100;
                return new ProcessCandidate(
                    sample.ProcessId,
                    sample.ProcessName,
                    sample.WorkingSet,
                    Math.Round(cpuPercent, 1));
            })
            .Where(candidate => candidate.WorkingSet > 0)
            .OrderByDescending(candidate => candidate.CpuPercent)
            .ThenByDescending(candidate => candidate.WorkingSet)
            .Take(take)
            .ToList();

        if (candidates.Count == 0)
        {
            candidates = later.Values
                .Select(sample => new ProcessCandidate(sample.ProcessId, sample.ProcessName, sample.WorkingSet, 0))
                .OrderByDescending(candidate => candidate.WorkingSet)
                .Take(take)
                .ToList();
        }

        return candidates;

        static Dictionary<int, ProcessSnapshot> CaptureSnapshot()
        {
            var snapshot = new Dictionary<int, ProcessSnapshot>();

            foreach (var process in DiagnosticsProcess.GetProcesses())
            {
                try
                {
                    snapshot[process.Id] = new ProcessSnapshot(
                        process.Id,
                        process.ProcessName,
                        process.WorkingSet64,
                        process.TotalProcessorTime);
                }
                catch (Exception)
                {
                    // ignore processes that exit mid-snapshot
                }
                finally
                {
                    process.Dispose();
                }
            }

            return snapshot;
        }
    }

    private ProcessesInfoResult GetProcesses(Func<DiagnosticsProcess[]> getProcessesFunc, int? pageNumber = null, int? pageSize = null)
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

    private async Task<bool> ElicitConfirmationAsync(McpServer server, ProcessCandidate process, CancellationToken cancellationToken)
    {
        var confirmationPhrase = $"CONFIRM PID {process.ProcessId}";

        var schema = new RequestSchema
        {
            Properties =
            {
                ["confirmation"] = new StringSchema
                {
                    Title = "Confirmation Phrase",
                    Description = $"Type '{confirmationPhrase}' to confirm termination.",
                    MinLength = confirmationPhrase.Length
                }
            }
        };

        var response = await server.ElicitAsync(
            new ElicitRequestParams
            {
                Message = $"You are about to terminate {process.ProcessName} (PID {process.ProcessId}). This cannot be undone.",
                RequestedSchema = schema
            },
            cancellationToken);

        var provided = response.Content is not null &&
                       response.Content.TryGetValue("confirmation", out var entry) &&
                       entry.ValueKind == JsonValueKind.String
            ? entry.GetString()
            : null;

        return response.Action == "accept" && string.Equals(provided, confirmationPhrase, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<ProcessCandidate?> ElicitProcessSelectionAsync(McpServer server, CancellationToken cancellationToken)
    {
        var candidates = await SampleTopCpuProcessesAsync(5, cancellationToken);
        if (candidates.Count == 0)
        {
            throw new McpException("Unable to locate any running processes. Try again in a moment.");
        }

        var schema = new RequestSchema
        {
            Properties =
            {
                ["process"] = new TitledSingleSelectEnumSchema
                {
                    Title = "Process",
                    Description = "Select the process you want to terminate.",
                    OneOf = candidates
                        .Select(candidate => new EnumSchemaOption
                        {
                            Const = candidate.ProcessId.ToString(CultureInfo.InvariantCulture),
                            Title = FormatCandidate(candidate)
                        })
                        .ToArray()
                }
            }
        };

        var response = await server.ElicitAsync(
            new ElicitRequestParams
            {
                Message = "Select one of the top CPU consumers to terminate. Only a handful are shown for safety.",
                RequestedSchema = schema
            },
            cancellationToken);

        if (response.Action != "accept" || response.Content is null ||
            !response.Content.TryGetValue("process", out var selectedElement) || selectedElement.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        if (!int.TryParse(selectedElement.GetString(), out var pid))
        {
            return null;
        }

        return candidates.FirstOrDefault(c => c.ProcessId == pid) ?? GetProcessCandidateById(pid);
    }

    private sealed record ProcessCandidate(int ProcessId, string ProcessName, long WorkingSet, double CpuPercent);

    private sealed record ProcessSnapshot(int ProcessId, string ProcessName, long WorkingSet, TimeSpan TotalProcessorTime);
}
