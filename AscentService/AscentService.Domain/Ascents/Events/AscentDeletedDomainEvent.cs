using Common.Domain.Abstractions;

namespace AscentService.Domain.Ascents.Events;

public sealed record AscentDeletedDomainEvent(
    Guid AscentId,
    Guid UserId,
    Guid PeakId,
    DateOnly AscentDate) : IDomainEvent;
