using System.ComponentModel.DataAnnotations;

namespace AscentService.Infrastructure.ExternalServices;

public sealed class ProfileDirectoryOptions
{
    public const string SectionName = "ProfileDirectory";

    [Required]
    public Uri BaseAddress { get; init; } = new("http://account-service:8080/");

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(3);
}
