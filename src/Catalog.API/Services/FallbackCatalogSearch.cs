namespace eShop.Catalog.API.Services;

/// <summary>
/// No-op implementation of <see cref="ICatalogSearch"/> used when OpenSearch is not configured.
/// </summary>
public sealed class FallbackCatalogSearch : ICatalogSearch
{
    public bool IsEnabled => false;

    public Task<List<CatalogSearchDocument>> SearchAsync(string query, int pageIndex, int pageSize)
        => Task.FromResult(new List<CatalogSearchDocument>());

    public Task IndexDocumentAsync(CatalogItem item) => Task.CompletedTask;

    public Task DeleteDocumentAsync(int itemId) => Task.CompletedTask;
}
