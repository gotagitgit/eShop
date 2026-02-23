# Using Ollama in WSL with eShop

This guide explains how to set up and use Ollama in WSL with the eShop application. This is ideal for developers who want to run AI features locally without requiring Azure OpenAI or other cloud services.

## Prerequisites

- Windows 10 version 2004+ or Windows 11
- Administrator access to enable WSL

## First-Time Setup for New Developers

If you're setting up this project for the first time and don't have WSL or Ollama installed, follow these steps:

### Step 1: Enable and Install WSL

#### Check if WSL is Already Enabled
```powershell
# In PowerShell
wsl --version
```

If you see version information, WSL is already installed. Skip to Step 2.

#### Install WSL (if not already installed)
```powershell
# In PowerShell as Administrator
wsl --install
```

This will:
- Enable WSL feature
- Install Ubuntu as the default Linux distribution
- Set up WSL2 (recommended)

**Restart your computer** after installation.

#### Set Up Your Linux User Account
After restart, Ubuntu will launch automatically and prompt you to:
1. Create a username (lowercase, no spaces)
2. Create a password

### Step 2: Verify WSL Installation

```powershell
# Check WSL version (should be WSL2)
wsl --version
wsl -l -v

# Launch WSL
wsl
```

You should now be in a Linux terminal.

### Step 3: Install Ollama in WSL

#### Option A: Using the Official Install Script (Recommended)
```bash
# In WSL terminal
curl -fsSL https://ollama.com/install.sh | sh
```

This will download and install Ollama automatically.

#### Option B: Manual Installation
```bash
# In WSL terminal
# Download Ollama
curl -L https://ollama.com/download/ollama-linux-amd64 -o ollama

# Make it executable
chmod +x ollama

# Move to system path
sudo mv ollama /usr/local/bin/

# Verify installation
ollama --version
```

### Step 4: Start Ollama Service

```bash
# In WSL terminal
ollama serve
```

Keep this terminal window open. Ollama is now running on `http://localhost:11434`.

**Tip**: To run Ollama in the background, use:
```bash
nohup ollama serve > /dev/null 2>&1 &
```

### Step 5: Pull Required Models

Open a **new WSL terminal** (keep the first one running Ollama):

```bash
# Pull embedding model (for product search)
ollama pull all-minilm

# Pull chat model - choose one based on your hardware:

# Option 1: Fastest (recommended for CPU-only laptops)
ollama pull llama3.2:1b

# Option 2: Balanced performance
ollama pull llama3.2

# Option 3: Better quality (slower on CPU)
ollama pull phi3:mini
```

Verify models are installed:
```bash
ollama list
```

### Step 6: Test Ollama from Windows

```powershell
# In PowerShell (Windows)
curl http://localhost:11434/api/version
```

If you see version information, Ollama is accessible from Windows!

### Step 7: Configure eShop Application

The application is already configured to use Ollama. Just verify the settings in `src/eShop.AppHost/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "embedding": "Endpoint=http://localhost:11434;Model=all-minilm",
    "chat": "Endpoint=http://localhost:11434;Model=llama3.2:1b"
  }
}
```

Update the `chat` model name if you chose a different model in Step 5.

## Quick Start for Developers with Existing Setup

If you already have Ollama installed in WSL:

## Quick Start for Developers with Existing Setup

If you already have Ollama installed in WSL:

### 1. Verify Ollama is Running

### 1. Verify Ollama is Running

#### In WSL

Check if Ollama is running:
```bash
# Check if Ollama service is running
ps aux | grep ollama

# Test Ollama API
curl http://localhost:11434/api/version
```

If not running, start Ollama:
```bash
ollama serve
```

#### From Windows

Verify Windows can access WSL Ollama:
```powershell
# Test connection from Windows
curl http://localhost:11434/api/version
```

If this fails, you may need to configure WSL networking (see Troubleshooting below).

## Required Models

eShop uses two Ollama models:

### 1. Embedding Model (for Catalog API)
```bash
# In WSL
ollama pull all-minilm
```

### 2. Chat Model (for WebApp)
```bash
# In WSL - Use llama3.2 (you have this installed)
# For better CPU performance, consider the 1B parameter version:
ollama pull llama3.2:1b   # Faster on CPU
# OR use the full version (slower but more capable):
ollama pull llama3.2      # What you currently have
```

Verify models are installed:
```bash
ollama list
```

## Enable Ollama in eShop

Edit `src/eShop.AppHost/Program.cs` and set:
```csharp
bool useOllama = true;  // Change from false to true
```

The application will connect to `http://localhost:11434` automatically.

## Running eShop with Ollama

```powershell
# Step 1: Ensure Ollama is running in WSL
# In WSL terminal: ollama serve

# Step 2: Start infrastructure containers
cd Docker
.\start-infrastructure.ps1

# Step 3: Run eShop application
cd ..
dotnet run --project src/eShop.AppHost/eShop.AppHost.csproj
```

## Configuration

The default configuration connects to:
- **Endpoint**: `http://localhost:11434`
- **Embedding Model**: `all-minilm` (used by Catalog API)
- **Chat Model**: `llama3.2` (used by WebApp)
- **Timeout**: 5 minutes (300 seconds) for CPU-based inference

### Custom Configuration

To use different models or endpoint, edit `src/eShop.AppHost/Program.cs`:

