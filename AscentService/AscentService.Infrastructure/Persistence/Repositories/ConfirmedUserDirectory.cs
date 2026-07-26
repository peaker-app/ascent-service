using AscentService.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class ConfirmedUserDirectory(AscentDbContext context) : IConfirmedUserDirectory
{
    public Task<bool> IsConfirmedAsync(Guid userId, CancellationToken cancellationToken) =>
        context.ConfirmedUsers
            .AsNoTracking()
            .AnyAsync(confirmedUser => confirmedUser.Id == userId, cancellationToken);
}
