using OpenSearch.Net;
using SearchServices.DTOs;
using SearchServices.Models;
using HttpMethod = OpenSearch.Net.HttpMethod;

namespace SearchServices.Services.SearchClients;

internal partial class OpenSearchClientWrapper
{
    public async Task<MLModelGroupResponse> FindModelGroupAsync(
        string groupName,
        CancellationToken cancellationToken)
    {
        var searchRequest = new { query = new { match = new { name = groupName } } };

        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            "/_plugins/_ml/model_groups/_search",
            cancellationToken,
            PostData.Serializable(searchRequest));

        return MLModelGroupResponse.ParseByHits(response);
    }

    public async Task<MLModelGroupResponse> CreateModelGroupAsync(
        MLModelGroupDto openSearchModelGroup,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            "/_plugins/_ml/model_groups/_register",
            cancellationToken,
            PostData.Serializable(openSearchModelGroup));

        return MLModelGroupResponse.Parse(response);
    }

    public async Task<MLModelRegistrationResponse> RegisterModelAsync(
        MLModelRegistrationDto modelRegistration,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            "/_plugins/_ml/models/_register",
            cancellationToken,
            PostData.Serializable(modelRegistration));

        return MLModelRegistrationResponse.Parse(response);
    }

    public async Task<MLModelSearchResponse> FindModelAsync(
        string name,
        string modelGroupId,
        CancellationToken cancellationToken)
    {
        var searchRequest = new
        {
            query = new
            {
                @bool = new
                {
                    must = new object[]
                    {
                        new { match_phrase = new { name = name } },
                        new { term = new { model_group_id = new { value = modelGroupId } } }
                    }
                }
            },
            size = 1
        };

        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            "/_plugins/_ml/models/_search",
            cancellationToken,
            PostData.Serializable(searchRequest));

        return MLModelSearchResponse.Parse(response);
    }

    public async Task<MLTaskResponse> GetTaskStatusAsync(
        string taskId,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.GET,
            $"/_plugins/_ml/tasks/{taskId}",
            cancellationToken);

        return MLTaskResponse.Parse(response);
    }

    public async Task<MLDeployResponse> DeployModelAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.POST,
            $"/_plugins/_ml/models/{modelId}/_deploy",
            cancellationToken);

        return MLDeployResponse.Parse(response);
    }

    public async Task<MLModelStatusResponse> GetModelStatusAsync(
        string modelId,
        CancellationToken cancellationToken)
    {
        var response = await LowLevel.DoRequestAsync<StringResponse>(
            HttpMethod.GET,
            $"/_plugins/_ml/models/{modelId}",
            cancellationToken);

        return MLModelStatusResponse.Parse(response);
    }
}
