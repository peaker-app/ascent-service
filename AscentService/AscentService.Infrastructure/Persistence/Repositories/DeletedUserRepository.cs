using AscentService.Domain.DeletedUsers;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class DeletedUserRepository(AscentDbContext context) : IDeletedUserRepository
{
    public Task<DeletedUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.DeletedUsers.FirstOrDefaultAsync(deletedUser => deletedUser.Id == userId, cancellationToken);

    public async Task<IReadOnlyCollection<Guid>> ListUserIdsAsync(int limit, CancellationToken cancellationToken) =>
        await context.DeletedUsers
            .AsNoTracking()
            .OrderBy(deletedUser => deletedUser.DeletedAtUtc)
            .Select(deletedUser => deletedUser.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);

    public void Add(DeletedUser deletedUser) => context.DeletedUsers.Add(deletedUser);
}
