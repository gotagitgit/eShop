using OpenSearch.Net;

namespace SearchServices.Models;

public sealed record OpenSearchResponse(bool IsSuccess, string? Body = null)
{
    public static OpenSearchResponse From(StringResponse response) =>
        new(response.Success, response.Body);
}
