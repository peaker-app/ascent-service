namespace AscentService.Domain.Ascents;

public interface IAscentRepository
{
    Task<Ascent?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ascent>> GetByPeakIdAsync(Guid peakId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Ascent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(Ascent ascent);

    void Remove(Ascent ascent);
}
