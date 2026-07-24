namespace AscentService.Application.Ascents.ListMyAscents;

public sealed record AscentSummaryResponse(
    Guid Id,
    Guid PeakId,
    string PeakName,
    int PeakAltitudeMeters,
    DateOnly AscentDate,
    string Visibility,
    string? ThumbnailUrl);
