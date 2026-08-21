using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Domain.Ascents;
using Common.Application.Pagination;

namespace AscentService.Application.Ascents.Mappings;

internal static class AscentMappings
{
    public static PagedResult<AscentSummaryResponse> ToResponse(
        this PagedResult<AscentSummaryRow> page,
        IPhotoUrlSigner signer) => new(
        [.. page.Items.Select(row => row.ToResponse(signer))],
        page.Page,
        page.Size,
        page.TotalCount);

    public static AscentSummaryResponse ToResponse(this AscentSummaryRow row, IPhotoUrlSigner signer) => new(
        row.Id,
        row.PeakId,
        row.PeakName,
        row.PeakAltitudeMeters,
        row.AscentDate,
        row.Visibility.ToString(),
        SignThumbnail(row, signer));

    public static AscentResponse ToResponse(this Ascent ascent, IPhotoUrlSigner signer) => new(
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
        [.. ascent.Photos.Select(photo => photo.ToResponse(signer, ascent.Visibility))]);

    public static AscentPhotoResponse ToResponse(
        this AscentPhoto photo,
        IPhotoUrlSigner signer,
        AscentVisibility visibility) => new(
        photo.Id,
        signer.Sign(photo.CloudinaryPublicId, PhotoDeliveryLifetime.For(visibility)),
        photo.Width,
        photo.Height,
        photo.Position,
        photo.UploadedAtUtc);

    private static AscentConditionsResponse ToResponse(this AscentConditions conditions) => new(
        conditions.Snow?.ToString(),
        conditions.Wind?.ToString(),
        conditions.Trail?.ToString());

    private static string? SignThumbnail(AscentSummaryRow row, IPhotoUrlSigner signer) =>
        row.ThumbnailPublicId is null
            ? null
            : signer.Sign(row.ThumbnailPublicId, PhotoDeliveryLifetime.For(row.Visibility));
}
