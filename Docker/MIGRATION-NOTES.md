# Migration to Manual Docker Container Management

## What Changed

Previously, .NET Aspire automatically pulled and managed Docker containers (Redis, RabbitMQ, PostgreSQL) when starting the application. Now, you manually control these containers using Docker Compose.

## Benefits

1. **Full Control**: Start/stop infrastructure independently of the application
2. **Faster Startup**: Application starts immediately without waiting for container pulls
3. **Easier Debugging**: Infrastructure runs separately, easier to troubleshoot
4. **Data Persistence**: Better control over when to preserve or clear data
5. **Resource Management**: Can stop containers when not developing

## Changes Made

### 1. Docker Compose Configuration
- **Location**: `Docker/docker-compose.yml`
- **Services**: Redis, RabbitMQ, PostgreSQL with pgvector
- **Volumes**: Persistent storage for RabbitMQ and PostgreSQL data
- **Network**: Isolated bridge network for all services

### 2. Aspire AppHost Updates
- **File**: `src/eShop.AppHost/Program.cs`
- **Change**: Replaced `AddRedis()`, `AddRabbitMQ()`, `AddPostgres()` with `AddConnectionString()`
- **Behavior**: Now connects to existing containers instead of creating new ones
- **Removed**: All `.WaitFor()` calls and container lifecycle management

### 3. Helper Scripts
- `start-infrastructure.ps1` - Start all containers with status display
- `stop-infrastructure.ps1` - Stop containers with optional volume removal

## New Workflow

### Before (Automatic)
```powershell
# Aspire pulled and started everything
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

### After (Manual Control)
```powershell
# Step 1: Start infrastructure
cd Docker
.\start-infrastructure.ps1

# Step 2: Run application
cd ..
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj

# Step 3: Stop infrastructure when done
cd Docker
.\stop-infrastructure.ps1
```

## Connection Details

All services use default credentials for local development:

| Service | Host | Port | Credentials |
|---------|------|------|-------------|
| Redis | localhost | 6379 | (none) |
| RabbitMQ | localhost | 5672, 15672 | guest/guest |
| PostgreSQL | localhost | 5432 | postgres/postgres |

## Troubleshooting

### Application Can't Connect to Containers
**Problem**: Application fails with connection errors

**Solution**: Ensure containers are running
```powershell
cd Docker
docker-compose ps
```

If not running, start them:
```powershell
.\start-infrastructure.ps1
```

### Port Conflicts
**Problem**: Container fails to start due to port already in use

**Solution**: Check what's using the port
```powershell
netstat -ano | findstr :5432
netstat -ano | findstr :6379
netstat -ano | findstr :5672
```

Stop the conflicting service or change ports in `docker-compose.yml`

### Database Connection Issues
**Problem**: Services can't connect to PostgreSQL databases

**Solution**: The databases are created automatically by Entity Framework migrations when the application first runs. Ensure:
1. PostgreSQL container is running
2. Connection string is correct (check AppHost Program.cs)
3. Run the application at least once to create databases

### Clean Slate Needed
**Problem**: Want to start fresh with no data

**Solution**: Remove all volumes
```powershell
.\stop-infrastructure.ps1 -RemoveVolumes
.\start-infrastructure.ps1
```

## Reverting Changes

If you want to go back to automatic container management:

1. Restore the original `src/eShop.AppHost/Program.cs` from git
2. Remove or ignore the `Docker/` folder
3. Let Aspire manage containers automatically

```powershell
git checkout src/eShop.AppHost/Program.cs
```

## Notes

- Ollama is NOT included in docker-compose (as requested)
- Container names are prefixed with `eshop-` for easy identification
- Data persists between restarts unless explicitly removed with `-RemoveVolumes`
- All containers use `restart: unless-stopped` policy
