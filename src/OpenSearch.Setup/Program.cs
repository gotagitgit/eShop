using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SearchServices;
using SearchServices.Models;
using SearchServices.Services;
using SearchServices.Settings;

// Parse arguments
var configuration = new ConfigurationBuilder()
    .AddCommandLine(args)
    .AddEnvironmentVariables()
    .Build();

var endpoint = configuration["endpoint"] ?? "http://localhost:9200";
var modelId = configuration["model-id"] ?? "huggingface/sentence-transformers/all-MiniLM-L6-v2";
var embeddingDimension = int.Parse(configuration["embedding-dimension"] ?? "384");
var pipelineName = configuration["pipeline"] ?? "catalog-neural-pipeline";
var searchPipelineName = configuration["search-pipeline"] ?? "catalog-hybrid-search";
var modelFilePath = configuration["model-file"] ?? 
    Path.Combine("..", "..", "..", "..", "Docker", "opensearch", "models", "sentence-transformers_all-MiniLM-L6-v2-1.0.1-torch_script.zip");

Console.WriteLine("OpenSearch Setup Utility");
Console.WriteLine($"  Endpoint:            {endpoint}");
Console.WriteLine($"  Model ID:            {modelId}");
Console.WriteLine($"  Embedding Dimension: {embeddingDimension}");
Console.WriteLine($"  Ingest Pipeline:     {pipelineName}");
Console.WriteLine($"  Search Pipeline:     {searchPipelineName}");
Console.WriteLine($"  Model File:          {modelFilePath}");
Console.WriteLine();

// Build DI container
var services = new ServiceCollection();
services.Configure<SearchSettings>(s => s.SearchAddress = endpoint);
services.Register();
var serviceProvider = services.BuildServiceProvider();

var modelService = serviceProvider.GetRequiredService<IOpenSearchModelService>();
var clusterService = serviceProvider.GetRequiredService<IOpenSearchClusterService>();

var cts = new CancellationTokenSource(TimeSpan.FromMinutes(60));
var token = cts.Token;

// Step 1: Wait for cluster
await WaitForClusterAsync();

// Step 2: Configure cluster settings
Console.Write("Configuring cluster settings... ");
var settingsResult = await clusterService.UpdateSettingsAsync(new ClusterSettings(new Dictionary<string, object>
{
    ["plugins.ml_commons.only_run_on_ml_node"] = "false",
    ["plugins.ml_commons.model_access_control_enabled"] = "true",
    ["plugins.ml_commons.native_memory_threshold"] = "99",
    ["plugins.ml_commons.allow_registering_model_via_url"] = true
}), token);

if (!settingsResult.IsSuccess)
    throw new InvalidOperationException($"Failed to configure cluster settings: {settingsResult.Body}");
Console.WriteLine("done.");

// Step 3: Create model group
Console.Write("Creating model group... ");
var groupResult = await modelService.CreateModelGroupAsync(
    new MLModelGroup("catalog_model_group", "Model group for catalog embedding models"), token);

if (!groupResult.IsSuccess)
    throw new InvalidOperationException("Failed to create or find model group.");
Console.WriteLine($"done (group_id: {groupResult.Id}).");

// Step 4: Register model from local file
Console.Write($"Registering model '{modelId}'... ");

if (!File.Exists(modelFilePath))
    throw new FileNotFoundException($"Model file not found: {Path.GetFullPath(modelFilePath)}");

var fileName = Path.GetFileName(modelFilePath);
var hash = ComputeSha256(modelFilePath);
Console.Write($"(hash: {hash[..12]}...) ");

var registration = new MLModelRegistration(
    Name: modelId,
    Version: "1.0.1",
    ModelFormat: "TORCH_SCRIPT",
    ModelGroupId: groupResult.Id,
    ModelType: "bert",
    EmbeddingDimension: embeddingDimension,
    FrameworkType: "sentence_transformers",
    Url: $"file:///tmp/models/{fileName}",
    ModelContentHashValue: hash);

var registerResult = await modelService.RegisterModelAsync(registration, token);
if (!registerResult.IsSuccess)
    throw new InvalidOperationException("Failed to register model.");

string deployedModelId;

if (registerResult.AlreadyExists)
{
    Console.WriteLine($"already registered (model_id: {registerResult.ModelId}).");
    deployedModelId = registerResult.ModelId!;
}
else
{
    Console.WriteLine($"done (task_id: {registerResult.TaskId}).");

    // Step 5: Wait for registration
    Console.Write("Waiting for model registration... ");
    var registrationTask = await modelService.PollTaskAsync(registerResult.TaskId!, token);
    Console.WriteLine($"done (model_id: {registrationTask.ModelId}).");
    deployedModelId = registrationTask.ModelId;
}

// Step 6: Deploy model
Console.Write("Deploying model... ");
var deployResult = await modelService.DeployModelAsync(deployedModelId, token);
if (!deployResult.IsSuccess)
    throw new InvalidOperationException("Failed to deploy model.");

if (!string.IsNullOrEmpty(deployResult.TaskId))
{
    await modelService.PollTaskAsync(deployResult.TaskId, token);
    Console.WriteLine("done.");
}
else
{
    Console.WriteLine("already deployed.");
}

// Step 7: Create ingest pipeline
Console.Write($"Ensuring ingest pipeline '{pipelineName}'... ");
var ingestResult = await clusterService.EnsureIngestPipelineAsync(
    new IngestPipelineConfig(pipelineName, deployedModelId, "nameDescription", "embedding"), token);
Console.WriteLine(ingestResult.IsSuccess ? "done." : $"FAILED: {ingestResult.Body}");

// Step 8: Create search pipeline
Console.Write($"Ensuring search pipeline '{searchPipelineName}'... ");
var searchResult = await clusterService.EnsureSearchPipelineAsync(
    new SearchPipelineConfig(searchPipelineName, "min_max", "arithmetic_mean", [0.7, 0.3]), token);
Console.WriteLine(searchResult.IsSuccess ? "done." : $"FAILED: {searchResult.Body}");

Console.WriteLine();
Console.WriteLine("Setup completed successfully.");
return;

// --- Functions ---

async Task WaitForClusterAsync()
{
    Console.Write("Waiting for OpenSearch cluster... ");
    var delay = TimeSpan.FromSeconds(1);
    var maxDelay = TimeSpan.FromSeconds(30);
    const int maxRetries = 10;

    for (var attempt = 1; attempt <= maxRetries; attempt++)
    {
        try
        {
            if (await clusterService.CheckHealthAsync(token))
            {
                Console.WriteLine("available.");
                return;
            }
        }
        catch
        {
            // retry
        }

        if (attempt < maxRetries)
        {
            Console.Write($"(attempt {attempt}/{maxRetries}) ");
            await Task.Delay(delay, token);
            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
        }
    }

    throw new InvalidOperationException($"OpenSearch was not available after {maxRetries} retries");
}

static string ComputeSha256(string filePath)
{
    using var stream = File.OpenRead(filePath);
    var hash = SHA256.HashData(stream);
    return Convert.ToHexStringLower(hash);
}
