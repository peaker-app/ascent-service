using AscentService.Application.Abstractions;
using CloudinaryDotNet;
using Common.Application.Abstractions;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class CloudinaryPhotoUrlSigner(
    CloudinaryFactory cloudinaryFactory,
    IDateTimeProvider dateTimeProvider) : IPhotoUrlSigner
{
    public string Sign(string publicId, TimeSpan lifetime)
    {
        Url url = cloudinaryFactory.Client.Api.UrlImgUp
            .Secure(true)
            .Type(PhotoDelivery.AuthenticatedType)
            .Signed(true);

        return HasTokenKey
            ? url.AuthToken(ExpiringToken(lifetime)).BuildUrl(publicId)
            : url.BuildUrl(publicId);
    }

    private bool HasTokenKey => !string.IsNullOrWhiteSpace(cloudinaryFactory.Options.AuthTokenKey);

    private AuthToken ExpiringToken(TimeSpan lifetime) =>
        new AuthToken(cloudinaryFactory.Options.AuthTokenKey)
            .StartTime(new DateTimeOffset(dateTimeProvider.UtcNow, TimeSpan.Zero).ToUnixTimeSeconds())
            .Duration((long)lifetime.TotalSeconds);
}
