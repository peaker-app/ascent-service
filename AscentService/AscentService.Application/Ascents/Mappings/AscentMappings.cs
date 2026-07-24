using AscentService.Application.Ascents.GetAscentById;
using AscentService.Domain.Ascents;

namespace AscentService.Application.Ascents.Mappings;

internal static class AscentMappings
{
    public static AscentResponse ToResponse(this Ascent ascent) => new(
        ascent.Id,
        ascent.UserId,
        ascent.Peak.PeakId,
        ascent.Peak.Name,
        ascent.Peak.AltitudeMeters,
        ascent.AscentDate,
        ascent.Companions,
        ascent.RouteNotes,
        ascent.Conditions.ToResponse(),
        ascent.Visibility.ToString(),
        [.. ascent.Photos.Select(photo => photo.ToResponse())]);

    public static AscentPhotoResponse ToResponse(this AscentPhoto photo) => new(
        photo.Id,
        photo.SecureUrl,
        photo.Width,
        photo.Height,
        photo.Position,
        photo.UploadedAtUtc);

    private static AscentConditionsResponse ToResponse(this AscentConditions conditions) => new(
        conditions.Snow?.ToString(),
        conditions.Wind?.ToString(),
        conditions.Trail?.ToString());
}
