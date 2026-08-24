using AscentService.Domain.Ascents;

namespace AscentService.Application.Ascents.ListMyAscents;

public sealed record AscentSummaryRow(
    Guid Id,
    Guid PeakId,
    string PeakName,
    int PeakAltitudeMeters,
    DateOnly AscentDate,
    AscentVisibility Visibility,
    string? ThumbnailPublicId);
