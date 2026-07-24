using Common.Domain.Results;

namespace AscentService.Application.Abstractions;

public interface IProfileDirectory
{
    Task<Result<bool>> IsProfilePublicAsync(Guid userId, CancellationToken cancellationToken);
}
