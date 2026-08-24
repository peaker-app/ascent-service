using Common.Domain.Abstractions;

namespace AscentService.Domain.Ascents.Events;

public sealed record AscentPhotoStoredDomainEvent(Guid AscentId, string CloudinaryPublicId) : IDomainEvent;
