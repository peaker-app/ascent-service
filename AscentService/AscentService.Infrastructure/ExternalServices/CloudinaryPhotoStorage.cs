using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Common.Domain.Results;
using Microsoft.Extensions.Logging;

namespace AscentService.Infrastructure.ExternalServices;

internal sealed class CloudinaryPhotoStorage(
    CloudinaryFactory cloudinaryFactory,
    ILogger<CloudinaryPhotoStorage> logger) : IPhotoStorage
{
    private const string DeletedOutcome = "ok";
    private const string MissingOutcome = "not found";

    public async Task<Result<StoredPhoto>> UploadAsync(PhotoFile file, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new(file.Content.ToArray());

        ImageUploadParams uploadParameters = new()
        {
            File = new FileDescription(file.FileName, stream),
            Folder = cloudinaryFactory.Options.Folder,
            Type = PhotoDelivery.AuthenticatedType,
            Tags = PhotoDelivery.QuarantineTag,
            Format = PhotoDelivery.StoredFormat,
            Transformation = PhotoDelivery.Sanitizing(),
            Overwrite = false
        };

        ImageUploadResult result = await cloudinaryFactory.Client.UploadAsync(uploadParameters, cancellationToken);

        if (result.Error is not null)
        {
            logger.LogWarning("Cloudinary photo upload failed: {ErrorMessage}", result.Error.Message);
            return Result.Failure<StoredPhoto>(AscentErrors.PhotoUploadFailed);
        }

        return new StoredPhoto(result.PublicId, result.Width, result.Height);
    }

    public async Task ConfirmAsync(string publicId, CancellationToken cancellationToken)
    {
        TagParams tagParameters = new()
        {
            Command = TagCommand.Remove,
            Tag = PhotoDelivery.QuarantineTag,
            Type = PhotoDelivery.AuthenticatedType,
            PublicIds = [publicId]
        };

        TagResult result = await cloudinaryFactory.Client.TagAsync(tagParameters, cancellationToken);

        if (result.Error is null)
        {
            return;
        }

        logger.LogWarning(
            "Cloudinary photo {PublicId} could not leave quarantine: {ErrorMessage}",
            publicId,
            result.Error.Message);

        throw new PhotoStorageException($"Cloudinary did not confirm the storage of '{publicId}'.");
    }

    public async Task DeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DeletionParams deletionParameters = new(publicId)
        {
            Type = PhotoDelivery.AuthenticatedType,
            Invalidate = true
        };

        DeletionResult result = await cloudinaryFactory.Client.DestroyAsync(deletionParameters);

        if (IsConfirmed(result))
        {
            return;
        }

        logger.LogWarning(
            "Cloudinary photo deletion was not confirmed for {PublicId}: {Outcome}",
            publicId,
            result.Error?.Message ?? result.Result);

        throw new PhotoStorageException($"Cloudinary did not confirm the deletion of '{publicId}'.");
    }

    public async Task TryDeleteAsync(string publicId, CancellationToken cancellationToken)
    {
        try
        {
            await DeleteAsync(publicId, cancellationToken);
        }
        catch (PhotoStorageException exception)
        {
            logger.LogError(exception, "Orphaned Cloudinary photo {PublicId} could not be removed", publicId);
        }
    }

    private static bool IsConfirmed(DeletionResult result) =>
        result.Error is null &&
        (string.Equals(result.Result, DeletedOutcome, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(result.Result, MissingOutcome, StringComparison.OrdinalIgnoreCase));
}
