using Common.Domain.Abstractions;

namespace AscentService.Domain.Ascents.Events;

public sealed record AscentRegisteredDomainEvent(
    Guid AscentId,
    Guid UserId,
    Guid PeakId,
    string PeakName,
    int PeakAltitudeMeters,
    DateOnly AscentDate,
    string Visibility) : IDomainEvent;
