using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakePeakCatalog : IPeakCatalog
{
    private readonly Dictionary<Guid, PeakSnapshot> _peaks = [];

    private int _lookups;

    public bool IsDown { get; set; }

    public int Lookups => Volatile.Read(ref _lookups);

    public void ResetLookups() => Volatile.Write(ref _lookups, 0);

    public PeakSnapshot Register(string name = "Aneto", int altitudeMeters = 3404)
    {
        PeakSnapshot peak = new(Guid.CreateVersion7(), name, altitudeMeters);
        _peaks[peak.PeakId] = peak;

        return peak;
    }

    public Task<Result<PeakSnapshot>> GetSnapshotAsync(Guid peakId, CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _lookups);

        if (IsDown)
        {
            return Task.FromResult(Result.Failure<PeakSnapshot>(AscentErrors.PeakCatalogUnavailable));
        }

        return Task.FromResult(_peaks.TryGetValue(peakId, out PeakSnapshot? peak)
            ? Result.Success(peak)
            : Result.Failure<PeakSnapshot>(AscentErrors.PeakNotFound(peakId)));
    }
}
