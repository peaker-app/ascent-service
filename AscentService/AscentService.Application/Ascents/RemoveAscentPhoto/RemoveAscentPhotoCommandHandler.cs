using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.RemoveAscentPhoto;

internal sealed class RemoveAscentPhotoCommandHandler(IAscentRepository ascentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<RemoveAscentPhotoCommand>
{
    public async Task<Result> Handle(RemoveAscentPhotoCommand command, CancellationToken cancellationToken)
    {
        Ascent? ascent = await ascentRepository.GetByIdAsync(command.AscentId, cancellationToken);

        if (ascent is null)
        {
            return Result.Failure(AscentErrors.NotFound(command.AscentId));
        }

        if (!ascent.IsOwnedBy(command.UserId))
        {
            return Result.Failure(AscentErrors.NotOwned);
        }

        Result removal = ascent.RemovePhoto(command.PhotoId);

        if (removal.IsFailure)
        {
            return removal;
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
