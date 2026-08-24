using System.Globalization;
using AscentService.Application.Abstractions;
using Common.Domain.Results;
using Microsoft.Extensions.Caching.Memory;

namespace AscentService.Infrastructure.ExternalServices;

public sealed class CachingProfileDirectory(IProfileDirectory inner, IMemoryCache cache) : IProfileDirectory
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(60);

    public async Task<Result<bool>> IsProfilePublicAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(KeyFor(userId), out bool cached))
        {
            return cached;
        }

        Result<bool> visibility = await inner.IsProfilePublicAsync(userId, cancellationToken);

        if (visibility.IsSuccess)
        {
            cache.Set(KeyFor(userId), visibility.Value, Lifetime);
        }

        return visibility;
    }

    public static void Evict(IMemoryCache cache, Guid userId) => cache.Remove(KeyFor(userId));

    private static string KeyFor(Guid userId) =>
        string.Create(CultureInfo.InvariantCulture, $"profile-directory:{userId}");
}
