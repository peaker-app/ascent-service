using System.Globalization;
using System.Net.Http.Headers;
using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.SweepDeletedUsers;
using AscentService.Application.Ascents.SweepOrphanedPhotos;
using AscentService.Domain.Ascents;
using AscentService.Domain.ConfirmedUsers;
using AscentService.Infrastructure.ExternalServices;
using AscentService.Infrastructure.Persistence;
using AscentService.IntegrationTests.Fakes;
using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using Common.Contracts.Profiles;
using Common.Contracts.Users;
using Common.Domain.Results;
using MassTransit;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;
using Testcontainers.RabbitMq;
using Xunit;

namespace AscentService.IntegrationTests;

public sealed class AscentServiceApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestAuthTokenKey = "00112233445566778899aabbccddeeff";

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("peaker_ascents")
        .WithUsername("peaker")
        .WithPassword("peaker")
        .Build();

    private readonly RabbitMqContainer _rabbitMq = new RabbitMqBuilder("rabbitmq:3-management-alpine").Build();

    private readonly TestTokenSigning _tokenSigning = new();

    internal FakePeakCatalog PeakCatalog { get; } = new();

    internal FakeProfileDirectory ProfileDirectory { get; } = new();

    internal FakePhotoStorage PhotoStorage { get; } = new();

    internal FakePhotoAssetInventory PhotoAssetInventory { get; } = new();

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _tokenSigning.CreateAccessToken(userId));

        return client;
    }

    public HttpClient CreateAdminClient(Guid userId)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", _tokenSigning.CreateAccessToken(userId, [PeakerRoles.Admin]));

        return client;
    }

    public async Task<HttpClient> CreateConfirmedClientAsync(Guid userId)
    {
        await ConfirmUserAsync(userId);

        return CreateAuthenticatedClient(userId);
    }

    public async Task ConfirmUserAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        if (await context.ConfirmedUsers.AnyAsync(confirmed => confirmed.Id == userId))
        {
            return;
        }

        context.ConfirmedUsers.Add(ConfirmedUser.Project(userId, DateTime.UtcNow));
        await context.SaveChangesAsync();
    }

    public async Task<bool> IsUserConfirmedAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        return await context.ConfirmedUsers.AsNoTracking().AnyAsync(confirmed => confirmed.Id == userId);
    }

    public async Task<bool> WaitForUserConfirmationAsync(Guid userId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await IsUserConfirmedAsync(userId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    public async Task PublishUserEmailConfirmedAsync(UserEmailConfirmed message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
    }

    public async Task PublishPeakUpdatedAsync(PeakUpdated message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
    }

    public async Task PublishPeakRenamedAsync(PeakRenamed message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
    }

    public async Task PublishProfileUpdatedAsync(ProfileUpdated message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
    }

    public async Task PublishUserDeletedAsync(UserDeleted message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
    }

    public async Task<IReadOnlyList<string>> ReadPhotoPublicIdsAsync(Guid ascentId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        return await context.Ascents
            .AsNoTracking()
            .Where(ascent => ascent.Id == ascentId)
            .SelectMany(ascent => ascent.Photos.Select(photo => photo.CloudinaryPublicId))
            .ToListAsync();
    }

    public async Task<Guid> InsertAscentDirectlyAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        PeakSnapshot peak = PeakCatalog.Register("Aneto", 3404);
        AscentDraft draft = new(
            userId,
            peak,
            new AscentDetails(new DateOnly(2026, 7, 1), null, null, AscentConditions.Unreported,
                AscentVisibility.Public),
            null);

        Ascent ascent = Ascent.Create(draft, new DateOnly(2026, 12, 31)).Value;
        context.Ascents.Add(ascent);
        await context.SaveChangesAsync();

        return ascent.Id;
    }

    internal async Task<DeletedUserSweepResponse> SweepDeletedUsersAsync()
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<DeletedUserSweepResponse> result = await sender.Send(new SweepDeletedUsersCommand(100));

        return result.Value;
    }

    public async Task<bool> WaitForUserTombstoneAsync(Guid userId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await IsUserTombstonedAsync(userId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    public async Task<bool> IsUserTombstonedAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        return await context.DeletedUsers.AsNoTracking().AnyAsync(deleted => deleted.Id == userId);
    }

    internal async Task<PhotoSweepResponse> SweepOrphanedPhotosAsync(DateTime uploadedBeforeUtc)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        Result<PhotoSweepResponse> result =
            await sender.Send(new SweepOrphanedPhotosCommand(uploadedBeforeUtc));

        return result.Value;
    }

    public async Task<int> CountAscentsAsync(Guid userId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        return await context.Ascents.AsNoTracking().CountAsync(ascent => ascent.UserId == userId);
    }

    public async Task<bool> WaitForAscentRemovalAsync(Guid userId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await CountAscentsAsync(userId) == 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    public async Task<string?> ReadPeakNameAsync(Guid ascentId)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();

        return await context.Ascents
            .AsNoTracking()
            .Where(ascent => ascent.Id == ascentId)
            .Select(ascent => ascent.Peak.Name)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> WaitForPeakNameAsync(Guid ascentId, string expectedName)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (await ReadPeakNameAsync(ascentId) == expectedName)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(BuildSettings()));

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IPeakCatalog>();
            services.AddScoped<IPeakCatalog>(provider => new CachingPeakCatalog(
                PeakCatalog, provider.GetRequiredService<IMemoryCache>()));

            services.RemoveAll<IProfileDirectory>();
            services.AddScoped<IProfileDirectory>(provider => new CachingProfileDirectory(
                ProfileDirectory, provider.GetRequiredService<IMemoryCache>()));

            services.RemoveAll<IPhotoStorage>();
            services.AddSingleton<IPhotoStorage>(PhotoStorage);

            services.RemoveAll<IPhotoAssetInventory>();
            services.AddSingleton<IPhotoAssetInventory>(PhotoAssetInventory);

            services.Configure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, ConfigureTestJwtBearer);
        });
    }

    private void ConfigureTestJwtBearer(JwtBearerOptions options)
    {
        options.Authority = null;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = TestTokenSigning.Issuer,
            ValidateAudience = true,
            ValidAudience = TestTokenSigning.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = _tokenSigning.PublicKey,
            NameClaimType = "sub",
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    private Dictionary<string, string?> BuildSettings()
    {
        Uri rabbitUri = new(_rabbitMq.GetConnectionString());
        string[] credentials = rabbitUri.UserInfo.Split(':');

        return new Dictionary<string, string?>
        {
            ["ConnectionStrings:AscentDatabase"] = _postgres.GetConnectionString(),
            ["Messaging:Host"] = rabbitUri.Host,
            ["Messaging:Port"] = rabbitUri.Port.ToString(CultureInfo.InvariantCulture),
            ["Messaging:Username"] = credentials[0],
            ["Messaging:Password"] = credentials[1],
            ["Messaging:VirtualHost"] = "/",
            ["Outbox:PollingInterval"] = "00:00:01",
            ["Jwt:Issuer"] = TestTokenSigning.Issuer,
            ["Jwt:Audience"] = TestTokenSigning.Audience,
            ["PeakCatalog:BaseAddress"] = "http://peak-service.test/",
            ["ProfileDirectory:BaseAddress"] = "http://account-service.test/",
            ["Cloudinary:CloudName"] = "test",
            ["Cloudinary:ApiKey"] = "test",
            ["Cloudinary:ApiSecret"] = "test",
            ["Cloudinary:AuthTokenKey"] = TestAuthTokenKey,
            ["PhotoSweep:Enabled"] = "false",
            ["DeletedUserSweep:Enabled"] = "false"
        };
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await _postgres.StartAsync();
        await _rabbitMq.StartAsync();

        using IServiceScope scope = Services.CreateScope();
        AscentDbContext context = scope.ServiceProvider.GetRequiredService<AscentDbContext>();
        await context.Database.MigrateAsync();
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        _tokenSigning.Dispose();
        await _postgres.DisposeAsync();
        await _rabbitMq.DisposeAsync();
        await base.DisposeAsync();
    }
}
