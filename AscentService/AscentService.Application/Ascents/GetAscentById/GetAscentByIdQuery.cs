using Common.Application.Messaging;

namespace AscentService.Application.Ascents.GetAscentById;

public sealed record GetAscentByIdQuery(Guid AscentId, Guid? RequesterId) : IQuery<AscentResponse>;
