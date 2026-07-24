using AscentService.Domain.Ascents;
using Common.Application.Messaging;

namespace AscentService.Application.Ascents.UpdateAscent;

public sealed record UpdateAscentCommand(
    Guid AscentId,
    Guid UserId,
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    AscentConditions Conditions,
    AscentVisibility Visibility) : ICommand
{
    public AscentDetails ToDetails() => new(AscentDate, Companions, RouteNotes, Conditions, Visibility);
}
