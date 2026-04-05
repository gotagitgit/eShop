namespace eShop.Catalog.API.Services;

public interface ICatalogSearch
{
    /// <summary>Gets whether the OpenSearch semantic search is enabled.</summary>
    bool IsEnabled { get; }

    /// <summary>Searches the catalog using hybrid (neural + keyword) search.</summary>
    Task<List<CatalogSearchDocument>> SearchAsync(string query, int pageIndex, int pageSize);

    /// <summary>Indexes a catalog item into the OpenSearch index.</summary>
    Task IndexDocumentAsync(CatalogItem item);

    /// <summary>Deletes a catalog item document from the OpenSearch index.</summary>
    Task DeleteDocumentAsync(int itemId);
}
