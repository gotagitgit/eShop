using System.Diagnostics;
using System.Text.Json;
using OpenSearch.Net;

namespace eShop.Catalog.API.Services;

public sealed class OpenSearchSearchService : ICatalogSearch
{
    private readonly OpenSearchLowLevelClient _client;
    private readonly OpenSearchOptions _options;
    private readonly ILogger<OpenSearchSearchService> _logger;

    public OpenSearchSearchService(
        OpenSearchLowLevelClient client,
        IOptions<OpenSearchOptions> options,
        ILogger<OpenSearchSearchService> logger)
    {
        _client = client;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsEnabled => true;

    /// <inheritdoc/>
    public async Task<List<CatalogSearchDocument>> SearchAsync(string query, int pageIndex, int pageSize)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var from = pageIndex * pageSize;

            var body = $$"""
            {
                "from": {{from}},
                "size": {{pageSize}},
                "query": {
                    "hybrid": {
                        "queries": [
                            {
                                "neural": {
                                    "embedding": {
                                        "query_text": {{JsonSerializer.Serialize(query)}},
                                        "model_id": {{JsonSerializer.Serialize(_options.ModelId)}},
                                        "k": 100
                                    }
                                }
                            },
                            {
                                "multi_match": {
                                    "query": {{JsonSerializer.Serialize(query)}},
                                    "fields": ["name", "description"]
                                }
                            }
                        ]
                    }
                }
            }
            """;

            var response = await _client.SearchAsync<StringResponse>(
                _options.IndexName,
                PostData.String(body),
                new SearchRequestParameters
                {
                    QueryString =
                    {
                        { "search_pipeline", _options.SearchPipelineName }
                    }
                });

            stopwatch.Stop();

            if (!response.Success)
            {
                _logger.LogError(
                    "OpenSearch search failed for query '{Query}' after {ElapsedMs}ms: {Error}",
                    query, stopwatch.ElapsedMilliseconds, response.OriginalException?.Message ?? "Unknown error");
                return [];
            }

            var results = ParseSearchResults(response.Body);

            _logger.LogInformation(
                "OpenSearch search for '{Query}' returned {ResultCount} results in {ElapsedMs}ms",
                query, results.Count, stopwatch.ElapsedMilliseconds);

            return results;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "OpenSearch search failed for query '{Query}' after {ElapsedMs}ms",
                query, stopwatch.ElapsedMilliseconds);
            return [];
        }
    }

    /// <inheritdoc/>
    public async Task IndexDocumentAsync(CatalogItem item)
    {
        try
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
                    QueryString =
                    {
                        { "pipeline", _options.PipelineName }
                    }
                });

            if (!response.Success)
            {
                _logger.LogError(
                    "Failed to index catalog item {ItemId} '{ItemName}': {Error}",
                    item.Id, item.Name, response.OriginalException?.Message ?? "Unknown error");
                return;
            }

            _logger.LogInformation("Indexed catalog item {ItemId} '{ItemName}'", item.Id, item.Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to index catalog item {ItemId} '{ItemName}'", item.Id, item.Name);
        }
    }

    /// <inheritdoc/>
    public async Task DeleteDocumentAsync(int itemId)
    {
        try
        {
            var response = await _client.DeleteAsync<StringResponse>(
                _options.IndexName,
                itemId.ToString());

            if (!response.Success)
            {
                _logger.LogError(
                    "Failed to delete catalog item {ItemId} from OpenSearch: {Error}",
                    itemId, response.OriginalException?.Message ?? "Unknown error");
                return;
            }

            _logger.LogInformation("Deleted catalog item {ItemId} from OpenSearch", itemId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete catalog item {ItemId} from OpenSearch", itemId);
        }
    }

    private static List<CatalogSearchDocument> ParseSearchResults(string responseBody)
    {
        var results = new List<CatalogSearchDocument>();

        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (!root.TryGetProperty("hits", out var hitsOuter) ||
            !hitsOuter.TryGetProperty("hits", out var hitsArray))
        {
            return results;
        }

        foreach (var hit in hitsArray.EnumerateArray())
        {
            if (!hit.TryGetProperty("_source", out var source))
            {
                continue;
            }

            var item = new CatalogSearchDocument(
                Id: source.GetProperty("id").GetInt32(),
                Name: source.GetProperty("name").GetString()!,
                Description: source.TryGetProperty("description", out var desc) ? desc.GetString() : null,
                Price: source.GetProperty("price").GetDecimal(),
                CatalogBrandId: source.GetProperty("catalogBrandId").GetInt32(),
                CatalogTypeId: source.GetProperty("catalogTypeId").GetInt32(),
                PictureFileName: source.TryGetProperty("pictureFileName", out var pic) ? pic.GetString() : null,
                AvailableStock: source.GetProperty("availableStock").GetInt32());

            results.Add(item);
        }

        return results;
    }
}
