using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.Mappings;
using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.ListMyAscents;

internal sealed class ListMyAscentsQueryHandler(IAscentReader ascentReader, IPhotoUrlSigner photoUrlSigner)
    : IQueryHandler<ListMyAscentsQuery, PagedResult<AscentSummaryResponse>>
{
    public async Task<Result<PagedResult<AscentSummaryResponse>>> Handle(
        ListMyAscentsQuery query,
        CancellationToken cancellationToken)
    {
        Result validation = query.Page.Validate();

        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<AscentSummaryResponse>>(validation.Error);
        }

        PagedResult<AscentSummaryRow> page =
            await ascentReader.ListByUserAsync(query.UserId, query.Page, cancellationToken);

        return page.ToResponse(photoUrlSigner);
    }
}
