using System.Text.Json.Serialization;
using AscentService.Application.Ascents.RegisterAscent;
using AscentService.Domain.Ascents;

namespace AscentService.API.Requests;

public sealed record RegisterAscentRequest(
    [property: JsonRequired] Guid PeakId,
    [property: JsonRequired] DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    SnowCondition? Snow,
    WindCondition? Wind,
    TrailCondition? Trail,
    AscentVisibility? Visibility,
    Guid? ClientAscentId = null)
{
    public RegisterAscentCommand ToCommand(Guid userId) => new(
        userId,
        PeakId,
        AscentDate,
        Companions,
        RouteNotes,
        new AscentConditions(Snow, Wind, Trail),
        Visibility ?? AscentVisibility.Public)
    {
        ClientAscentId = ClientAscentId,
    };
}
