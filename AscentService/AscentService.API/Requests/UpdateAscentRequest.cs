using AscentService.Application.Ascents.UpdateAscent;
using AscentService.Domain.Ascents;

namespace AscentService.API.Requests;

public sealed record UpdateAscentRequest(
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    SnowCondition? Snow,
    WindCondition? Wind,
    TrailCondition? Trail,
    AscentVisibility? Visibility)
{
    public UpdateAscentCommand ToCommand(Guid ascentId, Guid userId) => new(
        ascentId,
        userId,
        AscentDate,
        Companions,
        RouteNotes,
        new AscentConditions(Snow, Wind, Trail),
        Visibility ?? AscentVisibility.Public);
}
