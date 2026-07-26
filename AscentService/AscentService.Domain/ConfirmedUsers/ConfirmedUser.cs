using Common.Domain.Abstractions;

namespace AscentService.Domain.ConfirmedUsers;

public sealed class ConfirmedUser : AggregateRoot
{
    private ConfirmedUser()
    {
    }

    private ConfirmedUser(Guid userId, DateTime confirmedAtUtc) : base(userId) =>
        ConfirmedAtUtc = confirmedAtUtc;

    public DateTime ConfirmedAtUtc { get; private set; }

    public static ConfirmedUser Project(Guid userId, DateTime confirmedAtUtc) => new(userId, confirmedAtUtc);
}
