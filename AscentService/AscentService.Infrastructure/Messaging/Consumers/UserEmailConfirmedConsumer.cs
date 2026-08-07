using AscentService.Application.ConfirmedUsers.ConfirmUser;
using AscentService.Infrastructure.Persistence;
using Common.Application.Abstractions;
using Common.Contracts.Users;
using Common.Domain.Results;
using Common.Infrastructure.Persistence.Idempotency;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AscentService.Infrastructure.Messaging.Consumers;

internal sealed class UserEmailConfirmedConsumer(
    ISender sender,
    AscentDbContext dbContext,
    IDateTimeProvider dateTimeProvider) : IConsumer<UserEmailConfirmed>
{
    public async Task Consume(ConsumeContext<UserEmailConfirmed> context)
    {
        UserEmailConfirmed message = context.Message;
        CancellationToken cancellationToken = context.CancellationToken;

        if (await IsAlreadyProcessedAsync(message.MessageId, cancellationToken))
        {
            return;
        }

        Result result = await sender.Send(
            new ConfirmUserCommand(message.UserId, RestoreUtcKind(message.OccurredAtUtc)), cancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"No se pudo proyectar la confirmación del usuario {message.UserId}: {result.Error.Code}.");
        }

        dbContext.Set<ProcessedMessage>().Add(new ProcessedMessage
        {
            MessageId = message.MessageId,
            ProcessedAtUtc = dateTimeProvider.UtcNow
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static DateTime RestoreUtcKind(DateTime occurredAtUtc) =>
        DateTime.SpecifyKind(occurredAtUtc, DateTimeKind.Utc);

    private Task<bool> IsAlreadyProcessedAsync(Guid messageId, CancellationToken cancellationToken) =>
        dbContext.Set<ProcessedMessage>().AnyAsync(message => message.MessageId == messageId, cancellationToken);
}
