using AscentService.Application.Ascents.SweepDeletedUsers;
using Common.Domain.Results;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AscentService.Infrastructure.Maintenance;

public sealed class DeletedUserSweeper(
    IServiceScopeFactory scopeFactory,
    IOptions<DeletedUserSweepOptions> options,
    ILogger<DeletedUserSweeper> logger) : BackgroundService
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
                logger.LogError(exception, "Deleted user sweep failed");
            }
#pragma warning restore CA1031
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();

        SweepDeletedUsersCommand command = new(options.Value.BatchSize);
        Result<DeletedUserSweepResponse> result = await sender.Send(command, cancellationToken);

        if (result.IsFailure)
        {
            logger.LogWarning("Deleted user sweep reported {ErrorCode}", result.Error.Code);
            return;
        }

        if (result.Value.AscentsRemoved > 0)
        {
            logger.LogWarning(
                "Deleted user sweep removed {AscentsRemoved} ascents left behind by {UsersScanned} deleted users",
                result.Value.AscentsRemoved,
                result.Value.UsersScanned);
        }
    }
}
