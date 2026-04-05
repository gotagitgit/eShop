using System.Text.Json;
using OpenSearch.Net;
using HttpMethod = OpenSearch.Net.HttpMethod;

namespace eShop.Catalog.API.Services;

/// <summary>
/// Hosted service that initializes the OpenSearch index on startup:
/// creates the index with k-NN mappings and migrates existing catalog items from PostgreSQL.
/// All steps are idempotent — safe to run on every application restart.
///
/// Prerequisites: The ML model, ingest pipeline, and search pipeline must already exist
/// in OpenSearch. Use the OpenSearch.Setup utility to provision them before starting the app.
/// </summary>
public sealed class OpenSearchInitializationService : IHostedService
{
    private readonly OpenSearchLowLevelClient _client;
    private readonly OpenSearchOptions _options;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OpenSearchInitializationService> _logger;

    public OpenSearchInitializationService(
        OpenSearchLowLevelClient client,
        IOptions<OpenSearchOptions> options,
        IServiceProvider serviceProvider,
        ILogger<OpenSearchInitializationService> logger)
    {
        _client = client;
        _options = options.Value;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await WaitForOpenSearchAsync(cancellationToken);
            await ResolveModelIdAsync(cancellationToken);
            await CreateIndexAsync(cancellationToken);
            await MigrateDataAsync(cancellationToken);

            _logger.LogInformation("OpenSearch initialization completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OpenSearch initialization was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenSearch initialization failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task WaitForOpenSearchAsync(CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(1);
        var maxDelay = TimeSpan.FromSeconds(30);
        const int maxRetries = 10;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var response = await _client.DoRequestAsync<StringResponse>(
                    HttpMethod.GET,
                    "/_cluster/health",
                    cancellationToken);

                if (response.Success)
                {
                    _logger.LogInformation("OpenSearch cluster is available (attempt {Attempt})", attempt);
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "OpenSearch not available yet (attempt {Attempt}/{MaxRetries})", attempt, maxRetries);
            }

            if (attempt < maxRetries)
            {
                await Task.Delay(delay, cancellationToken);
                delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 2, maxDelay.TotalSeconds));
            }
        }

        throw new InvalidOperationException($"OpenSearch was not available after {maxRetries} retries");
    }

    private async Task ResolveModelIdAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Resolving deployed model ID for '{ModelName}'", _options.ModelId);

        var searchBody = JsonSerializer.Serialize(new
        {
            query = new
            {
                match_phrase = new { name = _options.ModelId }
            },
            size = 1
        });

        var response = await _client.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            "/_plugins/_ml/models/_search",
            cancellationToken,
            PostData.String(searchBody));

        if (!response.Success)
        {
            _logger.LogWarning("Failed to search for model '{ModelName}', using configured ModelId as-is", _options.ModelId);
            return;
        }

        using var doc = JsonDocument.Parse(response.Body);
        var hits = doc.RootElement.GetProperty("hits").GetProperty("hits");

        for (var i = 0; i < hits.GetArrayLength(); i++)
        {
            var source = hits[i].GetProperty("_source");

            // All documents (model and chunks) have model_id in _source
            if (source.TryGetProperty("model_id", out var modelIdElement))
            {
                var resolvedId = modelIdElement.GetString();
                if (!string.IsNullOrEmpty(resolvedId))
                {
                    _logger.LogInformation("Resolved model '{ModelName}' to ID '{ModelId}'", _options.ModelId, resolvedId);
                    _options.ModelId = resolvedId;
                    return;
                }
            }
        }

        _logger.LogWarning("Could not find deployed model '{ModelName}', using configured ModelId as-is", _options.ModelId);
    }

    private async Task CreateIndexAsync(CancellationToken cancellationToken)
    {
        var indexName = _options.IndexName;

        // Check if index already exists
        var headResponse = await _client.DoRequestAsync<StringResponse>(
            HttpMethod.HEAD,
            $"/{indexName}",
            cancellationToken);

        if (headResponse.Success)
        {
            _logger.LogInformation("Index '{IndexName}' already exists, skipping creation", indexName);
            return;
        }

        var indexBody = JsonSerializer.Serialize(new
        {
            settings = new
            {
                index = new Dictionary<string, object>
                {
                    ["knn"] = true,
                    ["default_pipeline"] = _options.PipelineName
                }
            },
            mappings = new
            {
                properties = new Dictionary<string, object>
                {
                    ["id"] = new { type = "integer" },
                    ["name"] = new { type = "text", analyzer = "standard" },
                    ["description"] = new { type = "text", analyzer = "standard" },
                    ["price"] = new { type = "float" },
                    ["catalogTypeId"] = new { type = "integer" },
                    ["catalogBrandId"] = new { type = "integer" },
                    ["availableStock"] = new { type = "integer" },
                    ["pictureFileName"] = new { type = "keyword" },
                    ["nameDescription"] = new { type = "text" },
                    ["embedding"] = new Dictionary<string, object>
                    {
                        ["type"] = "knn_vector",
                        ["dimension"] = _options.EmbeddingDimension,
                        ["method"] = new Dictionary<string, object>
                        {
                            ["name"] = "hnsw",
                            ["space_type"] = "cosinesimil",
                            ["engine"] = "lucene",
                            ["parameters"] = new Dictionary<string, object>
                            {
                                ["ef_construction"] = 512,
                                ["m"] = 16
                            }
                        }
                    }
                }
            }
        });

        var response = await _client.DoRequestAsync<StringResponse>(
            HttpMethod.PUT,
            $"/{indexName}",
            cancellationToken,
            PostData.String(indexBody));

        if (response.Success)
        {
            _logger.LogInformation("Created index '{IndexName}' with k-NN mappings", indexName);
        }
        else
        {
            _logger.LogError("Failed to create index '{IndexName}': {Response}", indexName, response.Body);
        }
    }

    private async Task MigrateDataAsync(CancellationToken cancellationToken)
    {
        // Check if index already has documents
        var countResponse = await _client.DoRequestAsync<StringResponse>(
            HttpMethod.GET,
            $"/{_options.IndexName}/_count",
            cancellationToken);

        if (countResponse.Success)
        {
            using var doc = JsonDocument.Parse(countResponse.Body);
            if (doc.RootElement.TryGetProperty("count", out var countElement) && countElement.GetInt64() > 0)
            {
                _logger.LogInformation(
                    "Index '{IndexName}' already contains {Count} documents, skipping migration",
                    _options.IndexName, countElement.GetInt64());
                return;
            }
        }

        _logger.LogInformation("Starting data migration from PostgreSQL to OpenSearch index '{IndexName}'", _options.IndexName);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CatalogContext>();

        var items = await context.CatalogItems
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (items.Count == 0)
        {
            _logger.LogInformation("No catalog items found in PostgreSQL, skipping migration");
            return;
        }

        var indexed = 0;
        foreach (var item in items)
        {
            var document = new
            {
                id = item.Id,
                name = item.Name,
                description = item.Description,
                price = item.Price,
                catalogTypeId = item.CatalogTypeId,
                catalogBrandId = item.CatalogBrandId,
                availableStock = item.AvailableStock,
                pictureFileName = item.PictureFileName,
                nameDescription = $"{item.Name} {item.Description}"
            };

            var response = await _client.IndexAsync<StringResponse>(
                _options.IndexName,
                item.Id.ToString(),
                PostData.Serializable(document),
                new IndexRequestParameters
                {
                    QueryString = { { "pipeline", _options.PipelineName } }
                },
                cancellationToken);

            if (response.Success)
            {
                indexed++;
            }
            else
            {
                _logger.LogWarning(
                    "Failed to index catalog item {ItemId} during migration: {Error}",
                    item.Id, response.OriginalException?.Message ?? "Unknown error");
            }
        }

        _logger.LogInformation(
            "Data migration completed: {IndexedCount}/{TotalCount} catalog items indexed to '{IndexName}'",
            indexed, items.Count, _options.IndexName);
    }
}
