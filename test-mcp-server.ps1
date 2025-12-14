# Simple MCP Server Testing Script
# Tests your MCP server without requiring LLM integration

# Ensure Unicode output works reliably in Windows terminals
$OutputEncoding = [System.Text.UTF8Encoding]::new()
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

# Help Python-based CLIs produce UTF-8 output reliably on Windows
$env:PYTHONUTF8 = "1"
$env:PYTHONIOENCODING = "utf-8"

Set-Location $PSScriptRoot

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "WinDiag MCP Server - Quick Test" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK not found in PATH" -ForegroundColor Red
    Write-Host "Please install .NET SDK from https://dot.net" -ForegroundColor Yellow
    pause
    exit 1
}

function Invoke-McpRequest {
    param(
        [Parameter(Mandatory=$true)][string]$Endpoint,
        [Parameter(Mandatory=$true)][string]$ApiKey,
        [Parameter(Mandatory=$true)][string]$Method,
        [Parameter(Mandatory=$false)][object]$Params,
        [Parameter(Mandatory=$false)][Nullable[int]]$Id,
        [Parameter(Mandatory=$false)][string]$SessionId
    )

    $payload = [ordered]@{
        jsonrpc = "2.0"
        method  = $Method
    }

    if ($null -ne $Id) {
        $payload.id = $Id
    }
    if ($null -ne $Params) {
        $payload.params = $Params
    }

    $headers = @{
        "X-API-Key" = $ApiKey
        "Accept"    = "application/json, text/event-stream"
    }
    if ($SessionId) {
        $headers["mcp-session-id"] = $SessionId
    }

    $json = $payload | ConvertTo-Json -Depth 20
    return Invoke-WebRequest -Uri $Endpoint -Method Post -Headers $headers -ContentType "application/json" -Body $json -TimeoutSec 15
}

function Get-McpJsonContent {
    param(
        [Parameter(Mandatory=$true)][string]$Content
    )

    $trimmed = $Content.Trim()
    if ($trimmed.StartsWith("{")) {
        return $trimmed
    }

    # Streamable HTTP responses may come back as SSE (text/event-stream)
    # Format:
    #   event: message
    #   data: { ...json... }
    $lines = $trimmed -split "`r?`n"
    $dataLines = @($lines | Where-Object { $_ -like "data:*" })
    if ($dataLines.Count -eq 0) {
        return $trimmed
    }

    $jsonText = ($dataLines | ForEach-Object { $_.Substring(5).TrimStart() }) -join ""
    return $jsonText.Trim()
}

# Test 1: Build
Write-Host "[1/4] Building server..." -ForegroundColor White
$buildResult = dotnet build --nologo --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build failed" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    pause
    exit 1
}
Write-Host "      Build successful!" -ForegroundColor Green
Write-Host ""

# Test 2: Connectivity
Write-Host "[2/4] Starting server and testing connectivity..." -ForegroundColor White

# Start server in background (use built exe for faster, more reliable startup)
$serverExe = Join-Path $PSScriptRoot "WinDiagMcpServer\bin\Debug\net10.0-windows\WinDiagMcpServer.exe"
if (-not (Test-Path $serverExe)) {
    Write-Host "      Server executable not found at: $serverExe" -ForegroundColor Yellow
    Write-Host "      Falling back to dotnet run..." -ForegroundColor DarkGray
    $serverProcess = Start-Process -FilePath "dotnet" -ArgumentList "run", "--project", "WinDiagMcpServer/WinDiagMcpServer.csproj", "--", "--urls=http://localhost:5000" -PassThru -WindowStyle Minimized
} else {
    $serverProcess = Start-Process -FilePath $serverExe -ArgumentList "--urls=http://localhost:5000" -PassThru -WindowStyle Minimized
}

Write-Host "      Waiting for server to initialize..." -ForegroundColor Gray
Start-Sleep -Seconds 2

