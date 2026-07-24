using AscentService.Domain.Ascents;

namespace AscentService.Domain.UnitTests.TestData;

internal static class AscentMother
{
    public static readonly DateOnly Today = new(2026, 7, 25);

    public static readonly Guid OwnerId = Guid.Parse("0198f000-0000-7000-8000-000000000001");

    public static readonly PeakSnapshot Aneto =
        new(Guid.Parse("0198f000-0000-7000-8000-0000000000a1"), "Aneto", 3404);

    public static AscentDetails Details(
        DateOnly? ascentDate = null,
        string? companions = null,
        string? routeNotes = null,
        AscentVisibility visibility = AscentVisibility.Public) =>
        new(ascentDate ?? Today, companions, routeNotes, AscentConditions.Unreported, visibility);

    public static AscentDraft Draft(AscentDetails? details = null) =>
        new(OwnerId, Aneto, details ?? Details());

    public static Ascent Registered(AscentDetails? details = null) =>
        Ascent.Create(Draft(details), Today).Value;

    public static Ascent WithPhotos(int count)
    {
        Ascent ascent = Registered();

        for (int index = 0; index < count; index++)
        {
            ascent.AddPhoto(Upload(index), DateTime.UnixEpoch.AddDays(index));
        }

        return ascent;
    }

    public static PhotoUpload Upload(int index = 0) =>
        new($"peaker/dev/ascents/photo-{index}", $"https://res.cloudinary.com/photo-{index}.webp", 1024, 768);
}
