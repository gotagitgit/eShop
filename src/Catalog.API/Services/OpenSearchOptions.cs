namespace eShop.Catalog.API.Services;

public sealed class OpenSearchOptions
{
    public string? Endpoint { get; set; }

    public string IndexName { get; set; } = "catalog-items";

    public string PipelineName { get; set; } = "catalog-neural-pipeline";

    public string SearchPipelineName { get; set; } = "catalog-hybrid-search";

    public string ModelId { get; set; } = "huggingface/sentence-transformers/all-MiniLM-L6-v2";

    public int EmbeddingDimension { get; set; } = 384;

    public bool Enabled { get; set; } = true;
}
