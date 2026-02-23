# Start eShop Infrastructure Containers
# This script starts Redis, RabbitMQ, and PostgreSQL containers

Write-Host "Starting eShop infrastructure containers..." -ForegroundColor Green

# Check if Docker is running
$dockerRunning = docker info 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) {
    Write-Host "ERROR: Docker is not running. Please start Docker Desktop first." -ForegroundColor Red
    exit 1
}

# Navigate to the Docker folder
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptPath

# Start containers
Write-Host "Starting containers with docker-compose..." -ForegroundColor Cyan
docker-compose up -d

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nContainers started successfully!" -ForegroundColor Green
    Write-Host "`nRunning containers:" -ForegroundColor Cyan
    docker-compose ps
    
    Write-Host "`n=== Service URLs ===" -ForegroundColor Yellow
    Write-Host "Redis:              localhost:6379" -ForegroundColor White
    Write-Host "RabbitMQ AMQP:      localhost:5672" -ForegroundColor White
    Write-Host "RabbitMQ Management: http://localhost:15672 (guest/guest)" -ForegroundColor White
    Write-Host "PostgreSQL:         localhost:5432 (postgres/postgres)" -ForegroundColor White
    
    Write-Host "`nYou can now run the eShop application:" -ForegroundColor Green
    Write-Host "  dotnet run --project ../src/eShop.AppHost/eShop.AppHost.csproj" -ForegroundColor Cyan
} else {
    Write-Host "`nERROR: Failed to start containers. Check the output above for details." -ForegroundColor Red
    exit 1
}
