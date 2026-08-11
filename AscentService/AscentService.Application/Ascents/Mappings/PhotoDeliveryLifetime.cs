using AscentService.Domain.Ascents;

namespace AscentService.Application.Ascents.Mappings;

internal static class PhotoDeliveryLifetime
{
    private static readonly TimeSpan PublicAscent = TimeSpan.FromHours(24);
    private static readonly TimeSpan PrivateAscent = TimeSpan.FromMinutes(10);

    public static TimeSpan For(AscentVisibility visibility) =>
        visibility is AscentVisibility.Public ? PublicAscent : PrivateAscent;
}
