using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.RegisterAscent;
using AscentService.Domain.Ascents;

namespace AscentService.Application.UnitTests.TestData;

internal static class AscentFactory
{
    public static readonly DateOnly Today = new(2026, 7, 25);

    public static readonly Guid OwnerId = Guid.Parse("0198f000-0000-7000-8000-000000000001");

    public static readonly Guid OtherUserId = Guid.Parse("0198f000-0000-7000-8000-000000000002");

    public static readonly PeakSnapshot Aneto =
        new(Guid.Parse("0198f000-0000-7000-8000-0000000000a1"), "Aneto", 3404);

    public static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46];

    public static Ascent Registered(AscentVisibility visibility = AscentVisibility.Public, Guid? userId = null)
    {
        AscentDetails details = new(Today, null, null, AscentConditions.Unreported, visibility);
        AscentDraft draft = new(userId ?? OwnerId, Aneto, details);

        Ascent ascent = Ascent.Create(draft, Today).Value;
        ascent.ClearDomainEvents();

        return ascent;
    }

    public static RegisterAscentCommand RegisterCommand(DateOnly? ascentDate = null) => new(
        OwnerId,
        Aneto.PeakId,
        ascentDate ?? Today,
        null,
        null,
        AscentConditions.Unreported,
        AscentVisibility.Public);

    public static PhotoFile PhotoFile(byte[]? content = null) =>
        new(content ?? JpegBytes, "image/jpeg", "cumbre.jpg");

    public static StoredPhoto StoredPhoto() =>
        new("peaker/dev/ascents/cumbre", "https://res.cloudinary.com/cumbre.jpg", 1600, 1200);
}
