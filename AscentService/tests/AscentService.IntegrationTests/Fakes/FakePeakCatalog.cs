using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakePeakCatalog : IPeakCatalog
{
    private readonly Dictionary<Guid, PeakSnapshot> _peaks = [];

    public bool IsDown { get; set; }

    public PeakSnapshot Register(string name = "Aneto", int altitudeMeters = 3404)
    {
        PeakSnapshot peak = new(Guid.CreateVersion7(), name, altitudeMeters);
        _peaks[peak.PeakId] = peak;

        return peak;
    }

    public Task<Result<PeakSnapshot>> GetSnapshotAsync(Guid peakId, CancellationToken cancellationToken)
    {
        if (IsDown)
        {
            return Task.FromResult(Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable));
        }

        return Task.FromResult(_peaks.TryGetValue(peakId, out PeakSnapshot? peak)
            ? Result.Success(peak)
            : Result.Failure<PeakSnapshot>(AscentErrors.PeakNotFound(peakId)));
    }
}
