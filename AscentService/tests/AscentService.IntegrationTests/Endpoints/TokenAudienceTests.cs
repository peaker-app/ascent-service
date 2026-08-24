using System.Net;
using FluentAssertions;
using Xunit;

namespace AscentService.IntegrationTests.Endpoints;

[Collection(nameof(AscentServiceCollection))]
public sealed class TokenAudienceTests(AscentServiceApiFactory factory)
{
    [Fact]
    public async Task ProtectedRoute_WithATokenIssuedForAnotherService_Returns401()
    {
        using HttpClient client = factory.CreateClientWithAudience(ApiTestHelpers.NewUserId(), "peaker-account");

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/ascents", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ProtectedRoute_WithATokenIssuedForThisService_IsAccepted()
    {
        using HttpClient client = factory.CreateClientWithAudience(ApiTestHelpers.NewUserId(), "peaker-ascent");

        using HttpResponseMessage response = await client.GetAsync(new Uri("/api/ascents", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
