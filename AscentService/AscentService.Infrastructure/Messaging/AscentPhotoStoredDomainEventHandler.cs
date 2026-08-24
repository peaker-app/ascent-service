using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentPhotoStoredDomainEventHandler(IPhotoStorage photoStorage)
    : IDomainEventHandler<AscentPhotoStoredDomainEvent>
{
    public Task Handle(
        AscentPhotoStoredDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        photoStorage.ConfirmAsync(domainEvent.CloudinaryPublicId, cancellationToken);
}
