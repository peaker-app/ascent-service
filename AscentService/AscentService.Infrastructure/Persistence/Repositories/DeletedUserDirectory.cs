using AscentService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class DeletedUserDirectory(AscentDbContext context) : IDeletedUserDirectory
{
    public Task<bool> IsDeletedAsync(Guid userId, CancellationToken cancellationToken) =>
        context.DeletedUsers
            .AsNoTracking()
            .AnyAsync(deletedUser => deletedUser.Id == userId, cancellationToken);
}