```csharp
if (useOllama)
{
    var ollamaEndpoint = builder.AddConnectionString("ollama", "http://localhost:11434");
    
    catalogApi.WithReference(ollamaEndpoint)
        .WithEnvironment("OllamaEnabled", "true")
        .WithEnvironment("Ollama__Endpoint", "http://localhost:11434")
        .WithEnvironment("Ollama__EmbeddingModel", "your-embedding-model");  // Change here
    
    webApp.WithReference(ollamaEndpoint)
        .WithEnvironment("OllamaEnabled", "true")
        .WithEnvironment("Ollama__Endpoint", "http://localhost:11434")
        .WithEnvironment("Ollama__ChatModel", "your-chat-model");  // Change here
}
```

## Troubleshooting

### Windows Cannot Access WSL Ollama

If `curl http://localhost:11434` fails from Windows:

#### Option 1: Use WSL IP Address
```powershell
# Get WSL IP address
wsl hostname -I
```

Update Program.cs to use the WSL IP:
```csharp
var ollamaEndpoint = builder.AddConnectionString("ollama", "http://<WSL_IP>:11434");
```

#### Option 2: Configure WSL Port Forwarding
```powershell
# In PowerShell as Administrator
netsh interface portproxy add v4tov4 listenport=11434 listenaddress=0.0.0.0 connectport=11434 connectaddress=<WSL_IP>
```

#### Option 3: Use WSL2 Mirrored Mode (Windows 11 22H2+)
Create/edit `%USERPROFILE%\.wslconfig`:
```ini
[wsl2]
networkingMode=mirrored
```

Restart WSL:
```powershell
wsl --shutdown
```

### Ollama Models Not Found

Ensure models are pulled:
```bash
# In WSL
ollama pull all-minilm
ollama pull llama3.1
ollama list
```

### Connection Timeout

Check Ollama is listening on all interfaces:
```bash
# In WSL, start Ollama with explicit host
OLLAMA_HOST=0.0.0.0:11434 ollama serve
```

### Performance Issues

Ollama on CPU (without GPU) can be slow, especially with larger models. Here are optimization strategies:

#### 1. Use Smaller/Faster Models
For better performance on CPU-only hardware:
```bash
# In WSL - Use smaller, faster models
ollama pull llama3.2:1b      # 1B parameter model (much faster)
ollama pull all-minilm       # Already optimized for embeddings
```

Then update `src/eShop.AppHost/appsettings.json`:
```json
{
  "ConnectionStrings": {
    "chat": "Endpoint=http://localhost:11434;Model=llama3.2:1b"
  }
}
```

#### 2. Increase Timeout Settings
The application is configured with 5-minute timeouts for CPU inference:
- HttpClient timeout: 5 minutes
- Resilience handler attempt timeout: 5 minutes  
- Resilience handler total request timeout: 5 minutes

These are configured in:
- `src/WebApp/Extensions/Extensions.cs` - Chat client timeout configuration
- `src/Catalog.API/Extensions/Extensions.cs` - Embedding client timeout configuration
- `src/eShop.AppHost/appsettings.json` - Aspire.OllamaSharp.Timeout setting

If you still see timeouts, you can increase these values further by editing the `TimeSpan.FromMinutes(5)` values in the Extensions.cs files.

#### 3. Allocate More WSL Resources
Ensure WSL2 (not WSL1): `wsl -l -v`

Create/edit `%USERPROFILE%\.wslconfig`:
```ini
[wsl2]
memory=8GB          # Increase RAM allocation
processors=4        # Increase CPU cores
swap=4GB            # Add swap space
```

Restart WSL:
```powershell
wsl --shutdown
```

#### 4. Monitor Ollama Performance
```bash
# In WSL - Watch Ollama logs
journalctl -u ollama -f

# Or if running manually
OLLAMA_DEBUG=1 ollama serve
```

#### Model Size Comparison
- `llama3.2:1b` - ~1GB, fastest, good for simple tasks
- `llama3.2` (3B) - ~2GB, balanced performance
- `llama3.1` (8B) - ~4.7GB, slower but more capable

For CPU-only systems, we recommend `llama3.2:1b` for the best experience.

## Testing Ollama Integration

### Test Embedding Model (Catalog API)
Once the application is running, the Catalog API will use Ollama for semantic search:

```powershell
# Search catalog with semantic relevance
curl "http://localhost:<catalog-port>/api/catalog/items/withsemanticrelevance/laptop?api-version=1.0"
```

### Test Chat Model (WebApp)
The WebApp will use Ollama for AI-powered features in the UI.

## Alternative: Run Ollama in Docker

If you prefer Docker over WSL, add to `Docker/docker-compose.yml`:

```yaml
  ollama:
    image: ollama/ollama:latest
    container_name: eshop-ollama
    ports:
      - "11434:11434"
    networks:
      - eshop-network
    volumes:
      - ollama-data:/root/.ollama
    restart: unless-stopped
```

Then pull models:
```powershell
docker exec -it eshop-ollama ollama pull all-minilm
docker exec -it eshop-ollama ollama pull llama3.1
```

## Resources

- [Ollama Documentation](https://ollama.ai/docs)
- [WSL Networking](https://learn.microsoft.com/en-us/windows/wsl/networking)
- [Ollama Models](https://ollama.ai/library)
