using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLDeployResponse(string TaskId, bool IsSuccess)
{
    public static MLDeployResponse Empty() => new(string.Empty, false);

    public static MLDeployResponse Parse(StringResponse response)
    {
        if (response is null || string.IsNullOrEmpty(response.Body) || !response.Success)
            return Empty();

        using var doc = JsonDocument.Parse(response.Body);
        var taskId = doc.RootElement.GetProperty("task_id").GetString()
            ?? throw new InvalidOperationException("task_id was null in deploy response.");

        return new MLDeployResponse(taskId, response.Success);
    }
}
