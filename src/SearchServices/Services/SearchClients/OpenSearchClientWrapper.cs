using OpenSearch.Client;
using OpenSearch.Net;
using SearchServices.Models;

namespace SearchServices.Services.SearchClients;

internal sealed partial class OpenSearchClientWrapper : IOpenSearchClientWrapper
{
    private readonly IOpenSearchClient _client;
    private IOpenSearchLowLevelClient LowLevel => _client.LowLevel;

    public OpenSearchClientWrapper(IOpenSearchClient client)
    {
        _client = client;
    }

    public async Task<OpenSearchResponse> CheckClusterHealthAsync(CancellationToken cancellationToken)
    {
        var response = await _client.Cluster.HealthAsync(ct: cancellationToken);
        return new OpenSearchResponse(response.IsValid, response.ApiCall?.HttpStatusCode ?? 0);
    }

    public async Task<Models.OpenSearchResponse> UpdateClusterSettingsAsync(
        ClusterSettings settings,
        CancellationToken cancellationToken)
    {
        var response = await _client.Cluster.PutSettingsAsync(s =>
            s.Persistent(d =>
            {
                foreach (var kvp in settings.Persistent)
                    d.Add(kvp.Key, kvp.Value);
                return d;
            }),
            cancellationToken);

        return new OpenSearchResponse(response.IsValid, response.ApiCall?.HttpStatusCode ?? 0, response.DebugInformation);
    }

    public async Task<Models.OpenSearchResponse> GetIngestPipelineAsync(
        string pipelineName,
        CancellationToken cancellationToken)
    {
        var response = await _client.Ingest.GetPipelineAsync(g => g.Id(pipelineName), cancellationToken);
        return new OpenSearchResponse(response.IsValid, response.ApiCall?.HttpStatusCode ?? 0);
    }
}
