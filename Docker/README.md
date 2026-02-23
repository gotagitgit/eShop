# eShop Docker Infrastructure

This folder contains the Docker Compose configuration for running eShop's infrastructure dependencies manually.

## Prerequisites

- Docker Desktop installed and running
- Docker Compose (included with Docker Desktop)

## Services Included

- **Redis** (port 6379) - Caching and basket storage
- **RabbitMQ** (ports 5672, 15672) - Message broker with management UI
- **PostgreSQL with pgvector** (port 5432) - Database server

## Quick Start

### Using PowerShell Scripts (Recommended for Windows)

```powershell
# Start all infrastructure containers
.\start-infrastructure.ps1

# Stop containers (preserve data)
.\stop-infrastructure.ps1

# Stop containers and remove all data
.\stop-infrastructure.ps1 -RemoveVolumes
```

### Using Docker Compose Directly

#### Start All Services

```powershell
# From the Docker folder
docker-compose up -d
```

#### Stop All Services

```powershell
docker-compose down
```

#### Stop and Remove Volumes (Clean Start)

```powershell
docker-compose down -v
```

### View Logs

```powershell
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f postgres
docker-compose logs -f rabbitmq
docker-compose logs -f redis
```

### Check Service Status

```powershell
docker-compose ps
```

## Service Details

### Redis
- **Container Name**: eshop-redis
- **Port**: 6379
- **Connection String**: localhost:6379

### RabbitMQ
- **Container Name**: eshop-rabbitmq
- **AMQP Port**: 5672
- **Management UI**: http://localhost:15672
- **Default Credentials**: guest/guest

### PostgreSQL
- **Container Name**: eshop-postgres
- **Port**: 5432
- **Username**: postgres
- **Password**: postgres
- **Connection String**: Host=localhost;Port=5432;Username=postgres;Password=postgres

### Ollama (Optional - Not in Docker Compose)
If you want to use AI features, see [OLLAMA-WSL-SETUP.md](OLLAMA-WSL-SETUP.md) for instructions on using your existing Ollama installation in WSL.

## Data Persistence

Data is persisted in Docker volumes:
- `rabbitmq-data` - RabbitMQ messages and configuration
- `postgres-data` - PostgreSQL databases

To completely reset all data, use:
```powershell
docker-compose down -v
```

## Running eShop

After starting these containers, run the eShop application:

```powershell
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

The Aspire AppHost is configured to connect to these existing containers instead of creating new ones.

## Troubleshooting

### Port Already in Use
If you get port conflicts, check what's using the ports:
```powershell
netstat -ano | findstr :5432
netstat -ano | findstr :6379
netstat -ano | findstr :5672
```

### Container Won't Start
Check logs for the specific service:
```powershell
docker-compose logs postgres
```

### Reset Everything
```powershell
docker-compose down -v
docker-compose up -d
```
