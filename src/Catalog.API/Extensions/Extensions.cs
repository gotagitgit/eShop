using eShop.Catalog.API.Services;
using OpenSearch.Net;

public static class Extensions
{
    public static void AddApplicationServices(this IHostApplicationBuilder builder)
    {
        // Avoid loading full database config and migrations if startup
        // is being invoked from build-time OpenAPI generation
        if (builder.Environment.IsBuild())
        {
            builder.Services.AddDbContext<CatalogContext>();
            return;
        }

        builder.AddNpgsqlDbContext<CatalogContext>("catalogdb");

        // REVIEW: This is done for development ease but shouldn't be here in production
        builder.Services.AddMigration<CatalogContext, CatalogContextSeed>();

        // Add the integration services that consume the DbContext
        builder.Services.AddTransient<IIntegrationEventLogService, IntegrationEventLogService<CatalogContext>>();

        builder.Services.AddTransient<ICatalogIntegrationEventService, CatalogIntegrationEventService>();

        builder.AddRabbitMqEventBus("eventbus")
               .AddSubscription<OrderStatusChangedToAwaitingValidationIntegrationEvent, OrderStatusChangedToAwaitingValidationIntegrationEventHandler>()
               .AddSubscription<OrderStatusChangedToPaidIntegrationEvent, OrderStatusChangedToPaidIntegrationEventHandler>();

        builder.Services.AddOptions<CatalogOptions>()
            .BindConfiguration(nameof(CatalogOptions));

        // OpenSearch configuration
        builder.Services.Configure<OpenSearchOptions>(builder.Configuration.GetSection("OpenSearch"));

        var openSearchOptions = builder.Configuration.GetSection("OpenSearch").Get<OpenSearchOptions>();

        // Aspire injects the endpoint as a connection string; map it to the options if not set directly
        if (string.IsNullOrWhiteSpace(openSearchOptions?.Endpoint))
        {
            var connectionString = builder.Configuration.GetConnectionString("opensearch");
            if (!string.IsNullOrWhiteSpace(connectionString))
            {
                openSearchOptions ??= new OpenSearchOptions();
                openSearchOptions.Endpoint = connectionString;
                builder.Services.PostConfigure<OpenSearchOptions>(o => o.Endpoint = connectionString);
            }
        }

        if (openSearchOptions?.Enabled == true && !string.IsNullOrWhiteSpace(openSearchOptions.Endpoint))
        {
            builder.Services.AddSingleton<OpenSearchLowLevelClient>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<OpenSearchOptions>>().Value;
                var settings = new ConnectionConfiguration(new Uri(options.Endpoint!));
                return new OpenSearchLowLevelClient(settings);
            });

            builder.Services.AddScoped<ICatalogSearch, OpenSearchSearchService>();
            builder.Services.AddHostedService<OpenSearchInitializationService>();
        }
        else
        {
            builder.Services.AddScoped<ICatalogSearch, FallbackCatalogSearch>();
        }
    }
}
