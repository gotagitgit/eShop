namespace SearchServices.Models;

public sealed record IngestPipelineConfig(
    string PipelineName,
    string ModelId,
    string SourceField,
    string DestinationField);
