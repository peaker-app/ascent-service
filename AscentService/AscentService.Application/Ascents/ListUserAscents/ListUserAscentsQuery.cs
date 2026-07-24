using AscentService.Application.Ascents.ListMyAscents;
using Common.Application.Messaging;
using Common.Application.Pagination;

namespace AscentService.Application.Ascents.ListUserAscents;

public sealed record ListUserAscentsQuery(Guid TargetUserId, Guid? RequesterId, PageRequest Page)
    : IQuery<PagedResult<AscentSummaryResponse>>;
