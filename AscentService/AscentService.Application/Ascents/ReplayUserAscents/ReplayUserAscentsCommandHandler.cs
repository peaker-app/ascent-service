using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.ReplayUserAscents;

internal sealed class ReplayUserAscentsCommandHandler(
    IAscentRepository ascentRepository,
    IUnitOfWork unitOfWork) : ICommandHandler<ReplayUserAscentsCommand, AscentReplayResponse>
{
    public async Task<Result<AscentReplayResponse>> Handle(
        ReplayUserAscentsCommand command,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByUserIdAsync(command.UserId, cancellationToken);

        foreach (Ascent ascent in ascents)
        {
            ascent.Republish();
        }

        if (ascents.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new AscentReplayResponse(command.UserId, ascents.Count);
    }
}
