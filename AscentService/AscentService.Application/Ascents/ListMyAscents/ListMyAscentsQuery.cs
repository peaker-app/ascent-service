using Common.Application.Messaging;
using Common.Application.Pagination;

namespace AscentService.Application.Ascents.ListMyAscents;

public sealed record ListMyAscentsQuery(Guid UserId, PageRequest Page)
    : IQuery<PagedResult<AscentSummaryResponse>>;
