using System.Net;
using System.Net.Http.Json;
using AscentService.Application.Ascents.AddAscentPhoto;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Domain.Ascents;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class AscentPhotoEndpointTests(AscentServiceApiFactory factory)
{
    private static readonly byte[] PdfBytes = [0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34];
    private static readonly byte[] PngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x01];

    private static readonly byte[] SvgBytes = System.Text.Encoding.UTF8.GetBytes(
        "<svg xmlns=\"http://www.w3.org/2000/svg\"><script>alert(1)</script></svg>");

    [Fact]
    public async Task AddPhoto_WithAValidJpeg_Returns201()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AddPhoto_WithAValidJpeg_ExposesItInTheAscentDetail()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        await owner.AddPhotoAsync(ascentId);

        AscentResponse? ascent = await owner.GetFromJsonAsync<AscentResponse>(
            ApiTestHelpers.AscentRoute(ascentId));

        ascent!.Photos.Should().ContainSingle();
    }

    [Fact]
    public async Task AddPhoto_UpToTheLimit_AssignsConsecutivePositions()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
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
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
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
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, PdfBytes);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhoto_WithAnSvgDisguisedAsJpeg_Returns400()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(
            ascentId, SvgBytes, "image/jpeg", "cumbre.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhoto_WithAPngDeclaredAsJpeg_Returns400()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(
            ascentId, PngBytes, "image/jpeg", "cumbre.jpg");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task AddPhoto_ByAnotherUser_Returns403()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AddPhoto_OnAnUnknownAscent_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

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
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        HttpResponseMessage response = await owner.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task RemovePhoto_InTheMiddle_ReindexesTheSurvivorsWithoutGaps()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
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
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);
        string publicId = await PublicIdOfAsync(ascentId);

        await owner.DeleteAsync($"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        bool deleted = await WaitForDeletionAsync(publicId);

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task RemovePhoto_WithAnUnknownPhoto_Returns404()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{Guid.CreateVersion7()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RemovePhoto_ByAnotherUser_Returns403()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);

        using HttpClient stranger = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        HttpResponseMessage response = await stranger.DeleteAsync(
            $"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task RemovePhoto_WhenRemoteStorageFails_StillRemovesThePhotoLocally()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);
        factory.PhotoStorage.FailNextDeletions(await PublicIdOfAsync(ascentId), attempts: 2);

        await owner.DeleteAsync($"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        IReadOnlyList<string> remaining = await factory.ReadPhotoPublicIdsAsync(ascentId);

        remaining.Should().BeEmpty();
    }

    [Fact]
    public async Task RemovePhoto_WhenRemoteStorageRecovers_RetriesTheRemoteDeletion()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());
        AscentPhotoResponse photo = await owner.AddPhotoAsync(ascentId);
        string publicId = await PublicIdOfAsync(ascentId);
        factory.PhotoStorage.FailNextDeletions(publicId, attempts: 1);

        await owner.DeleteAsync($"{ApiTestHelpers.AscentRoute(ascentId)}/photos/{photo.Id}");

        bool deleted = await WaitForDeletionAsync(publicId);

        deleted.Should().BeTrue();
    }

    [Fact]
    public async Task AddPhoto_WithAnOversizedImage_Returns400()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await owner.RegisterAscentAsync(RegisterBody());

        HttpResponseMessage response = await owner.UploadPhotoAsync(ascentId, OversizedJpeg());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static byte[] OversizedJpeg()
    {
        byte[] content = new byte[AddAscentPhotoCommand.MaxSizeInBytes + 1];
        ApiTestHelpers.JpegBytes.CopyTo(content, 0);

        return content;
    }

    private async Task<string> PublicIdOfAsync(Guid ascentId) =>
        (await factory.ReadPhotoPublicIdsAsync(ascentId)).Single();

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
