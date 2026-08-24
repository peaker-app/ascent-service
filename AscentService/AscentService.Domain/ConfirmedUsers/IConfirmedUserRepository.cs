namespace AscentService.Domain.ConfirmedUsers;

public interface IConfirmedUserRepository
{
    Task<ConfirmedUser?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken);

    void Add(ConfirmedUser confirmedUser);

    void Remove(ConfirmedUser confirmedUser);
}
