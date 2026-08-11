using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.Ascents.Mappings;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Images;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.AddAscentPhoto;

internal sealed class AddAscentPhotoCommandHandler(
    IAscentRepository ascentRepository,
    IPhotoStorage photoStorage,
    IPhotoUrlSigner photoUrlSigner,
    IImageValidator imageValidator,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<AddAscentPhotoCommand, AscentPhotoResponse>
{
    public async Task<Result<AscentPhotoResponse>> Handle(
        AddAscentPhotoCommand command,
        CancellationToken cancellationToken)
    {
        Ascent? ascent = await ascentRepository.GetByIdAsync(command.AscentId, cancellationToken);
        Result eligibility = CheckEligibility(ascent, command);

        if (eligibility.IsFailure)
        {
            return Result.Failure<AscentPhotoResponse>(eligibility.Error);
        }

        Result<StoredPhoto> stored = await photoStorage.UploadAsync(command.File, cancellationToken);

        if (stored.IsFailure)
        {
            return Result.Failure<AscentPhotoResponse>(stored.Error);
        }

        return await AttachAsync(ascent!, stored.Value, cancellationToken);
    }

    private Result CheckEligibility(Ascent? ascent, AddAscentPhotoCommand command)
    {
        if (ascent is null)
        {
            return Result.Failure(AscentErrors.NotFound(command.AscentId));
        }

        if (!ascent.IsOwnedBy(command.UserId))
        {
            return Result.Failure(AscentErrors.NotOwned);
        }

        return ascent.HasRoomForPhotos
            ? CheckContent(command.File)
            : Result.Failure(AscentErrors.PhotoLimitReached);
    }

    private Result CheckContent(PhotoFile file)
    {
        ImageRejection rejection = imageValidator.Validate(new ImageContent(
            file.Content,
            file.ContentType,
            AddAscentPhotoCommand.MaxSizeInBytes));

        return rejection is ImageRejection.None
            ? Result.Success()
            : Result.Failure(PhotoRejections.ToError(rejection));
    }

    private async Task<Result<AscentPhotoResponse>> AttachAsync(
        Ascent ascent,
        StoredPhoto stored,
        CancellationToken cancellationToken)
    {
        PhotoUpload upload = new(stored.PublicId, stored.Width, stored.Height);
        Result<AscentPhoto> photo = ascent.AddPhoto(upload, dateTimeProvider.UtcNow);

        if (photo.IsFailure)
        {
            await photoStorage.TryDeleteAsync(stored.PublicId, cancellationToken);

            return Result.Failure<AscentPhotoResponse>(photo.Error);
        }

        await SaveOrDiscardAsync(stored, cancellationToken);

        return photo.Value.ToResponse(photoUrlSigner, ascent.Visibility);
    }

    private async Task SaveOrDiscardAsync(StoredPhoto stored, CancellationToken cancellationToken)
    {
        bool persisted = false;

        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            persisted = true;
        }
        finally
        {
            if (!persisted)
            {
                await photoStorage.TryDeleteAsync(stored.PublicId, cancellationToken);
            }
        }
    }
}
