using Common.Domain.Abstractions;

namespace AscentService.Domain.Ascents.Events;

public sealed record AscentUpdatedDomainEvent(
    Guid AscentId,
    Guid UserId,
    Guid PeakId,
    DateOnly AscentDate,
    string Visibility) : IDomainEvent;
