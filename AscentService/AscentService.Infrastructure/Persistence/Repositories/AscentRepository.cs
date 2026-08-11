using AscentService.Domain.Ascents;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class AscentRepository(AscentDbContext context) : IAscentRepository
{
    public Task<Ascent?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        context.Ascents.FirstOrDefaultAsync(ascent => ascent.Id == id, cancellationToken);

    public Task<Ascent?> GetByClientAscentIdAsync(
        Guid userId,
        Guid clientAscentId,
        CancellationToken cancellationToken) =>
        context.Ascents.FirstOrDefaultAsync(
            ascent => ascent.UserId == userId && ascent.ClientAscentId == clientAscentId,
            cancellationToken);

    public async Task<IReadOnlyList<Ascent>> GetByPeakIdAsync(Guid peakId, CancellationToken cancellationToken) =>
        await context.Ascents
            .Where(ascent => ascent.Peak.PeakId == peakId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Ascent>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        await context.Ascents
            .Where(ascent => ascent.UserId == userId)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetKnownPhotoPublicIdsAsync(
        IReadOnlyCollection<string> candidates,
        CancellationToken cancellationToken)
    {
        List<string> known = await context.Ascents
            .SelectMany(ascent => ascent.Photos)
            .Where(photo => candidates.Contains(photo.CloudinaryPublicId))
            .Select(photo => photo.CloudinaryPublicId)
            .ToListAsync(cancellationToken);

        return known.ToHashSet(StringComparer.Ordinal);
    }

    public void Add(Ascent ascent) => context.Ascents.Add(ascent);

    public void Remove(Ascent ascent) => context.Ascents.Remove(ascent);
}
