using System.ComponentModel.DataAnnotations;

namespace AscentService.Infrastructure.ExternalServices;

public sealed class PeakCatalogOptions
{
    public const string SectionName = "PeakCatalog";

#pragma warning disable S5332 // Motivo: llamada intra-red de Docker; los servicios escuchan HTTP
    // plano en 8080 y el TLS termina en el gateway (DESIGN §3.3).
    [Required]
    public Uri BaseAddress { get; init; } = new("http://peak-service:8080/");
#pragma warning restore S5332

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromSeconds(3);
}
