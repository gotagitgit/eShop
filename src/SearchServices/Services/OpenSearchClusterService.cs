using SearchServices.Models;
using SearchServices.Services.SearchClients;

namespace SearchServices.Services;

internal sealed class OpenSearchClusterService : IOpenSearchClusterService
{
    private readonly IOpenSearchClientWrapper _wrapper;

    public OpenSearchClusterService(IOpenSearchClientWrapper wrapper)
    {
        _wrapper = wrapper;
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        var result = await _wrapper.CheckClusterHealthAsync(cancellationToken);
        return result.IsSuccess;
    }

    public async Task<OpenSearchResponse> UpdateSettingsAsync(
        ClusterSettings settings,
        CancellationToken cancellationToken)
    {
        return await _wrapper.UpdateClusterSettingsAsync(settings, cancellationToken);
    }

    public async Task<OpenSearchResponse> EnsureIngestPipelineAsync(
        IngestPipelineConfig config,
        CancellationToken cancellationToken)
    {
        var existing = await _wrapper.GetIngestPipelineAsync(config.PipelineName, cancellationToken);
        if (existing.IsSuccess)
            return existing;

        return await _wrapper.CreateIngestPipelineAsync(config, cancellationToken);
    }

    public async Task<OpenSearchResponse> EnsureSearchPipelineAsync(
        SearchPipelineConfig config,
        CancellationToken cancellationToken)
    {
        var existing = await _wrapper.GetSearchPipelineAsync(config.PipelineName, cancellationToken);
        if (existing.IsSuccess)
            return existing;

        return await _wrapper.CreateSearchPipelineAsync(config, cancellationToken);
    }
}
