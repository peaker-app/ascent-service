using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Domain.Ascents;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class UpdateAndDeleteAscentEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Update_ByTheOwner_Returns204()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(ascentId), UpdateBody());

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Update_ByTheOwner_PersistsTheNewValues()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        await owner.PutAsJsonAsync(ApiTestHelpers.AscentRoute(ascentId), UpdateBody());

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent.Should().BeEquivalentTo(new { Companions = "Solo", RouteNotes = "Arista norte" });
    }

    [Fact]
    public async Task Update_WithoutVisibility_ResetsTheAscentToPublic()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody(AscentVisibility.Private));

        await owner.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(ascentId), new { ascentDate = "2026-07-02" });

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Visibility.Should().Be(nameof(AscentVisibility.Public));
    }

    [Fact]
    public async Task Update_WithAFutureDate_Returns400()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(ascentId), new { ascentDate = "2099-01-01" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_ByAnotherUser_Returns403()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(ascentId), UpdateBody());

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Update_WithAnUnknownAscent_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await owner.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(Guid.CreateVersion7()), UpdateBody());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithoutAToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(
            ApiTestHelpers.AscentRoute(Guid.CreateVersion7()), UpdateBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Delete_ByTheOwner_Returns204()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.DeleteAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Delete_ByTheOwner_MakesTheAscentUnreachable()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        await owner.DeleteAsync(ApiTestHelpers.AscentRoute(ascentId));
        HttpResponseMessage response = await owner.GetAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ByTheOwner_RemovesItsPhotosFromRemoteStorage()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        await owner.DeleteAsync(ApiTestHelpers.AscentRoute(ascentId));

        bool compensated = await WaitForDeletionAsync(photo.SecureUrl);

        compensated.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ByAnotherUser_Returns403()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.DeleteAsync(ApiTestHelpers.AscentRoute(ascentId));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Delete_WithAnUnknownAscent_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await owner.DeleteAsync(
            ApiTestHelpers.AscentRoute(Guid.CreateVersion7()));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<bool> WaitForDeletionAsync(string secureUrl)
    {
        string publicId = secureUrl.Replace("https://res.cloudinary.test/", string.Empty, StringComparison.Ordinal)
            .Replace(".jpg", string.Empty, StringComparison.Ordinal);

        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (factory.PhotoStorage.DeletedPublicIds.Any(id => id.EndsWith(publicId, StringComparison.Ordinal)))
            {
                return true;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        return false;
    }

    private object RegisterBody(AscentVisibility visibility = AscentVisibility.Public) => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate = "2026-07-01",
        visibility = visibility.ToString()
    };

    private static object UpdateBody() => new
    {
        ascentDate = "2026-07-02",
        companions = "Solo",
        routeNotes = "Arista norte",
        visibility = nameof(AscentVisibility.Public)
    };
}
