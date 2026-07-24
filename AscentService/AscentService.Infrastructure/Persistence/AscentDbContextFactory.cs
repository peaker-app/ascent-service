using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AscentService.Infrastructure.Persistence;

internal sealed class AscentDbContextFactory : IDesignTimeDbContextFactory<AscentDbContext>
{
    private const string DesignTimeConnectionString =
        "Server=localhost;Port=5434;Database=peaker_ascents;Username=peaker;Password=peaker";

    public AscentDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AscentDbContext> options = new DbContextOptionsBuilder<AscentDbContext>()
            .UseNpgsql(DesignTimeConnectionString)
            .Options;

        return new AscentDbContext(options);
    }
}
