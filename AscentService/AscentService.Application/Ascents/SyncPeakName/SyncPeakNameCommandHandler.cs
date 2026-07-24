using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.SyncPeakName;

internal sealed class SyncPeakNameCommandHandler(IAscentRepository ascentRepository, IUnitOfWork unitOfWork)
    : ICommandHandler<SyncPeakNameCommand>
{
    public async Task<Result> Handle(SyncPeakNameCommand command, CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents = await ascentRepository.GetByPeakIdAsync(command.PeakId, cancellationToken);

        foreach (Ascent ascent in ascents)
        {
            ascent.SyncPeak(command.PeakName, command.PeakAltitudeMeters);
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
