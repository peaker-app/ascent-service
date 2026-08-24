using AscentService.Domain.Ascents;
using AscentService.Domain.ConfirmedUsers;
using AscentService.Domain.DeletedUsers;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.DeleteUserAscents;

internal sealed class DeleteUserAscentsCommandHandler(
    IAscentRepository ascentRepository,
    IConfirmedUserRepository confirmedUserRepository,
    IDeletedUserRepository deletedUserRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<DeleteUserAscentsCommand>
{
    public async Task<Result> Handle(DeleteUserAscentsCommand command, CancellationToken cancellationToken)
    {
        await MarkUserDeletedAsync(command.UserId, cancellationToken);
        await RemoveConfirmationAsync(command.UserId, cancellationToken);
        await RemoveAscentsAsync(command.UserId, cancellationToken);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }

    private async Task MarkUserDeletedAsync(Guid userId, CancellationToken cancellationToken)
    {
        DeletedUser? tombstone = await deletedUserRepository.GetByUserIdAsync(userId, cancellationToken);

        if (tombstone is null)
        {
            deletedUserRepository.Add(DeletedUser.Project(userId, dateTimeProvider.UtcNow));
        }
    }

    private async Task RemoveConfirmationAsync(Guid userId, CancellationToken cancellationToken)
    {
        ConfirmedUser? confirmedUser = await confirmedUserRepository.GetByUserIdAsync(userId, cancellationToken);

        if (confirmedUser is not null)
        {
            confirmedUserRepository.Remove(confirmedUser);
        }
    }

    private async Task RemoveAscentsAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByUserIdAsync(userId, cancellationToken);

        foreach (Ascent ascent in ascents)
        {
            ascent.MarkDeleted();
            ascentRepository.Remove(ascent);
        }
    }
}
