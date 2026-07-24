using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Domain.Ascents;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class AscentPhotoEndpointTests(AscentServiceApiFactory factory)
{
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];

    [Fact]
    public async Task AddPhoto_WithAValidJpeg_Returns201()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddPhoto_WithAValidJpeg_ExposesItInTheAscentDetail()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        await owner.AddPhotoAsync(ascentId);

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task AddPhoto_UpToTheLimit_AssignsConsecutivePositions()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        await owner.AddPhotoAsync(ascentId);
        await owner.AddPhotoAsync(ascentId);
        await owner.AddPhotoAsync(ascentId);

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Photos.Select(photo => photo.Position).Should().Equal((short)0, (short)1, (short)2);
    }

    [Fact]
    public async Task AddPhoto_BeyondTheLimit_Returns409()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        for (int index = 0; index < Ascent.MaxPhotos; index++)
        {
            await owner.AddPhotoAsync(ascentId);
        }

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddPhoto_WithAnUnsupportedFormat_Returns400()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, PdfBytes);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhoto_ByAnotherUser_Returns403()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        using HttpClient stranger = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPhoto_OnAnUnknownAscent_Returns404()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());

        HttpResponseMessage response = await owner.UploadPhotoAsync(
            Guid.CreateVersion7(), ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddPhoto_WithoutAToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.UploadPhotoAsync(
            Guid.CreateVersion7(), ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RemovePhoto_ByTheOwner_Returns204()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        HttpResponseMessage response = await owner.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemovePhoto_InTheMiddle_ReindexesTheSurvivorsWithoutGaps()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        await owner.AddPhotoAsync(ascentId);
        AscentPhotoResponse middle = await owner.AddPhotoAsync(ascentId);
        await owner.AddPhotoAsync(ascentId);

        await owner.DeleteAsync($"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{middle.Id}");

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Photos.Select(photo => photo.Position).Should().Equal((short)0, (short)1);
    }

    [Fact]
    public async Task RemovePhoto_ByTheOwner_DeletesItFromRemoteStorage()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);
        string publicId = PublicIdOf(photo);

        await owner.DeleteAsync($"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        bool deleted = await WaitForDeletionAsync(publicId);

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task RemovePhoto_WithAnUnknownPhoto_Returns404()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemovePhoto_ByAnotherUser_Returns403()
    {
        using HttpClient owner = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        using HttpClient stranger = factory.CreateAuthenticatedClient(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private static string PublicIdOf(AscentPhotoResponse photo) =>
        photo.SecureUrl.Replace("https://res.cloudinary.test/", string.Empty, StringComparison.Ordinal)
            .Replace(".jpg", string.Empty, StringComparison.Ordinal);

    private async Task<bool> WaitForDeletionAsync(string publicId)
    {
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

    private object RegisterBody() => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate = "2026-07-01"
    };
}
