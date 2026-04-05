using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLTaskResponse(MLTaskState State, string ModelId, string? Error, bool IsSuccess)
{
    public static MLTaskResponse Empty() => new(MLTaskState.Unknown, string.Empty, null, false);

    public static MLTaskResponse Parse(StringResponse response)
    {
        if (response is null || string.IsNullOrEmpty(response.Body) || !response.Success)
            return Empty();

        using var doc = JsonDocument.Parse(response.Body);
        var root = doc.RootElement;

        var state = ParseState(root.GetProperty("state").GetString());

        var modelId = root.TryGetProperty("model_id", out var modelIdElement)
            ? modelIdElement.GetString() ?? string.Empty
            : string.Empty;

        var error = root.TryGetProperty("error", out var errorElement)
            ? errorElement.GetString()
            : null;

        return new MLTaskResponse(state, modelId, error, response.Success);
    }

    private static MLTaskState ParseState(string? state) => state?.ToUpperInvariant() switch
    {
        "COMPLETED" => MLTaskState.Completed,
        "FAILED" => MLTaskState.Failed,
        _ => MLTaskState.Unknown
    };
}
