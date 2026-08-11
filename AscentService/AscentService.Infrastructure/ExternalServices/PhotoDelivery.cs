using CloudinaryDotNet;

namespace AscentService.Infrastructure.ExternalServices;

public static class PhotoDelivery
{
    public const string AuthenticatedType = "authenticated";
    public const string QuarantineTag = "peaker-quarantine";
    public const string StoredFormat = "webp";

    private const string StripProfileFlag = "strip_profile";
    private const string StripExifFlag = "strip_exif";
    private const string AutomaticQuality = "auto";

    public static Transformation Sanitizing() =>
        new Transformation().Quality(AutomaticQuality).Flags(StripProfileFlag, StripExifFlag);
}
