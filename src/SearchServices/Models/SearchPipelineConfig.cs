namespace SearchServices.Models;

public sealed record SearchPipelineConfig(
    string PipelineName,
    string NormalizationTechnique,
    string CombinationTechnique,
    double[] Weights);
