using Common.Application.Messaging;

namespace AscentService.Application.Ascents.SweepOrphanedPhotos;

public sealed record SweepOrphanedPhotosCommand(DateTime UploadedBeforeUtc) : ICommand<PhotoSweepResponse>;
