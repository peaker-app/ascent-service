namespace AscentService.Application.Abstractions;

public interface IDeletedUserDirectory
{
    Task<bool> IsDeletedAsync(Guid userId, CancellationToken cancellationToken);
}
