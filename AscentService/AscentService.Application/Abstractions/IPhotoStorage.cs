using Common.Domain.Results;

namespace AscentService.Application.Abstractions;

public sealed record PhotoFile(ReadOnlyMemory<byte> Content, string ContentType, string FileName);

public sealed record StoredPhoto(string PublicId, string SecureUrl, int Width, int Height);

public interface IPhotoStorage
{
    Task<Result<StoredPhoto>> UploadAsync(PhotoFile file, CancellationToken cancellationToken);

    Task DeleteAsync(string publicId, CancellationToken cancellationToken);
}
