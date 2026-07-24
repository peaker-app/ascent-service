using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.Mappings;
using AscentService.Domain.Ascents;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.GetAscentById;

internal sealed class GetAscentByIdQueryHandler(
    IAscentRepository ascentRepository,
    IProfileDirectory profileDirectory) : IQueryHandler<GetAscentByIdQuery, AscentResponse>
{
    public async Task<Result<AscentResponse>> Handle(
        GetAscentByIdQuery query,
        CancellationToken cancellationToken)
    {
        Ascent? ascent = await ascentRepository.GetByIdAsync(query.AscentId, cancellationToken);

        if (ascent is null || !ascent.IsVisibleTo(query.RequesterId))
        {
            return Result.Failure<AscentResponse>(AscentErrors.NotFound(query.AscentId));
        }

        if (query.RequesterId == ascent.UserId)
        {
            return ascent.ToResponse();
        }

        Result ownerIsReachable = await EnsureOwnerProfileIsPublicAsync(ascent, cancellationToken);

        return ownerIsReachable.IsSuccess
            ? ascent.ToResponse()
            : Result.Failure<AscentResponse>(ownerIsReachable.Error);
    }

    private async Task<Result> EnsureOwnerProfileIsPublicAsync(Ascent ascent, CancellationToken cancellationToken)
    {
        Result<bool> profileIsPublic = await profileDirectory.IsProfilePublicAsync(ascent.UserId, cancellationToken);

        if (profileIsPublic.IsFailure)
        {
            return Result.Failure(profileIsPublic.Error);
        }

        return profileIsPublic.Value
            ? Result.Success()
            : Result.Failure(AscentErrors.NotFound(ascent.Id));
    }
}
