using Microsoft.Extensions.DependencyInjection;
using OpenSearch.Client;
using OpenSearch.Net;
using SearchServices.Factory;
using SearchServices.Services;
using SearchServices.Services.SearchClients;

namespace SearchServices;

public static class SearchServiceDependencyInjection
{
    public static IServiceCollection Register(this IServiceCollection services)
    {
        RegisterSearchClients(services);
        RegisterServices(services);
        RegisterFactories(services);

        return services;
    }

    private static void RegisterSearchClients(this IServiceCollection services)
    {
        services.AddScoped(ioc =>
        {
            var client = ioc.GetRequiredService<IOpenSearchClientFactory>().Create();
            return client;
        });
        services.AddScoped(ioc => ioc.GetRequiredService<IOpenSearchClient>().LowLevel);
        services.AddScoped<IOpenSearchClientWrapper, OpenSearchClientWrapper>();
    }

    private static void RegisterServices(this IServiceCollection services)
    {
        services.AddScoped<IOpenSearchModelService, OpenSearchModelService>();
        services.AddScoped<IOpenSearchClusterService, OpenSearchClusterService>();
    }

    private static void RegisterFactories(this IServiceCollection services)
    {
        services.AddScoped<IOpenSearchClientFactory, OpenSearchClientFactory>();
    }
}
