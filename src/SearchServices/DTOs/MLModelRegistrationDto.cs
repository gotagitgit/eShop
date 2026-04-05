using System.Text.Json.Serialization;

namespace SearchServices.DTOs;

public record MLModelRegistrationDto(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("version")] string Version,
    [property: JsonPropertyName("model_format")] string ModelFormat,
    [property: JsonPropertyName("model_group_id")] string ModelGroupId,
    [property: JsonPropertyName("model_config")] MLModelConfigDto? ModelConfig = null,
    [property: JsonPropertyName("url")] string? Url = null,
    [property: JsonPropertyName("model_content_hash_value")] string? ModelContentHashValue = null);

public record MLModelConfigDto(
    [property: JsonPropertyName("model_type")] string ModelType,
    [property: JsonPropertyName("embedding_dimension")] int EmbeddingDimension,
    [property: JsonPropertyName("framework_type")] string FrameworkType);
