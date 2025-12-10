# This script registers the URL for the WinDiag MCP Server's HTTP endpoint
# It must be run as Administrator

$port = 5555
$url = "http://localhost:$port/"
$user = "$env:USERDOMAIN\$env:USERNAME"

Write-Host "Registering URL: $url for user: $user" -ForegroundColor Cyan

# Delete existing reservation if any
netsh http delete urlacl url=$url | Out-Null

# Add new reservation
$result = netsh http add urlacl url=$url user=$user

if ($LASTEXITCODE -eq 0) {
    Write-Host "Successfully registered URL!" -ForegroundColor Green
} else {
    Write-Host "Failed to register URL. Ensure you are running as Administrator." -ForegroundColor Red
}

Write-Host ""
Write-Host "Press any key to exit..."
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
