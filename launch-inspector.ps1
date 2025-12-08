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

# Set environment variable to disable authentication for local development
$env:DANGEROUSLY_OMIT_AUTH = "true"

# Use forward slashes which work on Windows and avoid escaping issues
# Convert backslashes to forward slashes for cross-platform compatibility
$projectPath = "WinDiagMcpServer/WinDiagMcpServer.csproj"

Write-Host "Project path: $projectPath" -ForegroundColor DarkGray
Write-Host ""

# Launch inspector with STDIO transport
# Use -- to separate inspector options from the command to run
mcp-inspector -- dotnet run --project $projectPath
