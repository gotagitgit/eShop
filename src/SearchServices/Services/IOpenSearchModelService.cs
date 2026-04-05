using SearchServices.Models;

namespace SearchServices.Services;

public interface IOpenSearchModelService
{
    Task<MLModelGroupResponse> CreateModelGroupAsync(MLModelGroup openSearchModelGroup, CancellationToken cancellationToken);
    Task<MLModelRegistrationResult> RegisterModelAsync(MLModelRegistration modelRegistration, CancellationToken cancellationToken);
    Task<MLTaskResponse> PollTaskAsync(string taskId, CancellationToken cancellationToken);
    Task<MLDeployResponse> DeployModelAsync(string modelId, CancellationToken cancellationToken);
}