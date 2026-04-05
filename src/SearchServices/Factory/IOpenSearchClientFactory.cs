using OpenSearch.Client;

namespace SearchServices.Factory;

public interface IOpenSearchClientFactory
{
    IOpenSearchClient Create();
}
