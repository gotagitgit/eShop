using Microsoft.Extensions.Options;
using OpenSearch.Client;
using OpenSearch.Net;
using SearchServices.Serialization;
using SearchServices.Settings;

namespace SearchServices.Factory;

internal sealed class OpenSearchClientFactory : IOpenSearchClientFactory
{
    private readonly SearchSettings _settings;

    public OpenSearchClientFactory(IOptions<SearchSettings> options)
    {
        _settings = options.Value;
    }

    public IOpenSearchClient Create()
    {
        var pool = new SingleNodeConnectionPool(new Uri(_settings.SearchAddress));
        var settings = new ConnectionSettings(pool,
            sourceSerializer: (builtin, connectionSettings) => SystemTextJsonSerializer.Instance);

#if DEBUG
        settings.EnableDebugMode();
#endif

        return new OpenSearchClient(settings);
    }
}
