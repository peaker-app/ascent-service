using System.ComponentModel.DataAnnotations;

namespace AscentService.Infrastructure.ExternalServices;

public sealed class PeakCatalogOptions
{
    public const string SectionName = "PeakCatalog";

    [Required]
    public Uri BaseAddress { get; init; } = new("http://peak-service:8080/");

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(3);
}
