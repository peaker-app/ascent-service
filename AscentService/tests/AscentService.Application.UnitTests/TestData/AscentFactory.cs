using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.RegisterAscent;
using AscentService.Domain.Ascents;
using NSubstitute;

namespace AscentService.Application.UnitTests.TestData;

internal static class AscentFactory
{
    public const string SignedUrlPrefix = "https://res.cloudinary.test/image/authenticated/";

    public static readonly DateOnly Today = new(2026, 7, 25);

    public static readonly Guid OwnerId = Guid.Parse("0198f000-0000-7000-8000-000000000001");

    public static readonly Guid OtherUserId = Guid.Parse("0198f000-0000-7000-8000-000000000002");

    public static readonly PeakSnapshot Aneto =
        new(Guid.Parse("0198f000-0000-7000-8000-0000000000a1"), "Aneto", 3404);

    public static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

    public static Ascent Registered(
        AscentVisibility visibility = AscentVisibility.Public,
        Guid? userId = null,
        Guid? clientAscentId = null)
    {
        AscentDetails details = new(Today, null, null, AscentConditions.Unreported, visibility);
        AscentDraft draft = new(userId ?? OwnerId, Aneto, details, clientAscentId);

        Ascent ascent = Ascent.Create(draft, Today).Value;
        ascent.ClearDomainEvents();

        return ascent;
    }

    public static RegisterAscentCommand RegisterCommand(
        DateOnly? ascentDate = null,
        Guid? clientAscentId = null) => new(
        OwnerId,
        Aneto.PeakId,
        ascentDate ?? Today,
        null,
        null,
        AscentConditions.Unreported,
        AscentVisibility.Public)
    {
        ClientAscentId = clientAscentId,
    };

    public static PhotoFile PhotoFile(byte[]? content = null) =>
        new(content ?? JpegBytes, "image/jpeg", "cumbre.jpg");

    public static StoredPhoto StoredPhoto() =>
        new("peaker/dev/ascents/cumbre", 1600, 1200);

    public static PhotoUpload PhotoUpload(int index = 0) =>
        new($"peaker/dev/ascents/photo-{index}", 800, 600);

    public static string SignedUrlFor(string publicId, TimeSpan lifetime) =>
        $"{SignedUrlPrefix}{publicId}?expires={lifetime.TotalSeconds}";

    public static IPhotoUrlSigner PhotoUrlSigner()
    {
        IPhotoUrlSigner signer = Substitute.For<IPhotoUrlSigner>();

        signer.Sign(Arg.Any<string>(), Arg.Any<TimeSpan>())
            .Returns(call => SignedUrlFor(call.ArgAt<string>(0), call.ArgAt<TimeSpan>(1)));

        return signer;
    }
}
