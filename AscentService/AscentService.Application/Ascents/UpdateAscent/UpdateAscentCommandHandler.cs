using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.UpdateAscent;

internal sealed class UpdateAscentCommandHandler(
    IAscentRepository ascentRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<UpdateAscentCommand>
{
    public async Task<Result> Handle(UpdateAscentCommand command, CancellationToken cancellationToken)
    {
        Ascent? ascent = await ascentRepository.GetByIdAsync(command.AscentId, cancellationToken);

        if (ascent is null || !ascent.IsOwnedBy(command.UserId))
        {
            return Result.Failure(AscentErrors.NotFound(command.AscentId));
        }

        Result amendment = ascent.Amend(command.ToDetails(), dateTimeProvider.Today);

        if (amendment.IsFailure)
        {
            return amendment;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
