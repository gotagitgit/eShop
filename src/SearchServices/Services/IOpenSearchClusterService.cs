using SearchServices.Models;

namespace SearchServices.Services;

public interface IOpenSearchClusterService
{
    Task<bool> CheckHealthAsync(CancellationToken cancellationToken);
    Task<OpenSearchResponse> UpdateSettingsAsync(ClusterSettings settings, CancellationToken cancellationToken);
    Task<OpenSearchResponse> EnsureIngestPipelineAsync(IngestPipelineConfig config, CancellationToken cancellationToken);
    Task<OpenSearchResponse> EnsureSearchPipelineAsync(SearchPipelineConfig config, CancellationToken cancellationToken);
}
