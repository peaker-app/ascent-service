using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;
using Microsoft.Extensions.Logging;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class PeakCatalogHttpClient(HttpClient httpClient, ILogger<PeakCatalogHttpClient> logger)
    : IPeakCatalog
{
    public async Task<Result<PeakSnapshot>> GetSnapshotAsync(Guid peakId, CancellationToken cancellationToken)
    {
        Uri route = new(string.Create(CultureInfo.InvariantCulture, $"api/peaks/{peakId}"), UriKind.Relative);

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(route, cancellationToken);

            return response.StatusCode is HttpStatusCode.NotFound
                ? Result.Failure<PeakSnapshot>(AscentErrors.PeakNotFound(peakId))
                : await ReadSnapshotAsync(response, peakId, cancellationToken);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "The peak catalog is unreachable while resolving {PeakId}", peakId);

            return Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable);
        }
    }

    private static async Task<Result<PeakSnapshot>> ReadSnapshotAsync(
        HttpResponseMessage response,
        Guid peakId,
        CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            return Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable);
        }

        PeakCatalogResource? peak =
            await response.Content.ReadFromJsonAsync<PeakCatalogResource>(cancellationToken);

        return peak is null
            ? Result.Failure<PeakSnapshot>(AscentErrors.PeakNotFound(peakId))
            : new PeakSnapshot(peak.Id, peak.Name, peak.AltitudeMeters);
    }
}
