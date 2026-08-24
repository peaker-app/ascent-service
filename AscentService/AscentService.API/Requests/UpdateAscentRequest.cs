using System.Text.Json.Serialization;
using AscentService.Application.Ascents.UpdateAscent;
using AscentService.Domain.Ascents;

namespace AscentService.API.Requests;

public sealed record UpdateAscentRequest(
    [property: JsonRequired] DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    SnowCondition? Snow,
    WindCondition? Wind,
    TrailCondition? Trail,
    [property: JsonRequired] AscentVisibility Visibility)
{
    public UpdateAscentCommand ToCommand(Guid ascentId, Guid userId) => new(
        ascentId,
        userId,
        AscentDate,
        Companions,
        RouteNotes,
        new AscentConditions(Snow, Wind, Trail),
        Visibility);
}
