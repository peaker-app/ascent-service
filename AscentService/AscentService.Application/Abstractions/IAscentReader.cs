using AscentService.Application.Ascents.ListMyAscents;
using Common.Application.Pagination;

namespace AscentService.Application.Abstractions;

public interface IAscentReader
{
    Task<PagedResult<AscentSummaryResponse>> ListByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken);

    Task<PagedResult<AscentSummaryResponse>> ListPublicByUserAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken);
}
