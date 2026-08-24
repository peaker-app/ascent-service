namespace AscentService.Application.Abstractions;

public interface IConfirmedUserDirectory
{
    Task<bool> IsConfirmedAsync(Guid userId, CancellationToken cancellationToken);
}
