using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;
using Common.Contracts.Ascents;
using MassTransit;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentRegisteredDomainEventHandler(
    IPublishEndpoint publishEndpoint,
    IDateTimeProvider dateTimeProvider) : IDomainEventHandler<AscentRegisteredDomainEvent>
{
    public Task Handle(AscentRegisteredDomainEvent domainEvent, CancellationToken cancellationToken) =>
        publishEndpoint.Publish(
            new AscentRegistered
            {
                AscentId = domainEvent.AscentId,
                UserId = domainEvent.UserId,
                PeakId = domainEvent.PeakId,
                PeakName = domainEvent.PeakName,
                PeakAltitudeM = domainEvent.PeakAltitudeMeters,
                AscentDate = domainEvent.AscentDate,
                Visibility = domainEvent.Visibility,
                OccurredAtUtc = dateTimeProvider.UtcNow
            },
            cancellationToken);
}
