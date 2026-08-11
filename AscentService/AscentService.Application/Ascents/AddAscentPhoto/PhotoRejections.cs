using AscentService.Domain.Ascents;
using Common.Application.Images;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.AddAscentPhoto;

internal static class PhotoRejections
{
    public static Error ToError(ImageRejection rejection) => rejection switch
    {
        ImageRejection.TooLarge => AscentErrors.PhotoTooLarge,
        _ => AscentErrors.UnsupportedPhotoFormat
    };
}
