using SearchServices.DTOs;
using SearchServices.Models;

namespace SearchServices.Extensions;

internal static class MLModelGroupExtensions
{
    public static MLModelGroupDto ToDto(this MLModelGroup modelGroup) => new(modelGroup.Name, modelGroup.Description);

    public static MLModelRegistrationDto ToDto(this MLModelRegistration registration) => new(
        registration.Name,
        registration.Version,
        registration.ModelFormat,
        registration.ModelGroupId,
        registration.ModelType is not null
            ? new MLModelConfigDto(registration.ModelType, registration.EmbeddingDimension!.Value, registration.FrameworkType!)
            : null,
        registration.Url,
        registration.ModelContentHashValue);
}
