using AscentService.Application.Ascents.ListMyAscents;
using Common.Application.Pagination;

namespace AscentService.Application.Abstractions;

public interface IAscentReader
{
    Task<PagedResult<AscentSummaryRow>> ListByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<AscentSummaryRow>> ListPublicByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken);
}
