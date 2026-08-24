using AscentService.Application.Ascents.DeleteUserAscents;
using AscentService.Infrastructure.Persistence;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using Common.Domain.Results;
using Common.Infrastructure.Persistence.Idempotency;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Messaging.Consumers;

internal sealed class UserDeletedConsumer(
    ISender sender,
    AscentDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IConsumer<UserDeleted>
{
    public async Task Consume(ConsumeContext<UserDeleted> context)
    {
        UserDeleted message = context.Message;
        CancellationToken cancellationToken = context.CancellationToken;

        if (await IsAlreadyProcessedAsync(message.MessageId, cancellationToken))
        {
            return;
        }

        Result result = await sender.Send(new DeleteUserAscentsCommand(message.UserId), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudieron borrar las ascensiones del usuario {message.UserId}: {result.Error.Code}.");
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
