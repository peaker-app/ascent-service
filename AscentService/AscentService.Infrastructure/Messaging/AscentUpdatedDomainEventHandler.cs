using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Contracts.Ascents;
using MassTransit;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentUpdatedDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<AscentUpdatedDomainEvent>
{
    public Task Handle(AscentUpdatedDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new AscentUpdated
            {
                AscentId = domainEvent.AscentId,
                UserId = domainEvent.UserId,
                PeakId = domainEvent.PeakId,
                AscentDate = domainEvent.AscentDate,
                Visibility = domainEvent.Visibility,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
