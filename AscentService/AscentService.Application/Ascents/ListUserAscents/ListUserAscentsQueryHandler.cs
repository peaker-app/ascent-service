using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Application.Ascents.Mappings;
using AscentService.Domain.Ascents;
using Common.Application.Messaging;
using Common.Application.Pagination;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.ListUserAscents;

internal sealed class ListUserAscentsQueryHandler(
    IAscentReader ascentReader,
    IProfileDirectory profileDirectory,
    IPhotoUrlSigner photoUrlSigner)
    : IQueryHandler<ListUserAscentsQuery, PagedResult<AscentSummaryResponse>>
{
    public async Task<Result<PagedResult<AscentSummaryResponse>>> Handle(
        ListUserAscentsQuery query,
        CancellationToken cancellationToken)
    {
        Result validation = await ValidateAsync(query, cancellationToken);

        if (validation.IsFailure)
        {
            return Result.Failure<PagedResult<AscentSummaryResponse>>(validation.Error);
        }

        PagedResult<AscentSummaryRow> page =
            await ascentReader.ListPublicByUserAsync(query.TargetUserId, query.Page, cancellationToken);

        return page.ToResponse(photoUrlSigner);
    }

    private async Task<Result> ValidateAsync(ListUserAscentsQuery query, CancellationToken cancellationToken)
    {
        Result page = query.Page.Validate();

        if (page.IsFailure || query.RequesterId == query.TargetUserId)
        {
            return page;
        }

        Result<bool> profileIsPublic =
            await profileDirectory.IsProfilePublicAsync(query.TargetUserId, cancellationToken);

        return profileIsPublic.IsSuccess && profileIsPublic.Value
            ? Result.Success()
            : Result.Failure(AscentErrors.ProfileNotVisible(query.TargetUserId));
    }
}
