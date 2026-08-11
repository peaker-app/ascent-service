using System.Net.Http.Json;
using AscentService.Application.Ascents.GetAscentById;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class PhotoDeliveryEndpointTests(AscentServiceApiFactory factory)
{
    private const string AuthTokenMarker = "__cld_token__";

    [Fact]
    public async Task AddPhoto_ToAPrivateAscent_ReturnsAnExpiringSignedUrl()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody("Private"));

        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        photo.SecureUrl.Should().Contain(AuthTokenMarker);
    }

    [Fact]
    public async Task AddPhoto_ToAPrivateAscent_NeverReturnsAPubliclyDeliverableUrl()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody("Private"));

        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        photo.SecureUrl.Should().NotContain("/image/upload/");
    }

    [Fact]
    public async Task GetById_AfterTurningTheAscentPrivate_ShortensThePhotoUrlLifetime()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody("Public"));
        await owner.AddPhotoAsync(ascentId);

        long asPublic = await ReadPhotoTokenExpiryAsync(owner, ascentId);
        await owner.PutAsJsonAsync(ApiTestHelpers.AscentRoute(ascentId), UpdateBody("Private"));
        long asPrivate = await ReadPhotoTokenExpiryAsync(owner, ascentId);

        asPrivate.Should().BeLessThan(asPublic);
    }

    [Fact]
    public async Task AddPhoto_LeavesTheAssetInQuarantineUntilTheOutboxConfirmsIt()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody("Public"));
        await owner.AddPhotoAsync(ascentId);
        string publicId = (await factory.ReadPhotoPublicIdsAsync(ascentId)).Single();

        bool confirmed = await WaitForConfirmationAsync(publicId);

        confirmed.Should().BeTrue();
    }

    private static async Task<long> ReadPhotoTokenExpiryAsync(HttpClient client, Guid ascentId)
    {
        AscentResponse? ascent = await client.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        return ExtractExpiry(ascent!.Photos.Single().SecureUrl);
    }

    private static long ExtractExpiry(string signedUrl)
    {
        int start = signedUrl.IndexOf("exp=", StringComparison.Ordinal) + 4;
        int end = signedUrl.IndexOf('~', start);

        return long.Parse(
            end < 0 ? signedUrl[start..] : signedUrl[start..end],
            System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task<bool> WaitForConfirmationAsync(string publicId)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (factory.PhotoStorage.ConfirmedPublicIds.Contains(publicId))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private object RegisterBody(string visibility) => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate = "2026-07-01",
        visibility
    };

    private static object UpdateBody(string visibility) => new
    {
        ascentDate = "2026-07-01",
        visibility
    };
}
