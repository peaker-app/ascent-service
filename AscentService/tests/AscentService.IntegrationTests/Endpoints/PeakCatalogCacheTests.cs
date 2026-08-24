using AscentService.Domain.Ascents;
using Common.Contracts.Peaks;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class PeakCatalogCacheTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task RegisterAscent_TwiceOnTheSamePeak_AsksTheCatalogOnlyOnce()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        PeakSnapshot peak = factory.PeakCatalog.Register();
        factory.PeakCatalog.ResetLookups();

        await owner.RegisterAscentAsync(BodyFor(peak));
        await owner.RegisterAscentAsync(BodyFor(peak));

        factory.PeakCatalog.Lookups.Should().Be(1);
    }

    [Fact]
    public async Task RegisterAscent_AfterThePeakChanges_AsksTheCatalogAgain()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        PeakSnapshot peak = factory.PeakCatalog.Register();

        await owner.RegisterAscentAsync(BodyFor(peak));
        await factory.PublishPeakUpdatedAsync(UpdatedMessageFor(peak));

        bool refreshed = await WaitForLookupAfterEvictionAsync(owner, peak);

        refreshed.Should().BeTrue();
    }

    private async Task<bool> WaitForLookupAfterEvictionAsync(HttpClient owner, PeakSnapshot peak)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            factory.PeakCatalog.ResetLookups();
            await owner.RegisterAscentAsync(BodyFor(peak));

            if (factory.PeakCatalog.Lookups > 0)
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private static object BodyFor(PeakSnapshot peak) => new
    {
        peakId = peak.PeakId,
        ascentDate = "2026-07-01"
    };

    private static PeakUpdated UpdatedMessageFor(PeakSnapshot peak) => new()
    {
        MessageId = Guid.CreateVersion7(),
        OccurredAtUtc = DateTime.UtcNow,
        PeakId = peak.PeakId,
        Name = peak.Name,
        AltitudeM = peak.AltitudeMeters
    };
}
