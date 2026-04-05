using System.Text.Json;
using OpenSearch.Net;

namespace SearchServices.Serialization;

internal sealed class SystemTextJsonSerializer : IOpenSearchSerializer
{
    public static readonly SystemTextJsonSerializer Instance = new();

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public T Deserialize<T>(Stream stream)
    {
        return JsonSerializer.Deserialize<T>(stream, Options)!;
    }

    public object Deserialize(Type type, Stream stream)
    {
        return JsonSerializer.Deserialize(stream, type, Options)!;
    }

    public async Task<T> DeserializeAsync<T>(Stream stream, CancellationToken cancellationToken = default)
    {
        var result = await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
        return result!;
    }

    public async Task<object> DeserializeAsync(Type type, Stream stream, CancellationToken cancellationToken = default)
    {
        return (await JsonSerializer.DeserializeAsync(stream, type, Options, cancellationToken))!;
    }

    public void Serialize<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None)
    {
        JsonSerializer.Serialize(stream, data, Options);
    }

    public Task SerializeAsync<T>(T data, Stream stream, SerializationFormatting formatting = SerializationFormatting.None, CancellationToken cancellationToken = default)
    {
        return JsonSerializer.SerializeAsync(stream, data, Options, cancellationToken);
    }
}
