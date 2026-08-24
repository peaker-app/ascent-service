using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Application.Ascents.ListUserAscents;
using Common.Application.Pagination;

namespace AscentService.API.Requests;

public sealed record ListAscentsRequest(int Page = PageRequest.MinPage, int Size = PageRequest.DefaultSize)
{
    public ListMyAscentsQuery ToQuery(Guid userId) => new(userId, new PageRequest(Page, Size));

    public ListUserAscentsQuery ToQuery(Guid targetUserId, Guid? requesterId) =>
        new(targetUserId, requesterId, new PageRequest(Page, Size));
}
