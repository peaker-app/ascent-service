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
        var deletionParameters = new DeletionParams(publicId);

        await _cloudinary.DestroyAsync(deletionParameters);
    }
}
