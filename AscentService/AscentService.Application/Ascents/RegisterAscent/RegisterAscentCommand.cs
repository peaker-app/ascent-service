using AscentService.Domain.Ascents;
using Common.Application.Messaging;

namespace AscentService.Application.Ascents.RegisterAscent;

public sealed record RegisterAscentCommand(
    Guid UserId,
    Guid PeakId,
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    AscentConditions Conditions,
    AscentVisibility Visibility) : ICommand<Guid>
{
    public Guid? ClientAscentId { get; init; }

    public AscentDetails ToDetails() => new(AscentDate, Companions, RouteNotes, Conditions, Visibility);

    public Guid? DeduplicationKey => ClientAscentId is { } key && key != Guid.Empty ? key : null;
}
