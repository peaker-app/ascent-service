using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using AscentService.Domain.Ascents.Events;
using AscentService.Domain.ConfirmedUsers;
using AscentService.Infrastructure.ExternalServices;
using AscentService.Infrastructure.Maintenance;
using AscentService.Infrastructure.Messaging;
using AscentService.Infrastructure.Messaging.Consumers;
using AscentService.Infrastructure.Persistence;
using AscentService.Infrastructure.Persistence.Repositories;
using Common.Application.Abstractions;
using Common.Application.Images;
using Common.Infrastructure.Messaging;
using Common.Infrastructure.Persistence;
using Common.Infrastructure.Persistence.Outbox;
using Common.Infrastructure.Time;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AscentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPersistence(configuration);
        services.AddPhotoStorage(configuration);
        services.AddExternalServices(configuration);
        services.AddEventBus(configuration, bus =>
        {
            bus.AddConsumer<PeakRenamedConsumer>();
            bus.AddConsumer<PeakUpdatedConsumer>().Endpoint(endpoint => endpoint.Temporary = true);
            bus.AddConsumer<UserDeletedConsumer>();
            bus.AddConsumer<UserEmailConfirmedConsumer>();
        });
        services.AddDomainEventHandlers();

        return services;
    }

    private static void AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<AuditableEntityInterceptor>();
        services.AddSingleton<OutboxInterceptor>();
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));

        services.AddAscentDbContext();
        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<AscentDbContext>());
        services.AddScoped<IAscentRepository, AscentRepository>();
        services.AddScoped<IAscentReader, AscentReader>();
        services.AddScoped<IConfirmedUserRepository, ConfirmedUserRepository>();
        services.AddScoped<IConfirmedUserDirectory, ConfirmedUserDirectory>();
        services.AddHostedService<OutboxProcessor<AscentDbContext>>();
    }

    private static void AddAscentDbContext(this IServiceCollection services) =>
        services.AddDbContext<AscentDbContext>((provider, options) => options
            .UseNpgsql(ResolveConnectionString(provider))
            .AddInterceptors(
                provider.GetRequiredService<AuditableEntityInterceptor>(),
                provider.GetRequiredService<OutboxInterceptor>()));

    private static string ResolveConnectionString(IServiceProvider provider)
    {
        string? connectionString = provider.GetRequiredService<IConfiguration>()
            .GetConnectionString("AscentDatabase");

        return string.IsNullOrWhiteSpace(connectionString)
            ? throw new InvalidOperationException("Connection string 'AscentDatabase' is not configured.")
            : connectionString;
    }

    private static void AddPhotoStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<CloudinaryOptions>()
            .Bind(configuration.GetSection(CloudinaryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<PhotoSweepOptions>(configuration.GetSection(PhotoSweepOptions.SectionName));

        services.AddSingleton<CloudinaryFactory>();
        services.AddSingleton<IImageValidator, ImageValidator>();
        services.AddSingleton<IPhotoUrlSigner, CloudinaryPhotoUrlSigner>();
        services.AddScoped<IPhotoStorage, CloudinaryPhotoStorage>();
        services.AddScoped<IPhotoAssetInventory, CloudinaryPhotoInventory>();
        services.AddHostedService<OrphanedPhotoSweeper>();
    }

    private static void AddExternalServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<PeakCatalogOptions>()
            .Bind(configuration.GetSection(PeakCatalogOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<ProfileDirectoryOptions>()
            .Bind(configuration.GetSection(ProfileDirectoryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddMemoryCache();
        services.AddHttpClient<PeakCatalogHttpClient>(ConfigurePeakCatalog)
            .AddStandardResilienceHandler();

        services.AddScoped<IPeakCatalog>(provider => new CachingPeakCatalog(
            provider.GetRequiredService<PeakCatalogHttpClient>(),
            provider.GetRequiredService<IMemoryCache>()));

        services.AddHttpClient<IProfileDirectory, AccountProfileHttpClient>(ConfigureProfileDirectory)
            .AddStandardResilienceHandler();
    }

    private static void ConfigurePeakCatalog(IServiceProvider provider, HttpClient client)
    {
        PeakCatalogOptions options = provider.GetRequiredService<IOptions<PeakCatalogOptions>>().Value;

        client.BaseAddress = options.BaseAddress;
        client.Timeout = options.RequestTimeout;
    }

    private static void ConfigureProfileDirectory(IServiceProvider provider, HttpClient client)
    {
        ProfileDirectoryOptions options = provider.GetRequiredService<IOptions<ProfileDirectoryOptions>>().Value;

        client.BaseAddress = options.BaseAddress;
        client.Timeout = options.RequestTimeout;
    }

    private static void AddDomainEventHandlers(this IServiceCollection services)
    {
        services.AddScoped<IDomainEventHandler<AscentRegisteredDomainEvent>, AscentRegisteredDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<AscentUpdatedDomainEvent>, AscentUpdatedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<AscentDeletedDomainEvent>, AscentDeletedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<AscentPhotoRemovedDomainEvent>, AscentPhotoRemovedDomainEventHandler>();
        services.AddScoped<IDomainEventHandler<AscentPhotoStoredDomainEvent>, AscentPhotoStoredDomainEventHandler>();
    }
}
