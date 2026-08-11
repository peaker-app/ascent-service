using System.Globalization;
using AscentService.Application.Abstractions;
using CloudinaryDotNet.Actions;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class CloudinaryPhotoInventory(CloudinaryFactory cloudinaryFactory) : IPhotoAssetInventory
{
    private const int PageSize = 500;
    private const int MaxPages = 20;

    public Task<IReadOnlyCollection<StoredAsset>> ListQuarantinedAsync(CancellationToken cancellationToken) =>
        CollectAsync(
            new AssetQuery(BuildQuarantineParameters, IsAuthenticated),
            cancellationToken);

    public Task<IReadOnlyCollection<StoredAsset>> ListConfirmedAsync(CancellationToken cancellationToken) =>
        CollectAsync(
            new AssetQuery(BuildFolderParameters, resource => IsAuthenticated(resource) && !IsQuarantined(resource)),
            cancellationToken);

    private static ListResourcesByTagParams BuildQuarantineParameters(string? cursor) =>
        new()
        {
            Tag = PhotoDelivery.QuarantineTag,
            Tags = true,
            MaxResults = PageSize,
            NextCursor = cursor
        };

    private ListResourcesByPrefixParams BuildFolderParameters(string? cursor) =>
        new()
        {
            Prefix = cloudinaryFactory.Options.Folder,
            Type = PhotoDelivery.AuthenticatedType,
            Tags = true,
            MaxResults = PageSize,
            NextCursor = cursor
        };

    private static bool IsAuthenticated(Resource resource) =>
        string.Equals(resource.Type, PhotoDelivery.AuthenticatedType, StringComparison.Ordinal);

    private static bool IsQuarantined(Resource resource) =>
        resource.Tags is not null && Array.Exists(
            resource.Tags,
            tag => string.Equals(tag, PhotoDelivery.QuarantineTag, StringComparison.Ordinal));

    private static StoredAsset ToStoredAsset(Resource resource) =>
        new(resource.PublicId, ParseCreatedAtUtc(resource.CreatedAt));

    private static DateTime ParseCreatedAtUtc(string? createdAt) =>
        DateTime.TryParse(
            createdAt,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
            out DateTime parsed)
            ? parsed
            : DateTime.MaxValue;

    private async Task<IReadOnlyCollection<StoredAsset>> CollectAsync(
        AssetQuery query,
        CancellationToken cancellationToken)
    {
        List<StoredAsset> assets = [];
        string? cursor = null;

        for (int page = 0; page < MaxPages; page++)
        {
            ListResourcesResult result = await cloudinaryFactory.Client
                .ListResourcesAsync(query.BuildParameters(cursor), cancellationToken);

            assets.AddRange((result.Resources ?? []).Where(query.Matches).Select(ToStoredAsset));
            cursor = result.NextCursor;

            if (string.IsNullOrEmpty(cursor))
            {
                break;
            }
        }

        return assets;
    }

    private sealed record AssetQuery(
        Func<string?, ListResourcesParams> BuildParameters,
        Func<Resource, bool> Matches);
}
