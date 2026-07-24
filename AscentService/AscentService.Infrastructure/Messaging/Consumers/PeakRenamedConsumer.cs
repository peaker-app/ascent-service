using AscentService.Application.Ascents.SyncPeakName;
using AscentService.Infrastructure.Persistence;
using Common.Application.Abstractions;
using Common.Contracts.Peaks;
using Common.Domain.Results;
using Common.Infrastructure.Persistence.Idempotency;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Messaging.Consumers;

internal sealed class PeakRenamedConsumer(
    ISender sender,
    AscentDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IConsumer<PeakRenamed>
{
    public async Task Consume(ConsumeContext<PeakRenamed> context)
    {
        PeakRenamed message = context.Message;
        CancellationToken cancellationToken = context.CancellationToken;

        if (await IsAlreadyProcessedAsync(message.MessageId, cancellationToken))
        {
            return;
        }

        Result result = await sender.Send(
            new SyncPeakNameCommand(message.PeakId, message.Name, message.AltitudeM), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudo sincronizar el nombre del pico {message.PeakId}: {result.Error.Code}.");
        }

        dbContext.Set<ProcessedMessage>().Add(new ProcessedMessage
        {
            MessageId = message.MessageId,
            ProcessedAtUtc = dateTimeProvider.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private Task<bool> IsAlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken) =>
        dbContext.Set<ProcessedMessage>().AnyAsync(message => message.MessageId == messageId, cancellationToken);
}
