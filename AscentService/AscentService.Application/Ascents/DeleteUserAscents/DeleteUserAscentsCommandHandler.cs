using AscentService.Domain.Ascents;
using AscentService.Domain.ConfirmedUsers;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.DeleteUserAscents;

internal sealed class DeleteUserAscentsCommandHandler(
    IAscentRepository ascentRepository,
    IConfirmedUserRepository confirmedUserRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<DeleteUserAscentsCommand>
{
    public async Task<Result> Handle(DeleteUserAscentsCommand command, CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByUserIdAsync(command.UserId, cancellationToken);
        ConfirmedUser? confirmedUser =
            await confirmedUserRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        if (ascents.Count == 0 && confirmedUser is null)
        {
            return Result.Success();
        }

        foreach (Ascent ascent in ascents)
        {
            ascent.MarkDeleted();
            ascentRepository.Remove(ascent);
        }

        if (confirmedUser is not null)
        {
            confirmedUserRepository.Remove(confirmedUser);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
