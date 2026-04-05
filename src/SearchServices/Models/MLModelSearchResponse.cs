using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLModelSearchResponse(string ModelId, bool IsFound)
{
    public static MLModelSearchResponse NotFound() => new(string.Empty, false);

    public static MLModelSearchResponse Parse(StringResponse response)
    {
        if (response is null || string.IsNullOrEmpty(response.Body) || !response.Success)
            return NotFound();

        using var doc = JsonDocument.Parse(response.Body);
        var hits = doc.RootElement.GetProperty("hits").GetProperty("hits");

        for (var i = 0; i < hits.GetArrayLength(); i++)
        {
            var source = hits[i].GetProperty("_source");

            // Skip chunk documents — the actual model doc has no chunk_number
            if (source.TryGetProperty("chunk_number", out _))
                continue;

            var modelId = hits[i].GetProperty("_id").GetString() ?? string.Empty;
            return new MLModelSearchResponse(modelId, true);
        }

        return NotFound();
    }
}
