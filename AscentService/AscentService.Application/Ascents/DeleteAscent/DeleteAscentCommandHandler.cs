using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.DeleteAscent;

internal sealed class DeleteAscentCommandHandler(IAscentRepository ascentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteAscentCommand>
{
    public async Task<Result> Handle(DeleteAscentCommand command, CancellationToken cancellationToken)
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

        ascent.MarkDeleted();
        ascentRepository.Remove(ascent);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
