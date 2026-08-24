using AscentService.Domain.Ascents;
using Common.Contracts.Peaks;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class PeakRenamedConsumerTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Consume_WithARenamedPeak_UpdatesTheDenormalisedName()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Cervino", 4478);
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(BodyFor(peak));

        await factory.PublishPeakRenamedAsync(RenameOf(peak, "Matterhorn"));

        bool synchronised = await factory.WaitForPeakNameAsync(ascentId, "Matterhorn");

        synchronised.Should().BeTrue();
    }

    [Fact]
    public async Task Consume_WithADuplicatedMessage_LeavesTheAscentUnchanged()
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Cervino", 4478);
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(BodyFor(peak));

        PeakRenamed message = RenameOf(peak, "Matterhorn");
        await factory.PublishPeakRenamedAsync(message);
        await factory.WaitForPeakNameAsync(ascentId, "Matterhorn");

        await factory.PublishPeakRenamedAsync(message);
        await Task.Delay(TimeSpan.FromSeconds(2));

        string? name = await factory.ReadPeakNameAsync(ascentId);

        name.Should().Be("Matterhorn");
    }

    private static PeakRenamed RenameOf(PeakSnapshot peak, string newName) => new()
    {
        MessageId = Guid.CreateVersion7(),
        PeakId = peak.PeakId,
        Name = newName,
        AltitudeM = peak.AltitudeMeters,
        CountryCode = "CH",
        OccurredAtUtc = DateTime.UtcNow
    };

    private static object BodyFor(PeakSnapshot peak) => new
    {
        peakId = peak.PeakId,
        ascentDate = "2026-07-01"
    };
}
