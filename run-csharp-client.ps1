# Run the C# MCP Client
# This demonstrates a .NET client connecting to the MCP server

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "WinDiag MCP Client - C# Demo" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is available
if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Host "ERROR: .NET SDK not found in PATH" -ForegroundColor Red
    Write-Host "Please install .NET SDK from https://dot.net" -ForegroundColor Yellow
    pause
    exit 1
}

# Build the client
Write-Host "Building C# MCP Client..." -ForegroundColor White
$buildResult = dotnet build WinDiagMcpClient\WinDiagMcpClient.csproj --nologo --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Build failed" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    Write-Host ""
    pause
    exit 1
}
Write-Host "Build successful!" -ForegroundColor Green
Write-Host ""

# Run the client
Write-Host "Running C# MCP Client..." -ForegroundColor White
Write-Host ""
dotnet run --project WinDiagMcpClient\WinDiagMcpClient.csproj --no-build

Write-Host ""
pause
