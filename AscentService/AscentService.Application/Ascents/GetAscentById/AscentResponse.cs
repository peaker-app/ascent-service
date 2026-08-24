namespace AscentService.Application.Ascents.GetAscentById;

public sealed record AscentResponse(
    Guid Id,
    Guid UserId,
    Guid PeakId,
    string PeakName,
    int PeakAltitudeMeters,
    DateOnly AscentDate,
    string? Companions,
    string? RouteNotes,
    AscentConditionsResponse Conditions,
    string Visibility,
    IReadOnlyList<AscentPhotoResponse> Photos);

public sealed record AscentConditionsResponse(string? Snow, string? Wind, string? Trail);
