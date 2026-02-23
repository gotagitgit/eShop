# eShop Quick Reference

## Infrastructure Services

| Service | Container | Port(s) | Credentials | Status |
|---------|-----------|---------|-------------|--------|
| Redis | eshop-redis | 6379 | (none) | Required |
| RabbitMQ | eshop-rabbitmq | 5672, 15672 | guest/guest | Required |
| PostgreSQL | eshop-postgres | 5432 | postgres/postgres | Required |
| Ollama | (WSL - manual) | 11434 | (none) | Optional |

## Quick Start Commands

### Start Infrastructure
```powershell
cd Docker
.\start-infrastructure.ps1
```

### Stop Infrastructure
```powershell
cd Docker
.\stop-infrastructure.ps1
```

### Run Application
```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### Clean Slate (Remove All Data)
```powershell
cd Docker
.\stop-infrastructure.ps1 -RemoveVolumes
.\start-infrastructure.ps1
```

## Service URLs

### Infrastructure
- **RabbitMQ Management**: http://localhost:15672 (guest/guest)
- **Aspire Dashboard**: http://localhost:19888 (shown in console output)

### Application (after starting)
- **WebApp**: Check Aspire Dashboard for URL
- **Catalog API**: Check Aspire Dashboard for URL
- **Identity API**: Check Aspire Dashboard for URL

## Enable Optional Features

### Enable Ollama (AI Features)

1. Ensure Ollama is running in WSL:
   ```bash
   # In WSL
   ollama serve
   ```

2. Pull required models:
   ```bash
   ollama pull all-minilm
   ollama pull llama3.1
   ```

3. Edit `src/eShop.AppHost/Program.cs`:
   ```csharp
   bool useOllama = true;  // Change to true
   ```

See [OLLAMA-WSL-SETUP.md](OLLAMA-WSL-SETUP.md) for details.

### Enable Azure OpenAI

1. Add connection string to `src/eShop.AppHost/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "OpenAi": "Endpoint=xxx;Key=xxx;"
     }
   }
   ```

2. Edit `src/eShop.AppHost/Program.cs`:
   ```csharp
   bool useOpenAI = true;  // Change to true
   ```

## Troubleshooting

### Containers Won't Start
```powershell
# Check Docker is running
docker info

# View container logs
cd Docker
docker-compose logs
```

### Application Can't Connect
```powershell
# Verify containers are running
cd Docker
docker-compose ps

# Should show all 3 containers as "Up"
```

### Port Conflicts
```powershell
# Check what's using a port
netstat -ano | findstr :5432
netstat -ano | findstr :6379
netstat -ano | findstr :5672
```

### Database Issues
```powershell
# Reset all data
cd Docker
.\stop-infrastructure.ps1 -RemoveVolumes
.\start-infrastructure.ps1
```

### Ollama Not Working
```powershell
# Test from Windows
curl http://localhost:11434/api/version

# If fails, check WSL networking
# See OLLAMA-WSL-SETUP.md for solutions
```

## File Locations

- **Docker Compose**: `Docker/docker-compose.yml`
- **Aspire AppHost**: `src/eShop.AppHost/Program.cs`
- **Infrastructure Scripts**: `Docker/*.ps1`
- **Documentation**: `Docker/*.md`

## Data Persistence

- **RabbitMQ Data**: Docker volume `rabbitmq-data`
- **PostgreSQL Data**: Docker volume `postgres-data`
- **Redis Data**: Not persisted (in-memory only)

To preserve data between runs, use `docker-compose down` (without `-v` flag).

To start fresh, use `.\stop-infrastructure.ps1 -RemoveVolumes`.

## Common Workflows

### Daily Development
```powershell
# Morning
cd Docker
.\start-infrastructure.ps1
cd ..
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Evening
cd Docker
.\stop-infrastructure.ps1
```

### Testing Changes
```powershell
# Keep infrastructure running
# Stop app with Ctrl+C
# Make code changes
# Restart app
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### Clean Environment
```powershell
cd Docker
.\stop-infrastructure.ps1 -RemoveVolumes
.\start-infrastructure.ps1
cd ..
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

## Environment Variables

### Force HTTP Endpoints (for testing)
```powershell
$env:ESHOP_USE_HTTP_ENDPOINTS=1
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

## Getting Help

- **Docker Issues**: See `Docker/README.md`
- **Ollama Setup**: See `Docker/OLLAMA-WSL-SETUP.md`
- **Migration Notes**: See `Docker/MIGRATION-NOTES.md`
- **Main README**: See `README.md`
