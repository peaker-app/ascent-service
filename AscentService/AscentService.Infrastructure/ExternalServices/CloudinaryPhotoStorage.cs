using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Common.Domain.Results;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class CloudinaryPhotoStorage : IPhotoStorage
{
    private const string DeletedOutcome = "ok";

    // Motivo: el outbox reentrega la compensación hasta confirmarla; una foto ya borrada no es un fallo.
    private const string MissingOutcome = "not found";

    private readonly Cloudinary _cloudinary;
    private readonly CloudinaryOptions _options;
    private readonly ILogger<CloudinaryPhotoStorage> _logger;

    public CloudinaryPhotoStorage(IOptions<CloudinaryOptions> options, ILogger<CloudinaryPhotoStorage> logger)
    {
        _options = options.Value;
        _logger = logger;
        _cloudinary = new Cloudinary(new Account(_options.CloudName, _options.ApiKey, _options.ApiSecret));
        _cloudinary.Api.Secure = true;
    }

    public async Task<Result<StoredPhoto>> UploadAsync(PhotoFile file, CancellationToken cancellationToken)
    {
        using var stream = new MemoryStream(file.Content.ToArray());

        var uploadParameters = new ImageUploadParams
        {
            File = new FileDescription(file.FileName, stream),
            Folder = _options.Folder,
            Overwrite = false
        };

        ImageUploadResult result = await _cloudinary.UploadAsync(uploadParameters, cancellationToken);

        if (result.Error is not null)
        {
            _logger.LogWarning("Cloudinary photo upload failed: {ErrorMessage}", result.Error.Message);
            return Result.Failure<StoredPhoto>(AscentErrors.PhotoUploadFailed);
        }

        return new StoredPhoto(result.PublicId, result.SecureUrl.ToString(), result.Width, result.Height);
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        // Motivo: DestroyAsync de CloudinaryDotNet 1.27.7 no admite CancellationToken.
        cancellationToken.ThrowIfCancellationRequested();

        DeletionResult result = await _cloudinary.DestroyAsync(new DeletionParams(publicId));

        if (IsConfirmed(result))
        {
            return;
        }

        _logger.LogWarning(
            "Cloudinary photo deletion was not confirmed for {PublicId}: {Outcome}",
            publicId,
            result.Error?.Message ?? result.Result);

        throw new PhotoStorageException($"Cloudinary did not confirm the deletion of '{publicId}'.");
    }

    private static bool IsConfirmed(DeletionResult result) =>
        result.Error is null &&
        (string.Equals(result.Result, DeletedOutcome, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(result.Result, MissingOutcome, StringComparison.OrdinalIgnoreCase));
}
