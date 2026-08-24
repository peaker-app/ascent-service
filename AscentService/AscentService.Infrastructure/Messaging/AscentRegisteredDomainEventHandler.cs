using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Contracts.Ascents;
using MassTransit;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentRegisteredDomainEventHandler(IPublishEndpoint publishEndpoint)
    : IDomainEventHandler<AscentRegisteredDomainEvent>
{
    public Task Handle(
        AscentRegisteredDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new AscentRegistered
            {
                MessageId = context.MessageId,
                OccurredAtUtc = context.OccurredAtUtc,
                AscentId = domainEvent.AscentId,
                UserId = domainEvent.UserId,
                PeakId = domainEvent.PeakId,
                PeakName = domainEvent.PeakName,
                PeakAltitudeM = domainEvent.PeakAltitudeMeters,
                AscentDate = domainEvent.AscentDate,
                Visibility = domainEvent.Visibility
            },
            cancellationToken);
}
