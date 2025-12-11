# Namespace Update Script
# Updates all namespaces to match new folder structure

Write-Host "Updating namespaces..." -ForegroundColor Cyan

# Function to update namespace in file
function Update-Namespace {
    param (
        [string]$FilePath,
        [string]$NewNamespace
    )
    
    if (Test-Path $FilePath) {
        $content = Get-Content $FilePath -Raw
        $content = $content -replace 'namespace\s+WinDiagMcpServer\s*;', "namespace $NewNamespace;"
        Set-Content $FilePath $content -NoNewline
        Write-Host "  Updated: $FilePath" -ForegroundColor Green
    }
}

# Update Tools/SystemInfo
Update-Namespace "WinDiagMcpServer\Tools\SystemInfo\SystemInfoResult.cs" "WinDiagMcpServer.Tools.SystemInfo"

# Update Tools/Process
Update-Namespace "WinDiagMcpServer\Tools\Process\McpServerProcessToolType.cs" "WinDiagMcpServer.Tools.Process"
Update-Namespace "WinDiagMcpServer\Tools\Process\ProcessInfoResult.cs" "WinDiagMcpServer.Tools.Process"
Update-Namespace "WinDiagMcpServer\Tools\Process\ProcessInfo.cs" "WinDiagMcpServer.Tools.Process"
Update-Namespace "WinDiagMcpServer\Tools\Process\BasicProcessInfo.cs" "WinDiagMcpServer.Tools.Process"
Update-Namespace "WinDiagMcpServer\Tools\Process\ProcessesInfoResult.cs" "WinDiagMcpServer.Tools.Process"
Update-Namespace "WinDiagMcpServer\Tools\Process\MemoryUsage.cs" "WinDiagMcpServer.Tools.Process"

# Update Tools/EventLog
Update-Namespace "WinDiagMcpServer\Tools\EventLog\McpServerEventLogToolType.cs" "WinDiagMcpServer.Tools.EventLog"
Update-Namespace "WinDiagMcpServer\Tools\EventLog\EventLogSnapshotDto.cs" "WinDiagMcpServer.Tools.EventLog"
Update-Namespace "WinDiagMcpServer\Tools\EventLog\EventLogRecordDto.cs" "WinDiagMcpServer.Tools.EventLog"
Update-Namespace "WinDiagMcpServer\Tools\EventLog\EventLogSnapshotResourceInfo.cs" "WinDiagMcpServer.Tools.EventLog"

# Update Resources/EventLog
Update-Namespace "WinDiagMcpServer\Resources\EventLog\McpServerEventLogResourceType.cs" "WinDiagMcpServer.Resources.EventLog"
Update-Namespace "WinDiagMcpServer\Resources\EventLog\IEventLogSnapshotStorage.cs" "WinDiagMcpServer.Resources.EventLog"
Update-Namespace "WinDiagMcpServer\Resources\EventLog\EventLogSnapshotStorage.cs" "WinDiagMcpServer.Resources.EventLog"
Update-Namespace "WinDiagMcpServer\Resources\EventLog\EventLogSnapshotEntry.cs" "WinDiagMcpServer.Resources.EventLog"

# Update Infrastructure
Update-Namespace "WinDiagMcpServer\Infrastructure\ConsoleUi.cs" "WinDiagMcpServer.Infrastructure"
Update-Namespace "WinDiagMcpServer\Infrastructure\Win32API.cs" "WinDiagMcpServer.Infrastructure"

Write-Host ""
Write-Host "Namespaces updated successfully!" -ForegroundColor Green
