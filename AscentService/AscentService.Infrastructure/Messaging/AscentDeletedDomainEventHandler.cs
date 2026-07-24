using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Contracts.Ascents;
using MassTransit;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentDeletedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<AscentDeletedDomainEvent>
{
    public Task Handle(AscentDeletedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new AscentDeleted
            {
                AscentId = domainEvent.AscentId,
                UserId = domainEvent.UserId,
                PeakId = domainEvent.PeakId,
                AscentDate = domainEvent.AscentDate,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
