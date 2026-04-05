using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record OpenSearchResponse(bool IsSuccess, int StatusCode, string? Body = null)
{
    public static OpenSearchResponse From(StringResponse response) =>
        new(response.HttpStatusCode == 200, response.HttpStatusCode ?? 0, response.Body);
}
