using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents.Events;
using Common.Application.Abstractions;

namespace AscentService.Infrastructure.Messaging;

internal sealed class AscentPhotoRemovedDomainEventHandler(IPhotoStorage photoStorage)
    : IDomainEventHandler<AscentPhotoRemovedDomainEvent>
{
    public Task Handle(
        AscentPhotoRemovedDomainEvent domainEvent,
        DomainEventContext context,
        CancellationToken cancellationToken) =>
        photoStorage.DeleteAsync(domainEvent.CloudinaryPublicId, cancellationToken);
}
