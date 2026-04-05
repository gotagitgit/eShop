using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record MLModelGroupResponse(string Id, bool IsSuccess)
{
    private const string ModelGroupId = "model_group_id";

    public static MLModelGroupResponse Empty() => new(string.Empty, false);

    public static MLModelGroupResponse Parse(StringResponse response)
    {
        return Parse(response, static root =>
            root.GetProperty(ModelGroupId).GetString()
                ?? throw new InvalidOperationException("model_group_id was null in response."));
    }

    public static MLModelGroupResponse ParseByHits(StringResponse response)
    {
        return Parse(response, static root =>
        {
            var hits = root.GetProperty("hits").GetProperty("hits");

            return hits.GetArrayLength() > 0
                ? hits[0].GetProperty("_id").GetString() ?? string.Empty
                : string.Empty;
        });
    }

    private static MLModelGroupResponse Parse(StringResponse response, Func<JsonElement, string> extractId)
    {
        if (response is null || string.IsNullOrEmpty(response.Body))
            return Empty();

        var id = string.Empty;

        if (response.Success)
        {
            using var doc = JsonDocument.Parse(response.Body);

            id = extractId(doc.RootElement);
        }

        var isSuccess = !string.IsNullOrWhiteSpace(id);

        return new MLModelGroupResponse(id, isSuccess);
    }
}
