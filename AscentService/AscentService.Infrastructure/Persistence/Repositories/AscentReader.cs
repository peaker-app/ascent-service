using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Domain.Ascents;
using Common.Application.Pagination;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class AscentReader(AscentDbContext context) : IAscentReader
{
    public Task<PagedResult<AscentSummaryRow>> ListByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        PaginateAsync(context.Ascents.Where(ascent => ascent.UserId == userId), page, cancellationToken);

    public Task<PagedResult<AscentSummaryRow>> ListPublicByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken) =>
        PaginateAsync(
            context.Ascents.Where(ascent =>
                ascent.UserId == userId && ascent.Visibility == AscentVisibility.Public),
            page,
            cancellationToken);

    private static async Task<PagedResult<AscentSummaryRow>> PaginateAsync(
        IQueryable<Ascent> source,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        IQueryable<Ascent> ordered = source.AsNoTracking()
            .OrderByDescending(ascent => ascent.AscentDate)
            .ThenByDescending(ascent => ascent.Id);

        int totalCount = await source.CountAsync(cancellationToken);

        List<AscentSummaryRow> items = await ordered
            .Skip(page.Skip)
            .Take(page.Size)
            .Select(ascent => new AscentSummaryRow(
                ascent.Id,
                ascent.Peak.PeakId,
                ascent.Peak.Name,
                ascent.Peak.AltitudeMeters,
                ascent.AscentDate,
                ascent.Visibility,
                ascent.Photos
                    .OrderBy(photo => photo.Position)
                    .Select(photo => photo.CloudinaryPublicId)
                    .FirstOrDefault()))
            .ToListAsync(cancellationToken);

        return new PagedResult<AscentSummaryRow>(items, page.Page, page.Size, totalCount);
    }
}
