using Common.Domain.Abstractions;

namespace AscentService.Domain.DeletedUsers;

public sealed class DeletedUser : AggregateRoot
{
    private DeletedUser()
    {
    }

    private DeletedUser(Guid userId, DateTime deletedAtUtc) : base(userId) =>
        DeletedAtUtc = deletedAtUtc;

    public DateTime DeletedAtUtc { get; private set; }

    public static DeletedUser Project(Guid userId, DateTime deletedAtUtc) => new(userId, deletedAtUtc);
}
