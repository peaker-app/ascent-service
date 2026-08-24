using AscentService.Application.Ascents.SweepOrphanedPhotos;
using Common.Application.Abstractions;
using Common.Domain.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AscentService.Infrastructure.Maintenance;

public sealed class OrphanedPhotoSweeper(
    IServiceScopeFactory scopeFactory,
    IDateTimeProvider dateTimeProvider,
    IOptions<PhotoSweepOptions> options,
    ILogger<OrphanedPhotoSweeper> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        using PeriodicTimer timer = new(options.Value.Interval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
#pragma warning disable CA1031
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Orphaned photo sweep failed");
            }
#pragma warning restore CA1031
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        SweepOrphanedPhotosCommand command = new(dateTimeProvider.UtcNow - options.Value.Retention);
        Result<PhotoSweepResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Orphaned photo sweep reported {ErrorCode}", result.Error.Code);
            return;
        }

        logger.LogInformation(
            "Orphaned photo sweep removed {QuarantinedRemoved} quarantined and {UnreferencedRemoved} unreferenced assets",
            result.Value.QuarantinedRemoved,
            result.Value.UnreferencedRemoved);
    }
}
