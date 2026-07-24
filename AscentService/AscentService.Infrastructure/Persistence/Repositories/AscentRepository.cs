using AscentService.Domain.Ascents;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class AscentRepository(AscentDbContext context) : IAscentRepository
{
    public Task<Ascent?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Ascents.FirstOrDefaultAsync(ascent => ascent.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Ascent>> GetByPeakIdAsync(Guid peakId, CancellationToken cancellationToken) =>
        await context.Ascents
            .Where(ascent => ascent.Peak.PeakId == peakId)
            .ToListAsync(cancellationToken);

    public void Add(Ascent ascent) => context.Ascents.Add(ascent);

    public void Remove(Ascent ascent) => context.Ascents.Remove(ascent);
}
