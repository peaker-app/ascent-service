using System.Net;
using AscentService.Domain.Ascents;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class AscentPhotoConcurrencyTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task AddPhoto_WithTwoSimultaneousUploadsOnTheThirdSlot_RejectsOneWithAConflict()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await GivenAnAscentWithTwoPhotosAsync(owner);

        IReadOnlyList<HttpStatusCode> codes = await UploadTwiceAtOnceAsync(owner, ascentId);

        codes.Should().Contain(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddPhoto_WithTwoSimultaneousUploadsOnTheThirdSlot_NeverReturns500()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await GivenAnAscentWithTwoPhotosAsync(owner);

        IReadOnlyList<HttpStatusCode> codes = await UploadTwiceAtOnceAsync(owner, ascentId);

        codes.Should().NotContain(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task AddPhoto_WithTwoSimultaneousUploadsOnTheThirdSlot_LeavesExactlyThreePhotos()
    {
        using HttpClient owner = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        Guid ascentId = await GivenAnAscentWithTwoPhotosAsync(owner);

        await UploadTwiceAtOnceAsync(owner, ascentId);
        IReadOnlyList<string> photos = await factory.ReadPhotoPublicIdsAsync(ascentId);

        photos.Should().HaveCount(Ascent.MaxPhotos);
    }

    private static async Task<IReadOnlyList<HttpStatusCode>> UploadTwiceAtOnceAsync(
        HttpClient owner,
        Guid ascentId)
    {
        Task<HttpResponseMessage> first = owner.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);
        Task<HttpResponseMessage> second = owner.UploadPhotoAsync(ascentId, ApiTestHelpers.JpegBytes);

        HttpResponseMessage[] responses = await Task.WhenAll(first, second);

        return [.. responses.Select(response => response.StatusCode)];
    }

    private async Task<Guid> GivenAnAscentWithTwoPhotosAsync(HttpClient owner)
    {
        PeakSnapshot peak = factory.PeakCatalog.Register("Aneto", 3404);
        Guid ascentId = await owner.RegisterAscentAsync(
            new { peakId = peak.PeakId, ascentDate = "2026-07-01" });

        await owner.AddPhotoAsync(ascentId);
        await owner.AddPhotoAsync(ascentId);

        return ascentId;
    }
}
