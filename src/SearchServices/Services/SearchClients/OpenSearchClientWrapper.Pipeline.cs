using OpenSearch.Net;
using SearchServices.Models;
using HttpMethod = OpenSearch.Net.HttpMethod;

namespace SearchServices.Services.SearchClients;

internal partial class OpenSearchClientWrapper
{
    public async Task<OpenSearchResponse> CreateIngestPipelineAsync(
        IngestPipelineConfig config,
        CancellationToken cancellationToken)
    {
        var body = new
        {
            description = "Pipeline for generating catalog item embeddings",
            processors = new object[]
            {
                new
                {
                    text_embedding = new
                    {
                        model_id = config.ModelId,
                        field_map = new Dictionary<string, string>
                        {
                            [config.SourceField] = config.DestinationField
                        }
                    }
                }
            }
        };

        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.PUT,
            $"/_ingest/pipeline/{config.PipelineName}",
            cancellationToken,
            PostData.Serializable(body));

        return OpenSearchResponse.From(response);
    }

    public async Task<Models.OpenSearchResponse> GetSearchPipelineAsync(
        string pipelineName,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.GET,
            $"/_search/pipeline/{pipelineName}",
            cancellationToken);

        return OpenSearchResponse.From(response);
    }

    public async Task<OpenSearchResponse> CreateSearchPipelineAsync(
        SearchPipelineConfig config,
        CancellationToken cancellationToken)
    {
        var body = new Dictionary<string, object>
        {
            ["description"] = "Search pipeline for hybrid score normalization",
            ["phase_results_processors"] = new object[]
            {
                new Dictionary<string, object>
                {
                    ["normalization-processor"] = new
                    {
                        normalization = new { technique = config.NormalizationTechnique },
                        combination = new
                        {
                            technique = config.CombinationTechnique,
                            parameters = new { weights = config.Weights }
                        }
                    }
                }
            }
        };

        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.PUT,
            $"/_search/pipeline/{config.PipelineName}",
            cancellationToken,
            PostData.Serializable(body));

        return OpenSearchResponse.From(response);
    }
}
