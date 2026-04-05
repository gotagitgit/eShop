using SearchServices.DTOs;
using SearchServices.Models;

namespace SearchServices.Services.SearchClients;

public interface IOpenSearchClientWrapper
{
    Task<OpenSearchResponse> CheckClusterHealthAsync(CancellationToken cancellationToken);
    Task<OpenSearchResponse> UpdateClusterSettingsAsync(ClusterSettings settings, CancellationToken cancellationToken);
    Task<MLModelGroupResponse> CreateModelGroupAsync(MLModelGroupDto openSearchModelGroup, CancellationToken cancellationToken);
    Task<MLModelGroupResponse> FindModelGroupAsync(string groupName, CancellationToken cancellationToken);
    Task<MLModelRegistrationResponse> RegisterModelAsync(MLModelRegistrationDto modelRegistration, CancellationToken cancellationToken);
    Task<MLModelSearchResponse> FindModelAsync(string name, string modelGroupId, CancellationToken cancellationToken);
    Task<MLTaskResponse> GetTaskStatusAsync(string taskId, CancellationToken cancellationToken);
    Task<MLDeployResponse> DeployModelAsync(string modelId, CancellationToken cancellationToken);
    Task<MLModelStatusResponse> GetModelStatusAsync(string modelId, CancellationToken cancellationToken);
    Task<OpenSearchResponse> GetIngestPipelineAsync(string pipelineName, CancellationToken cancellationToken);
    Task<OpenSearchResponse> CreateIngestPipelineAsync(IngestPipelineConfig config, CancellationToken cancellationToken);
    Task<OpenSearchResponse> GetSearchPipelineAsync(string pipelineName, CancellationToken cancellationToken);
    Task<OpenSearchResponse> CreateSearchPipelineAsync(SearchPipelineConfig config, CancellationToken cancellationToken);
}
