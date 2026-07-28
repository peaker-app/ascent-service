using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AscentService.Infrastructure.Persistence;

internal sealed class AscentDbContextFactory : IDesignTimeDbContextFactory<AscentDbContext>
{
    private const string ConnectionStringVariable = "ConnectionStrings__AscentDatabase";

    private const string ModelOnlyConnectionString =
        "Server=localhost;Port=5434;Database=peaker_ascents";

    public AscentDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<AscentDbContext> options = new DbContextOptionsBuilder<AscentDbContext>()
            .UseNpgsql(ResolveConnectionString())
            .Options;

        return new AscentDbContext(options);
    }

    private static string ResolveConnectionString()
    {
        string? configured = Environment.GetEnvironmentVariable(ConnectionStringVariable);

        return string.IsNullOrWhiteSpace(configured)
            ? ModelOnlyConnectionString
            : configured;
    }
}
