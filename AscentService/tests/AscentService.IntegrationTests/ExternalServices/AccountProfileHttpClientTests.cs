using System.Net;
using AscentService.Domain.Ascents;
using AscentService.Infrastructure.ExternalServices;
using AscentService.IntegrationTests.Fakes;
using Common.Domain.Results;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace AscentService.IntegrationTests.ExternalServices;

public sealed class AccountProfileHttpClientTests
{
    private static readonly Guid UserId = Guid.Parse("0198f000-0000-7000-8000-000000000001");

    [Fact]
    public async Task IsProfilePublicAsync_WhenTheProfileAnswers200_ReportsAPublicProfile()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Responding(HttpStatusCode.OK, "{}");

        Result<bool> result = await DirectoryOver(handler).IsProfilePublicAsync(UserId, CancellationToken.None);

        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task IsProfilePublicAsync_WhenTheProfileAnswers404_ReportsANonPublicProfile()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Responding(HttpStatusCode.NotFound);

        Result<bool> result = await DirectoryOver(handler).IsProfilePublicAsync(UserId, CancellationToken.None);

        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task IsProfilePublicAsync_WhenTheProfileAnswers500_ReturnsProfileDirectoryUnavailable()
    {
        using StubHttpMessageHandler handler =
            StubHttpMessageHandler.Responding(HttpStatusCode.InternalServerError);

        Result<bool> result = await DirectoryOver(handler).IsProfilePublicAsync(UserId, CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    [Fact]
    public async Task IsProfilePublicAsync_WhenTheDirectoryIsUnreachable_ReturnsProfileDirectoryUnavailable()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Failing(new HttpRequestException("down"));

        Result<bool> result = await DirectoryOver(handler).IsProfilePublicAsync(UserId, CancellationToken.None);

        result.Error.Should().Be(AscentErrors.ProfileDirectoryUnavailable);
    }

    [Fact]
    public async Task IsProfilePublicAsync_WhenTheDirectoryTimesOut_ReturnsProfileDirectoryUnavailable()
    {
        using StubHttpMessageHandler handler = StubHttpMessageHandler.Failing(new TaskCanceledException("timeout"));

        Result<bool> result = await DirectoryOver(handler).IsProfilePublicAsync(UserId, CancellationToken.None);

        result.Error.Type.Should().Be(ErrorType.Unavailable);
    }

    private static AccountProfileHttpClient DirectoryOver(HttpMessageHandler handler) =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("http://account-service.test/") },
            NullLogger<AccountProfileHttpClient>.Instance);
}
