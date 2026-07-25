using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.DeleteUserAscents;

internal sealed class DeleteUserAscentsCommandHandler(IAscentRepository ascentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<DeleteUserAscentsCommand>
{
    public async Task<Result> Handle(DeleteUserAscentsCommand command, CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (ascents.Count == 0)
        {
            return Result.Success();
        }

        foreach (Ascent ascent in ascents)
        {
            ascent.MarkDeleted();
            ascentRepository.Remove(ascent);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
