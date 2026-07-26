using AscentService.Domain.ConfirmedUsers;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence.Repositories;

internal sealed class ConfirmedUserRepository(AscentDbContext context) : IConfirmedUserRepository
{
    public Task<ConfirmedUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken) =>
        context.ConfirmedUsers.FirstOrDefaultAsync(confirmedUser => confirmedUser.Id == userId, cancellationToken);

    public void Add(ConfirmedUser confirmedUser) => context.ConfirmedUsers.Add(confirmedUser);

    public void Remove(ConfirmedUser confirmedUser) => context.ConfirmedUsers.Remove(confirmedUser);
}
