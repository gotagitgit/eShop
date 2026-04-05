namespace SearchServices.Models;

public sealed record MLModelRegistration(
    string Name,
    string Version,
    string ModelFormat,
    string ModelGroupId,
    string? ModelType = null,
    int? EmbeddingDimension = null,
    string? FrameworkType = null,
    string? Url = null,
    string? ModelContentHashValue = null);
