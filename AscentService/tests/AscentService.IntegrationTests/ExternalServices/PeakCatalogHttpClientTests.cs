using System.Net;
using AscentService.Domain.Ascents;
using AscentService.Infrastructure.ExternalServices;
using AscentService.IntegrationTests.Fakes;
using Common.Domain.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AscentService.IntegrationTests.ExternalServices;

public sealed class PeakCatalogHttpClientTests
{
    private static readonly Guid PeakId = Guid.Parse("0198f000-0000-7000-8000-0000000000a1");

    [Fact]
    public async Task GetSnapshotAsync_WhenTheCatalogAnswers200_MapsTheSnapshot()
    {
        const string body = """{"id":"0198f000-0000-7000-8000-0000000000a1","name":"Aneto","altitudeMeters":3404}""";
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Responding(HttpStatusCode.OK, body);

        Result<PeakSnapshot> result = await CatalogOver(handler).GetSnapshotAsync(PeakId, CancellationToken.None);

        result.Value.Should().Be(new PeakSnapshot(PeakId, "Aneto", 3404));
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenTheCatalogAnswers404_ReturnsPeakNotFound()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Responding(HttpStatusCode.NotFound);

        Result<PeakSnapshot> result = await CatalogOver(handler).GetSnapshotAsync(PeakId, CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PeakNotFound(PeakId));
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenTheCatalogAnswers500_ReturnsPeakCatalogUnavailable()
    {
        using StubHttpMessageHandler handler =
            StubHttpMessageHandler.Responding(HttpStatusCode.InternalServerError);

        Result<PeakSnapshot> result = await CatalogOver(handler).GetSnapshotAsync(PeakId, CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenTheCatalogIsUnreachable_ReturnsPeakCatalogUnavailable()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Failing(new HttpRequestException("down"));

        Result<PeakSnapshot> result = await CatalogOver(handler).GetSnapshotAsync(PeakId, CancellationToken.None);

        result.Error.Should().Be(AscentErrors.PeakCatalogUnavailable);
    }

    [Fact]
    public async Task GetSnapshotAsync_WhenTheCatalogTimesOut_ReturnsPeakCatalogUnavailable()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Failing(new TaskCanceledException("timeout"));

        Result<PeakSnapshot> result = await CatalogOver(handler).GetSnapshotAsync(PeakId, CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    private static PeakCatalogHttpClient CatalogOver(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://peak-service.test/") },
            NullLogger<PeakCatalogHttpClient>.Instance);
}
