using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.SweepOrphanedPhotos;

internal sealed class SweepOrphanedPhotosCommandHandler(
    IPhotoAssetInventory photoAssetInventory,
    IPhotoStorage photoStorage,
    IAscentRepository ascentRepository) : ICommandHandler<SweepOrphanedPhotosCommand, PhotoSweepResponse>
{
    public async Task<Result<PhotoSweepResponse>> Handle(
        SweepOrphanedPhotosCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<StoredAsset> quarantined =
            await photoAssetInventory.ListQuarantinedAsync(cancellationToken);

        IReadOnlyCollection<StoredAsset> confirmed =
            await photoAssetInventory.ListConfirmedAsync(cancellationToken);

        return new PhotoSweepResponse(
            await DeleteOrphansAsync(quarantined, command.UploadedBeforeUtc, cancellationToken),
            await DeleteOrphansAsync(confirmed, command.UploadedBeforeUtc, cancellationToken));
    }

    private async Task<int> DeleteOrphansAsync(
        IReadOnlyCollection<StoredAsset> assets,
        DateTime uploadedBeforeUtc,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> candidates =
        [
            .. assets.Where(asset => asset.CreatedAtUtc < uploadedBeforeUtc).Select(asset => asset.PublicId)
        ];

        if (candidates.Count == 0)
        {
            return 0;
        }

        IReadOnlySet<string> referenced =
            await ascentRepository.GetKnownPhotoPublicIdsAsync(candidates, cancellationToken);

        return await DeleteAllAsync(candidates.Except(referenced), cancellationToken);
    }

    private async Task<int> DeleteAllAsync(IEnumerable<string> publicIds, CancellationToken cancellationToken)
    {
        int removed = 0;

        foreach (string publicId in publicIds)
        {
            await photoStorage.TryDeleteAsync(publicId, cancellationToken);
            removed++;
        }

        return removed;
    }
}
