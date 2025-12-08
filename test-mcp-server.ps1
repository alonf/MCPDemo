# Simple MCP Server Testing Script
# Tests your MCP server without requiring LLM integration

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

# Check if mcp-cli is available
if (-not (Get-Command mcp-cli -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: mcp-cli not found" -ForegroundColor Red
    Write-Host "Install with: pip install mcp-cli" -ForegroundColor Yellow
    pause
    exit 1
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
Write-Host "[2/4] Testing server connectivity..." -ForegroundColor White
$pingResult = mcp-cli ping --server windiag --config-file server_config.json 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Server not responding" -ForegroundColor Red
    Write-Host "      Check server_config.json configuration" -ForegroundColor Yellow
    pause
    exit 1
}
Write-Host "      Server is responding!" -ForegroundColor Green
Write-Host ""

# Test 3: Tool Discovery
Write-Host "[3/4] Discovering tools..." -ForegroundColor White
$toolsResult = mcp-cli tools --server windiag --config-file server_config.json 2>&1
if ($toolsResult -notmatch "system_info") {
    Write-Host "      get_system_info tool not found" -ForegroundColor Red
    pause
    exit 1
}
Write-Host "      Tools discovered successfully!" -ForegroundColor Green
Write-Host ""

# Test 4: Tool Execution
Write-Host "[4/4] Executing get_system_info tool..." -ForegroundColor White
Write-Host ""
$cmdResult = mcp-cli cmd --server windiag --config-file server_config.json --tool get_system_info 2>&1

# Parse and display the result
if ($cmdResult -match '"isError"\s*:\s*false') {
    Write-Host $cmdResult -ForegroundColor Gray
    Write-Host ""
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "All tests passed! ✓" -ForegroundColor Green
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Your MCP server is working correctly." -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor White
    Write-Host "  - Use MCP Inspector for interactive testing" -ForegroundColor Gray
    Write-Host "    npm install -g @modelcontextprotocol/inspector" -ForegroundColor DarkGray
    Write-Host "    mcp-inspector dotnet run --project WinDiagMcpServer/WinDiagMcpServer.csproj" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  - Use Claude Desktop for LLM integration" -ForegroundColor Gray
    Write-Host "    https://claude.ai/download" -ForegroundColor DarkGray
    Write-Host ""
    Write-Host "  - See docs\MCP_TESTING_GUIDE.md for details" -ForegroundColor Gray
    Write-Host ""
} else {
    Write-Host "      Tool execution failed" -ForegroundColor Red
    Write-Host $cmdResult -ForegroundColor Red
    pause
    exit 1
}

pause