try {
    $endpoint = "http://localhost:5000/mcp"
    $apiKey = "secure-mcp-key"

    Write-Host "      Waiting 5 seconds before MCP call..." -ForegroundColor Gray
    Start-Sleep -Seconds 5

    $sessionId = $null
    $initializeResponseContent = $null
    $protocolVersionsToTry = @("2024-11-05", "2024-10-07")

    foreach ($protocolVersion in $protocolVersionsToTry) {
        try {
            $initParams = @{
                protocolVersion = $protocolVersion
                capabilities    = @{}
                clientInfo      = @{
                    name    = "windiag-mcp-smoke-test"
                    version = "1.0"
                }
            }

            $initResponse = Invoke-McpRequest -Endpoint $endpoint -ApiKey $apiKey -Method "initialize" -Params $initParams -Id 1
            $initializeResponseContent = $initResponse.Content
            $rawSessionId = $initResponse.Headers["Mcp-Session-Id"]
            if ($rawSessionId) {
                if ($rawSessionId -is [string]) {
                    $sessionId = $rawSessionId
                } else {
                    $sessionId = [string](@($rawSessionId) | Select-Object -First 1)
                }
            }
            if ($sessionId) {
                break
            }
        }
        catch {
            # Try next protocol version
        }
    }

    if (-not $sessionId) {
        if ($initializeResponseContent) {
            Write-Host $initializeResponseContent -ForegroundColor Red
        }
        throw "Failed to initialize MCP session (missing mcp-session-id)"
    }

    # Notify initialized
    [void](Invoke-McpRequest -Endpoint $endpoint -ApiKey $apiKey -Method "notifications/initialized" -Params @{} -Id $null -SessionId $sessionId)

    Write-Host "      Server is responding!" -ForegroundColor Green
    Write-Host ""

    # Test 3: Tool Discovery
    Write-Host "[3/4] Discovering tools..." -ForegroundColor White
    $toolsResponse = Invoke-McpRequest -Endpoint $endpoint -ApiKey $apiKey -Method "tools/list" -Params @{} -Id 2 -SessionId $sessionId
    $toolsJsonText = Get-McpJsonContent -Content $toolsResponse.Content
    $toolsJson = $toolsJsonText | ConvertFrom-Json
    $tools = @($toolsJson.result.tools)

    if (-not ($tools | Where-Object { $_.name -eq "get_system_info" })) {
        Write-Host "      get_system_info tool not found" -ForegroundColor Red
        throw "Tool listing failed"
    }

    Write-Host "      Tools discovered successfully!" -ForegroundColor Green
    Write-Host ""

    # Test 4: Tool Execution
    Write-Host "[4/4] Executing get_system_info tool..." -ForegroundColor White
    Write-Host ""
    $callParams = @{
        name      = "get_system_info"
        arguments = @{}
    }
    $toolResponse = Invoke-McpRequest -Endpoint $endpoint -ApiKey $apiKey -Method "tools/call" -Params $callParams -Id 3 -SessionId $sessionId
    $cmdResult = Get-McpJsonContent -Content $toolResponse.Content

    $toolCallResponse = $null
    try {
        $toolCallResponse = $cmdResult | ConvertFrom-Json
    }
    catch {
        $toolCallResponse = $null
    }

    # Parse and display the result
    if ($toolCallResponse -and $null -eq $toolCallResponse.error -and $null -ne $toolCallResponse.result) {
        Write-Host $cmdResult -ForegroundColor Gray
        Write-Host ""
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host "All tests passed!" -ForegroundColor Green
        Write-Host "============================================" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Your MCP server is working correctly." -ForegroundColor Green
        Write-Host ""
        Write-Host "Next steps:" -ForegroundColor White
        Write-Host "  - Use MCP Inspector for interactive testing" -ForegroundColor Gray
        Write-Host "    .\launch-inspector.ps1" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "  - Use the AI Chat Client" -ForegroundColor Gray
        Write-Host "    cd WinDiagMcpChat; dotnet run" -ForegroundColor DarkGray
        Write-Host ""
        Write-Host "  - See docs\TESTING.md for details" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Host "      Tool execution failed" -ForegroundColor Red
        Write-Host $cmdResult -ForegroundColor Red
        throw "Tool execution failed"
    }
}
catch {
    Write-Host "Test failed: $_" -ForegroundColor Red
    exit 1
}
finally {
    if ($serverProcess -and -not $serverProcess.HasExited) {
        Write-Host "Stopping MCP Server..." -ForegroundColor Yellow
        Stop-Process -Id $serverProcess.Id -Force
    }
}

pause
