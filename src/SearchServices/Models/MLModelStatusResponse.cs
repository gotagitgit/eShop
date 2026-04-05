using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLModelStatusResponse(string ModelState, bool IsSuccess)
{
    public bool IsDeployed => string.Equals(ModelState, "DEPLOYED", StringComparison.OrdinalIgnoreCase);

    public static MLModelStatusResponse Empty() => new(string.Empty, false);

    public static MLModelStatusResponse Parse(StringResponse response)
    {
        if (response is null || string.IsNullOrEmpty(response.Body) || !response.Success)
            return Empty();

        using var doc = JsonDocument.Parse(response.Body);
        var state = doc.RootElement.TryGetProperty("model_state", out var stateElement)
            ? stateElement.GetString() ?? string.Empty
            : string.Empty;

        return new MLModelStatusResponse(state, response.Success);
    }
}
