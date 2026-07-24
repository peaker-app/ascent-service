using AscentService.Application.Abstractions;
using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.ListMyAscents;

internal sealed class ListMyAscentsQueryHandler(IAscentReader ascentReader)
    : IQueryHandler<ListMyAscentsQuery, PagedResult<AscentSummaryResponse>>
{
    public async Task<Result<PagedResult<AscentSummaryResponse>>> Handle(
        ListMyAscentsQuery query,
        CancellationToken cancellationToken)
    {
        Result validation = query.Page.Validate();

        return validation.IsFailure
            ? Result.Failure<PagedResult<AscentSummaryResponse>>(validation.Error)
            : await ascentReader.ListByUserAsync(query.UserId, query.Page, cancellationToken);
    }
}
