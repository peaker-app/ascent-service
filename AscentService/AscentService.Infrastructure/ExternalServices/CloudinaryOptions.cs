using System.ComponentModel.DataAnnotations;

namespace AscentService.Infrastructure.ExternalServices;

public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";

    [Required]
    public string CloudName { get; init; } = string.Empty;

    [Required]
    public string ApiKey { get; init; } = string.Empty;

    [Required]
    public string ApiSecret { get; init; } = string.Empty;

    [Required]
    public string Folder { get; init; } = "peaker/dev/ascents";
}
