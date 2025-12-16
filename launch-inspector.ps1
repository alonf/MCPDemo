# Launch MCP Inspector with WinDiag MCP Server
# This provides a visual interface for testing your MCP server

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "MCP Inspector - Visual MCP Server Testing" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is available
if (-not (Get-Command node -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: Node.js not found" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Node.js from https://nodejs.org" -ForegroundColor Yellow
    Write-Host "Then run: npm install -g @modelcontextprotocol/inspector" -ForegroundColor Yellow
    Write-Host ""
    pause
    exit 1
}

# Check if mcp-inspector is installed
if (-not (Get-Command mcp-inspector -ErrorAction SilentlyContinue)) {
    Write-Host "MCP Inspector not found. Installing..." -ForegroundColor Yellow
    Write-Host ""
    
    $npmResult = npm install -g @modelcontextprotocol/inspector 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Failed to install MCP Inspector" -ForegroundColor Red
        Write-Host $npmResult -ForegroundColor Red
        pause
        exit 1
    }
    
    Write-Host ""
    Write-Host "Installation complete!" -ForegroundColor Green
    Write-Host ""
}

# Build the server first
Write-Host "Building WinDiag MCP Server..." -ForegroundColor White
$buildResult = dotnet build --nologo --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    Write-Host ""
    pause
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

Write-Host "Starting MCP Inspector..." -ForegroundColor White
Write-Host ""
Write-Host "This will:" -ForegroundColor Cyan
Write-Host "  1. Start your .NET MCP server (STDIO transport)" -ForegroundColor Gray
Write-Host "  2. Launch MCP Inspector web UI (no authentication)" -ForegroundColor Gray
Write-Host "  3. Open your browser" -ForegroundColor Gray
Write-Host ""
Write-Host "Once browser opens:" -ForegroundColor Yellow
Write-Host "  1. Click 'Connect' button in left panel" -ForegroundColor Gray
Write-Host "  2. Wait for connection (green dot)" -ForegroundColor Gray
Write-Host "  3. Click 'Tools' tab at top" -ForegroundColor Gray
Write-Host "  4. Click 'get_system_info'" -ForegroundColor Gray
Write-Host "  5. Click 'Call Tool' to see your system info!" -ForegroundColor Gray
Write-Host ""
Write-Host "Press Ctrl+C to stop" -ForegroundColor Yellow
Write-Host ""

# Inspector's local proxy server uses port 6277. If a previous Inspector instance is still running,
# it can leave a node.exe process listening on that port and prevent new launches.
$proxyPort = 6277
try {
    $listener = Get-NetTCPConnection -LocalPort $proxyPort -State Listen -ErrorAction Stop | Select-Object -First 1
} catch {
    $listener = $null
}

if ($null -ne $listener) {
    $owningProcessId = $listener.OwningProcess
    Write-Host "Detected port $proxyPort already in use (PID $owningProcessId)." -ForegroundColor Yellow

    $proc = $null
    try { $proc = Get-CimInstance Win32_Process -Filter "ProcessId=$owningProcessId" -ErrorAction Stop } catch { }

    $looksLikeInspector = $false
    if ($null -ne $proc -and $null -ne $proc.CommandLine) {
        if ($proc.CommandLine -match "modelcontextprotocol" -and $proc.CommandLine -match "inspector" -and $proc.CommandLine -match "index\\.js") {
            $looksLikeInspector = $true
        }
    }

    if ($looksLikeInspector) {
        Write-Host "Stopping previous MCP Inspector process..." -ForegroundColor Yellow
        try {
            Stop-Process -Id $owningProcessId -Force -ErrorAction Stop
            Start-Sleep -Milliseconds 500
        } catch {
            Write-Host "ERROR: Failed to stop the previous MCP Inspector process (PID $owningProcessId)." -ForegroundColor Red
            Write-Host "Close the existing Inspector window or run: Stop-Process -Id $owningProcessId -Force" -ForegroundColor Yellow
            pause
            exit 1
        }
    } elseif ($null -ne $proc -and $proc.Name -eq "node.exe") {
        Write-Host "Port $proxyPort is held by node.exe (PID $owningProcessId)." -ForegroundColor Yellow
        $answer = Read-Host "Stop this node.exe process and continue? (Y/N)"
        if ($answer -match '^(y|yes)$') {
            try {
                Stop-Process -Id $owningProcessId -Force -ErrorAction Stop
                Start-Sleep -Milliseconds 500
            } catch {
                Write-Host "ERROR: Failed to stop node.exe (PID $owningProcessId)." -ForegroundColor Red
                pause
                exit 1
            }
        } else {
            Write-Host "Aborted. Please free port $proxyPort and retry." -ForegroundColor Red
            pause
            exit 1
        }
    } else {
        Write-Host "Port $proxyPort is in use by a non-Inspector process (PID $owningProcessId)." -ForegroundColor Red
        Write-Host "Please free the port and retry. You can inspect it with:" -ForegroundColor Yellow
        Write-Host "  netstat -ano | findstr :$proxyPort" -ForegroundColor DarkGray
        pause
        exit 1
    }
}

# Launch inspector with STDIO transport using the local config file.
# This matches the Inspector CLI help and keeps the command line simple for users.
$configPath = "server_config.json"
$serverName = "windiag"

Write-Host "Config: $configPath" -ForegroundColor DarkGray
Write-Host "Server: $serverName" -ForegroundColor DarkGray
Write-Host ""

mcp-inspector --transport stdio --config $configPath --server $serverName
