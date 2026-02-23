# Stop eShop Infrastructure Containers
# This script stops Redis, RabbitMQ, and PostgreSQL containers

param(
    [switch]$RemoveVolumes
)

Write-Host "Stopping eShop infrastructure containers..." -ForegroundColor Yellow

# Navigate to the Docker folder
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

if ($RemoveVolumes) {
    Write-Host "Stopping containers and removing volumes (all data will be lost)..." -ForegroundColor Red
    docker-compose down -v
} else {
    Write-Host "Stopping containers (data will be preserved)..." -ForegroundColor Cyan
    docker-compose down
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nContainers stopped successfully!" -ForegroundColor Green
    
    if ($RemoveVolumes) {
        Write-Host "All data volumes have been removed." -ForegroundColor Yellow
    } else {
        Write-Host "Data volumes preserved. Use -RemoveVolumes flag to delete all data." -ForegroundColor Cyan
    }
} else {
    Write-Host "`nERROR: Failed to stop containers. Check the output above for details." -ForegroundColor Red
    exit 1
}
