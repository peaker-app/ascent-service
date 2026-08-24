namespace AscentService.Domain.Ascents;

public sealed record AscentDetails(
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    AscentConditions Conditions,
    AscentVisibility Visibility);
