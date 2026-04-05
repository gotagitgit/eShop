namespace SearchServices.Models;

public sealed record MLModelRegistrationResult(string? ModelId, string? TaskId, bool AlreadyExists)
{
    public bool IsSuccess => AlreadyExists || !string.IsNullOrEmpty(TaskId);

    public static MLModelRegistrationResult Existing(string modelId) => new(modelId, null, true);
    public static MLModelRegistrationResult Registered(string taskId) => new(null, taskId, false);
    public static MLModelRegistrationResult Failed() => new(null, null, false);
}
