using AscentService.Application.Ascents.RegisterAscent;
using AscentService.Domain.Ascents;

namespace AscentService.API.Requests;

public sealed record RegisterAscentRequest(
    Guid PeakId,
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    SnowCondition? Snow,
    WindCondition? Wind,
    TrailCondition? Trail,
    AscentVisibility? Visibility)
{
    public RegisterAscentCommand ToCommand(Guid userId) => new(
        userId,
        PeakId,
        AscentDate,
        Companions,
        RouteNotes,
        new AscentConditions(Snow, Wind, Trail),
        Visibility ?? AscentVisibility.Public);
}
