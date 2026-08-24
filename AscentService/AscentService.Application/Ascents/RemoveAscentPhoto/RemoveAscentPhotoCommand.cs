using Common.Application.Messaging;

namespace AscentService.Application.Ascents.RemoveAscentPhoto;

public sealed record RemoveAscentPhotoCommand(Guid AscentId, Guid PhotoId, Guid UserId) : ICommand;
