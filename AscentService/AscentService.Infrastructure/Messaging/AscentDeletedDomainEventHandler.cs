using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Contracts.Ascents;
using MassTransit;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentDeletedDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<AscentDeletedDomainEvent>
{
    public Task Handle(
        AscentDeletedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new AscentDeleted
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                AscentId = domainEvent.AscentId,
                UserId = domainEvent.UserId,
                PeakId = domainEvent.PeakId,
                AscentDate = domainEvent.AscentDate
            },
            cancellationToken);
}
