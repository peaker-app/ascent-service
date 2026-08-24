using Common.Domain.Abstractions;

namespace AscentService.Domain.Ascents.Events;

public sealed record AscentPhotoRemovedDomainEvent(Guid AscentId, string CloudinaryPublicId) : IDomainEvent;
