using SearchServices.Extensions;
using SearchServices.Models;
using SearchServices.Services.SearchClients;

namespace SearchServices.Services;

internal sealed class OpenSearchModelService : IOpenSearchModelService
{
    private readonly IOpenSearchClientWrapper _openSearchLowLevelClientWrapper;

    public OpenSearchModelService(IOpenSearchClientWrapper openSearchLowLevelClientWrapper)
    {
        _openSearchLowLevelClientWrapper = openSearchLowLevelClientWrapper;
    }

    public async Task<MLModelGroupResponse> CreateModelGroupAsync(
        MLModelGroup openSearchModelGroup,
        CancellationToken cancellationToken)
    {
        var existing = await _openSearchLowLevelClientWrapper.FindModelGroupAsync(openSearchModelGroup.Name, cancellationToken);

        if (existing.IsSuccess)
        {
            return existing;
        }

        return await _openSearchLowLevelClientWrapper.CreateModelGroupAsync(openSearchModelGroup.ToDto(), cancellationToken);
    }

    public async Task<MLModelRegistrationResult> RegisterModelAsync(
        MLModelRegistration modelRegistration,
        CancellationToken cancellationToken)
    {
        var existing = await _openSearchLowLevelClientWrapper.FindModelAsync(
            modelRegistration.Name, modelRegistration.ModelGroupId, cancellationToken);

        if (existing.IsFound)
        {
            return MLModelRegistrationResult.Existing(existing.ModelId);
        }

        var result = await _openSearchLowLevelClientWrapper.RegisterModelAsync(modelRegistration.ToDto(), cancellationToken);

        return result.IsSuccess
            ? MLModelRegistrationResult.Registered(result.TaskId)
            : MLModelRegistrationResult.Failed();
    }

    public async Task<MLTaskResponse> PollTaskAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        var delay = TimeSpan.FromSeconds(2);
        var maxDelay = TimeSpan.FromSeconds(15);
        const int maxRetries = 30;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            await Task.Delay(delay, cancellationToken);

            var result = await _openSearchLowLevelClientWrapper.GetTaskStatusAsync(taskId, cancellationToken);

            if (result.State is MLTaskState.Completed)
            {
                return result;
            }

            if (result.State is MLTaskState.Failed)
            {
                throw new InvalidOperationException($"Task {taskId} failed: {result.Error ?? "unknown error"}");
            }

            delay = TimeSpan.FromSeconds(Math.Min(delay.TotalSeconds * 1.5, maxDelay.TotalSeconds));
        }

        throw new InvalidOperationException($"Task {taskId} did not complete after {maxRetries} attempts.");
    }

    public async Task<MLDeployResponse> DeployModelAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        var status = await _openSearchLowLevelClientWrapper.GetModelStatusAsync(modelId, cancellationToken);

        if (status.IsDeployed)
        {
            return new MLDeployResponse(string.Empty, true);
        }

        return await _openSearchLowLevelClientWrapper.DeployModelAsync(modelId, cancellationToken);
    }
}
