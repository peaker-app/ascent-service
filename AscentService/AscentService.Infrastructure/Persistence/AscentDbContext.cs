using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Infrastructure.Persistence.Idempotency;
using Common.Infrastructure.Persistence.Outbox;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Persistence;

public sealed class AscentDbContext(DbContextOptions<AscentDbContext> options) : DbContext(options), IUnitOfWork
{
    public DbSet<Ascent> Ascents => Set<Ascent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AscentDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
        modelBuilder.ApplyConfiguration(new ProcessedMessageConfiguration());
    }
}
