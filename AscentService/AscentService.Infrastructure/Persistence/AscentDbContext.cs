using AscentService.Domain.Ascents;
using AscentService.Domain.ConfirmedUsers;
using AscentService.Domain.DeletedUsers;
using Common.Application.Abstractions;
using Common.Infrastructure.Persistence.Idempotency;
using Common.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence;

public sealed class AscentDbContext(DbContextOptions<AscentDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Ascent> Ascents => Set<Ascent>();

    public DbSet<ConfirmedUser> ConfirmedUsers => Set<ConfirmedUser>();

    public DbSet<DeletedUser> DeletedUsers => Set<DeletedUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AscentDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedMessageConfiguration());
    }
}
