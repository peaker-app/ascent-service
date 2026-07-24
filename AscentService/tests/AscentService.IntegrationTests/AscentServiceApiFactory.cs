using System.Globalization;
using System.Net.Http.Headers;
using AscentService.Application.Abstractions;
using AscentService.Infrastructure.Persistence;
using AscentService.IntegrationTests.Fakes;
using Common.Contracts.Peaks;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
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

    public HttpClient CreateAuthenticatedClient(Guid userId)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _tokenSigning.CreateAccessToken(userId));

        return client;
    }

    public async Task PublishPeakRenamedAsync(PeakRenamed message)
    {
        await using AsyncServiceScope scope = Services.CreateAsyncScope();
        IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        await publishEndpoint.Publish(message);
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
            services.AddSingleton<IPeakCatalog>(PeakCatalog);

            services.RemoveAll<IProfileDirectory>();
            services.AddSingleton<IProfileDirectory>(ProfileDirectory);

            services.RemoveAll<IPhotoStorage>();
            services.AddSingleton<IPhotoStorage>(PhotoStorage);

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
        var rabbitUri = new Uri(_rabbitMq.GetConnectionString());
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
            ["Cloudinary:ApiSecret"] = "test"
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
