using AscentService.Domain.Ascents;
using Common.Application.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;

namespace AscentService.Infrastructure.Persistence;

public sealed class AscentPhotoConcurrencyInterceptor : SaveChangesInterceptor
{
    private const string AscentIdColumn = "ascent_id";
    private const string PositionIndex = "ux_ascent_photo_position";
    private const string UniqueViolation = "23505";

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            TouchOwnersOfChangedPhotos(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        TranslatePositionClash(eventData);
        base.SaveChangesFailed(eventData);
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        TranslatePositionClash(eventData);
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    private static void TranslatePositionClash(DbContextErrorEventData eventData)
    {
        if (eventData.Exception is DbUpdateException { InnerException: PostgresException postgres }
            && postgres.SqlState is UniqueViolation
            && string.Equals(postgres.ConstraintName, PositionIndex, StringComparison.Ordinal))
        {
            throw new ConcurrencyConflictException(eventData.Exception);
        }
    }

    private static void TouchOwnersOfChangedPhotos(DbContext context)
    {
        HashSet<Guid> changedOwners = ChangedOwnerIds(context);

        if (changedOwners.Count is 0)
        {
            return;
        }

        foreach (EntityEntry<Ascent> owner in context.ChangeTracker.Entries<Ascent>())
        {
            if (owner.State is not EntityState.Deleted && changedOwners.Contains(owner.Entity.Id))
            {
                owner.Property(ascent => ascent.UpdatedAtUtc).IsModified = true;
            }
        }
    }

    private static HashSet<Guid> ChangedOwnerIds(DbContext context) =>
    [
        .. context.ChangeTracker.Entries<AscentPhoto>()
            .Where(entry => entry.State is EntityState.Added or EntityState.Deleted or EntityState.Modified)
            .Select(entry => entry.Property(AscentIdColumn).CurrentValue)
            .OfType<Guid>()
    ];
}
