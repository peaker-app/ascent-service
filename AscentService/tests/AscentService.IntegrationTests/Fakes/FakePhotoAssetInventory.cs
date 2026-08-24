using System.Collections.Concurrent;
using AscentService.Application.Abstractions;

namespace AscentService.IntegrationTests.Fakes;

internal sealed class FakePhotoAssetInventory : IPhotoAssetInventory
{
    public ConcurrentBag<StoredAsset> Quarantined { get; } = [];

    public ConcurrentBag<StoredAsset> Confirmed { get; } = [];

    public void Clear()
    {
        Quarantined.Clear();
        Confirmed.Clear();
    }

    public Task<IReadOnlyCollection<StoredAsset>> ListQuarantinedAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<StoredAsset>>([.. Quarantined]);

    public Task<IReadOnlyCollection<StoredAsset>> ListConfirmedAsync(CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyCollection<StoredAsset>>([.. Confirmed]);
}
