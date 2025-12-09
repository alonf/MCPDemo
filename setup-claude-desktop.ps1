# Setup Claude Desktop with WinDiag MCP Server
# This script installs (if needed) and configures Claude Desktop to use the WinDiag MCP Server

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Claude Desktop MCP Configuration" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Get the current project directory
$projectRoot = $PSScriptRoot
$projectPath = Join-Path $projectRoot "WinDiagMcpServer\WinDiagMcpServer.csproj"

Write-Host "Project Root: $projectRoot" -ForegroundColor Gray
Write-Host "Project Path: $projectPath" -ForegroundColor Gray
Write-Host ""

# Check if Claude Desktop is installed
$claudeConfigDir = Join-Path $env:APPDATA "Claude"
$claudeConfigFile = Join-Path $claudeConfigDir "claude_desktop_config.json"

Write-Host "[1/5] Checking Claude Desktop installation..." -ForegroundColor White

if (-not (Test-Path $claudeConfigDir)) {
    Write-Host "      Claude Desktop not found!" -ForegroundColor Yellow
    Write-Host ""
    
    # Check if winget is available
    if (Get-Command winget -ErrorAction SilentlyContinue) {
        Write-Host "      Attempting to install Claude Desktop using winget..." -ForegroundColor Cyan
        Write-Host ""
        
        # Try to install Claude Desktop
        $installResult = winget install --id=Anthropic.Claude --exact --accept-source-agreements --accept-package-agreements 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "      Claude Desktop installed successfully!" -ForegroundColor Green
            Write-Host "      Please restart this script to configure it." -ForegroundColor Yellow
            Write-Host ""
            pause
            exit 0
        }
        else {
            Write-Host "      Automated installation failed." -ForegroundColor Yellow
            Write-Host ""
            Write-Host "Please install Claude Desktop manually:" -ForegroundColor Yellow
            Write-Host "  Option 1 (Recommended): " -ForegroundColor White
            Write-Host "    winget install Anthropic.Claude" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "  Option 2: Download from" -ForegroundColor White
            Write-Host "    https://claude.ai/download" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "Then run this script again." -ForegroundColor Gray
            Write-Host ""
            pause
            exit 1
        }
    }
    else {
        Write-Host "      winget not available. Install manually:" -ForegroundColor Yellow
        Write-Host ""
        Write-Host "  Option 1: Install winget (Windows Package Manager)" -ForegroundColor White
        Write-Host "    https://apps.microsoft.com/detail/9nblggh4nns1" -ForegroundColor Cyan
        Write-Host "    Then run: winget install Anthropic.Claude" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  Option 2: Download Claude Desktop directly" -ForegroundColor White
        Write-Host "    https://claude.ai/download" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "Then run this script again." -ForegroundColor Gray
        Write-Host ""
        pause
        exit 1
    }
}

Write-Host "      Claude Desktop found at: $claudeConfigDir" -ForegroundColor Green
Write-Host ""

# Build the server first
Write-Host "[2/5] Building WinDiag MCP Server..." -ForegroundColor White
$buildResult = dotnet build --nologo --verbosity quiet 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "      Build failed" -ForegroundColor Red
    Write-Host $buildResult -ForegroundColor Red
    Write-Host ""
    pause
    exit 1
}
Write-Host "      Build successful!" -ForegroundColor Green
Write-Host ""

# Create or update Claude Desktop configuration
Write-Host "[3/5] Configuring Claude Desktop..." -ForegroundColor White

# Read existing config if it exists
$config = @{
    mcpServers = @{}
}

if (Test-Path $claudeConfigFile) {
    Write-Host "      Found existing configuration" -ForegroundColor Gray
    try {
        $existingConfig = Get-Content $claudeConfigFile -Raw | ConvertFrom-Json
        if ($existingConfig.mcpServers) {
            $config.mcpServers = @{}
            foreach ($property in $existingConfig.mcpServers.PSObject.Properties) {
                $config.mcpServers[$property.Name] = $property.Value
            }
        }
    }
    catch {
        Write-Host "      Warning: Could not parse existing config, creating new one" -ForegroundColor Yellow
    }
}

