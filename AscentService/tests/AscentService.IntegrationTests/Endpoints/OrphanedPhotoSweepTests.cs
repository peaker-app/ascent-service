using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.SweepOrphanedPhotos;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class OrphanedPhotoSweepTests(AscentServiceApiFactory factory)
{
    private static readonly DateTime Cutoff = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task Sweep_WithAnAgedQuarantinedAssetThatWasNeverPersisted_RemovesIt()
    {
        factory.PhotoAssetInventory.Clear();
        factory.PhotoAssetInventory.Quarantined.Add(Aged("peaker/test/ascents/never-persisted"));

        PhotoSweepResponse result = await factory.SweepOrphanedPhotosAsync(Cutoff);

        result.QuarantinedRemoved.Should().Be(1);
    }

    [Fact]
    public async Task Sweep_WithAConfirmedAssetWithoutARow_RemovesIt()
    {
        factory.PhotoAssetInventory.Clear();
        factory.PhotoAssetInventory.Confirmed.Add(Aged("peaker/test/ascents/unreferenced"));

        PhotoSweepResponse result = await factory.SweepOrphanedPhotosAsync(Cutoff);

        result.UnreferencedRemoved.Should().Be(1);
    }

    [Fact]
    public async Task Sweep_WithAnAssetStillReferencedByAnAscent_KeepsIt()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        await owner.AddPhotoAsync(ascentId);
        string publicId = (await factory.ReadPhotoPublicIdsAsync(ascentId)).Single();

        factory.PhotoAssetInventory.Clear();
        factory.PhotoAssetInventory.Confirmed.Add(Aged(publicId));

        PhotoSweepResponse result = await factory.SweepOrphanedPhotosAsync(Cutoff);

        result.UnreferencedRemoved.Should().Be(0);
    }

    [Fact]
    public async Task Sweep_WithAnAssetUploadedAfterTheCutoff_KeepsIt()
    {
        factory.PhotoAssetInventory.Clear();
        factory.PhotoAssetInventory.Quarantined.Add(
            new StoredAsset("peaker/test/ascents/in-flight", Cutoff.AddMinutes(1)));

        PhotoSweepResponse result = await factory.SweepOrphanedPhotosAsync(Cutoff);

        result.QuarantinedRemoved.Should().Be(0);
    }

    private static StoredAsset Aged(string publicId) => new(publicId, Cutoff.AddDays(-1));

    private object RegisterBody() => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate = "2026-07-01"
    };
}
