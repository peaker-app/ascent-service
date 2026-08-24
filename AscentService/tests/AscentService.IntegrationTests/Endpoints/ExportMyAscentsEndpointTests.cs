using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AscentService.Domain.Ascents;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class ExportMyAscentsEndpointTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task Export_WithoutAToken_Returns401()
    {
        using HttpClient client = factory.CreateClient();

        using HttpResponseMessage response = await client.GetAsync(
            new Uri("/api/ascents/me/export", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Export_ReturnsEveryAscentIncludingThePrivateOnes()
    {
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Public));
        await client.RegisterAscentAsync(Body(visibility: AscentVisibility.Private));

        JsonElement export = await client.GetFromJsonAsync<JsonElement>("/api/ascents/me/export");

        export.GetProperty("totalAscents").GetInt32().Should().Be(2);
        export.GetProperty("ascents").GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task Export_NeverIncludesAnotherClimbersAscents()
    {
        using HttpClient mine = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        using HttpClient theirs = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());
        await mine.RegisterAscentAsync(Body());
        await theirs.RegisterAscentAsync(Body());
        await theirs.RegisterAscentAsync(Body());

        JsonElement export = await mine.GetFromJsonAsync<JsonElement>("/api/ascents/me/export");

        export.GetProperty("totalAscents").GetInt32().Should().Be(1);
    }

    [Fact]
    public async Task Export_WithoutAscents_ReturnsAnEmptyCollectionRatherThanAnError()
    {
        using HttpClient client = await factory.CreateConfirmedClientAsync(ApiTestHelpers.NewUserId());

        JsonElement export = await client.GetFromJsonAsync<JsonElement>("/api/ascents/me/export");

        export.GetProperty("totalAscents").GetInt32().Should().Be(0);
        export.GetProperty("ascents").GetArrayLength().Should().Be(0);
    }

    private object Body(
        string ascentDate = "2026-07-01",
        AscentVisibility visibility = AscentVisibility.Public) => new
    {
        peakId = factory.PeakCatalog.Register().PeakId,
        ascentDate,
        visibility = visibility.ToString()
    };
}
