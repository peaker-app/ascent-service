using AscentService.Domain.Ascents;
using AscentService.Domain.DeletedUsers;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.SweepDeletedUsers;

internal sealed class SweepDeletedUsersCommandHandler(
    IAscentRepository ascentRepository,
    IDeletedUserRepository deletedUserRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<SweepDeletedUsersCommand, DeletedUserSweepResponse>
{
    public async Task<Result<DeletedUserSweepResponse>> Handle(
        SweepDeletedUsersCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<Guid> userIds =
            await deletedUserRepository.ListUserIdsAsync(command.MaxUsers, cancellationToken);

        int removed = 0;

        foreach (Guid userId in userIds)
        {
            removed += await RemoveLeftoversAsync(userId, cancellationToken);
        }

        if (removed > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new DeletedUserSweepResponse(userIds.Count, removed);
    }

    private async Task<int> RemoveLeftoversAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByUserIdAsync(userId, cancellationToken);

        foreach (Ascent ascent in ascents)
        {
            ascent.MarkDeleted();
            ascentRepository.Remove(ascent);
        }

        return ascents.Count;
    }
}
