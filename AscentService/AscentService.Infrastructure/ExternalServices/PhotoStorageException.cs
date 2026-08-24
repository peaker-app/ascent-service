namespace AscentService.Infrastructure.ExternalServices;

public sealed class PhotoStorageException : Exception
{
    public PhotoStorageException()
    {
    }

    public PhotoStorageException(string message) : base(message)
    {
    }

    public PhotoStorageException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
