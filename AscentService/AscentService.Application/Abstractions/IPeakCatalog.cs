using AscentService.Domain.Ascents;
using Common.Domain.Results;

namespace AscentService.Application.Abstractions;

public interface IPeakCatalog
{
    Task<Result<PeakSnapshot>> GetSnapshotAsync(Guid peakId, CancellationToken cancellationToken);
}