# Add WinDiag server configuration
# Use forward slashes for cross-platform compatibility
$projectPathUnix = $projectPath -replace '\\', '/'

$config.mcpServers["windiag"] = @{
    command = "dotnet"
    args = @("run", "--project", $projectPathUnix)
}

# Create backup of existing config
if (Test-Path $claudeConfigFile) {
    $backupFile = "$claudeConfigFile.backup-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
    Copy-Item $claudeConfigFile $backupFile -Force
    Write-Host "      Backed up existing config to: $backupFile" -ForegroundColor Gray
}

# Write new configuration
$configJson = $config | ConvertTo-Json -Depth 10
Set-Content -Path $claudeConfigFile -Value $configJson -Encoding UTF8

Write-Host "      Configuration written successfully!" -ForegroundColor Green
Write-Host ""

# Display configuration
Write-Host "[4/5] Configuration Summary:" -ForegroundColor White
Write-Host ""
Write-Host "  Config File: $claudeConfigFile" -ForegroundColor Cyan
Write-Host ""
Write-Host "  MCP Server Configuration:" -ForegroundColor Cyan
Write-Host "  {" -ForegroundColor Gray
Write-Host "    `"mcpServers`": {" -ForegroundColor Gray
Write-Host "      `"windiag`": {" -ForegroundColor Gray
Write-Host "        `"command`": `"dotnet`"," -ForegroundColor Gray
Write-Host "        `"args`": [" -ForegroundColor Gray
Write-Host "          `"run`"," -ForegroundColor Gray
Write-Host "          `"--project`"," -ForegroundColor Gray
Write-Host "          `"$projectPathUnix`"" -ForegroundColor Gray
Write-Host "        ]" -ForegroundColor Gray
Write-Host "      }" -ForegroundColor Gray
Write-Host "    }" -ForegroundColor Gray
Write-Host "  }" -ForegroundColor Gray
Write-Host ""

# Check if Claude Desktop is running
Write-Host "[5/5] Checking Claude Desktop status..." -ForegroundColor White
$claudeProcess = Get-Process -Name "Claude" -ErrorAction SilentlyContinue
if ($claudeProcess) {
    Write-Host "      Claude Desktop is running" -ForegroundColor Yellow
    Write-Host "      You need to restart it for changes to take effect" -ForegroundColor Yellow
}
else {
    Write-Host "      Claude Desktop is not currently running" -ForegroundColor Gray
}
Write-Host ""

Write-Host "============================================" -ForegroundColor Cyan
Write-Host "Setup Complete! ✓" -ForegroundColor Green
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Next Steps:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  1. Restart Claude Desktop" -ForegroundColor White
Write-Host "     (Close and reopen the application)" -ForegroundColor Gray
Write-Host ""
Write-Host "  2. Start a new conversation in Claude" -ForegroundColor White
Write-Host ""
Write-Host "  3. Ask Claude:" -ForegroundColor White
Write-Host "     'What tools do you have access to?'" -ForegroundColor Cyan
Write-Host ""
Write-Host "  4. Test the tool:" -ForegroundColor White
Write-Host "     'What is my system information?'" -ForegroundColor Cyan
Write-Host ""
Write-Host "  5. Claude should use the get_system_info tool" -ForegroundColor White
Write-Host "     and display your system diagnostics!" -ForegroundColor Gray
Write-Host ""

Write-Host "Troubleshooting:" -ForegroundColor Yellow
Write-Host ""
Write-Host "  If tools don't appear:" -ForegroundColor White
Write-Host "    - Make sure Claude Desktop is fully closed and restarted" -ForegroundColor Gray
Write-Host "    - Check that .NET SDK is in your PATH" -ForegroundColor Gray
Write-Host "    - Look for errors in Claude's developer console" -ForegroundColor Gray
Write-Host ""
Write-Host "  For more help, see: docs\MCP_TESTING_GUIDE.md" -ForegroundColor Gray
Write-Host ""

pause
