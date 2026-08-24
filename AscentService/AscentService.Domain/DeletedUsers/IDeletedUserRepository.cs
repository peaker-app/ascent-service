namespace AscentService.Domain.DeletedUsers;

public interface IDeletedUserRepository
{
    Task<DeletedUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<Guid>> ListUserIdsAsync(int limit, CancellationToken cancellationToken);

    void Add(DeletedUser deletedUser);
}
