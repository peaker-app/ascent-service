using System.Globalization;
using System.Net;
using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Domain.Results;
using Microsoft.Extensions.Logging;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class AccountProfileHttpClient(HttpClient httpClient, ILogger<AccountProfileHttpClient> logger)
    : IProfileDirectory
{
    public async Task<Result<bool>> IsProfilePublicAsync(Guid userId, CancellationToken cancellationToken)
    {
        Uri route = new(string.Create(CultureInfo.InvariantCulture, $"api/profiles/{userId}"), UriKind.Relative);

        try
        {
            using HttpResponseMessage response = await httpClient.GetAsync(route, cancellationToken);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => Result.Success(true),
                HttpStatusCode.NotFound => Result.Success(false),
                _ => Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable)
            };
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "The profile directory is unreachable while resolving {UserId}", userId);

            return Result.Failure<bool>(AscentErrors.ProfileDirectoryUnavailable);
        }
    }
}
