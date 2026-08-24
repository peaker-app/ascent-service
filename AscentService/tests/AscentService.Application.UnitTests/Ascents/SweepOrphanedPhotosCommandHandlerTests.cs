using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.SweepOrphanedPhotos;
using AscentService.Domain.Ascents;
using Common.Domain.Results;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace AscentService.Application.UnitTests.Ascents;

public sealed class SweepOrphanedPhotosCommandHandlerTests
{
    private static readonly DateTime Cutoff = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IPhotoAssetInventory _inventory = Substitute.For<IPhotoAssetInventory>();
    private readonly IPhotoStorage _photoStorage = Substitute.For<IPhotoStorage>();
    private readonly IAscentRepository _ascentRepository = Substitute.For<IAscentRepository>();

    private readonly SweepOrphanedPhotosCommandHandler _handler;

    public SweepOrphanedPhotosCommandHandlerTests()
    {
        GivenQuarantined();
        GivenConfirmed();
        GivenKnown();
        GivenDeletionConfirmed(true);

        _handler = new SweepOrphanedPhotosCommandHandler(_inventory, _photoStorage, _ascentRepository);
    }

    [Fact]
    public async Task Handle_WithAnAgedQuarantinedAsset_DeletesIt()
    {
        GivenQuarantined(Aged("orphan"));

        await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        await _photoStorage.Received(1).TryDeleteAsync("orphan", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAnAssetUploadedAfterTheCutoff_KeepsIt()
    {
        GivenQuarantined(new StoredAsset("in-flight", Cutoff.AddMinutes(1)));

        Result<PhotoSweepResponse> result =
            await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        result.Value.QuarantinedRemoved.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithAnAssetStillReferenced_KeepsIt()
    {
        GivenConfirmed(Aged("referenced"));
        GivenKnown("referenced");

        Result<PhotoSweepResponse> result =
            await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        result.Value.UnreferencedRemoved.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithAnUnreferencedConfirmedAsset_DeletesIt()
    {
        GivenConfirmed(Aged("stray"));

        Result<PhotoSweepResponse> result =
            await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        result.Value.UnreferencedRemoved.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WhenCloudinaryDoesNotConfirmTheDeletion_DoesNotCountItAsRemoved()
    {
        GivenConfirmed(Aged("stray"));
        GivenDeletionConfirmed(false);

        Result<PhotoSweepResponse> result =
            await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        result.Value.UnreferencedRemoved.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithNoCandidates_NeverQueriesTheRepository()
    {
        await _handler.Handle(new SweepOrphanedPhotosCommand(Cutoff), CancellationToken.None);

        await _ascentRepository.DidNotReceive().GetKnownPhotoPublicIdsAsync(
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    private static StoredAsset Aged(string publicId) => new(publicId, Cutoff.AddDays(-1));

    private void GivenDeletionConfirmed(bool confirmed) =>
        _photoStorage.TryDeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(confirmed);

    private void GivenQuarantined(params StoredAsset[] assets) =>
        _inventory.ListQuarantinedAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyCollection<StoredAsset>>(assets);

    private void GivenConfirmed(params StoredAsset[] assets) =>
        _inventory.ListConfirmedAsync(Arg.Any<CancellationToken>())
            .Returns<IReadOnlyCollection<StoredAsset>>(assets);

    private void GivenKnown(params string[] publicIds) =>
        _ascentRepository
            .GetKnownPhotoPublicIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns<IReadOnlySet<string>>(publicIds.ToHashSet(StringComparer.Ordinal));
}
