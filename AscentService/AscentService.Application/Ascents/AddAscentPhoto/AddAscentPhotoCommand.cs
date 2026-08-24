using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.GetAscentById;
using Common.Application.Messaging;

namespace AscentService.Application.Ascents.AddAscentPhoto;

public sealed record AddAscentPhotoCommand(Guid AscentId, Guid UserId, PhotoFile File)
    : ICommand<AscentPhotoResponse>
{
    public const int MaxSizeInBytes = 10 * 1024 * 1024;

    public const int MaxRequestSizeInBytes = MaxSizeInBytes + (64 * 1024);
}
