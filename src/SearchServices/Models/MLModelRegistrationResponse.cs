using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLModelRegistrationResponse(string TaskId, bool IsSuccess)
{
    public static MLModelRegistrationResponse Empty() => new(string.Empty, false);

    public static MLModelRegistrationResponse Parse(StringResponse response)
    {
        if (response is null || string.IsNullOrEmpty(response.Body))
            return Empty();

        var taskId = string.Empty;

        if (response.Success)
        {
            using var doc = JsonDocument.Parse(response.Body);
            taskId = doc.RootElement.GetProperty("task_id").GetString()
                ?? throw new InvalidOperationException("task_id was null in register response.");
        }

        return new MLModelRegistrationResponse(taskId, response.Success);
    }
}
