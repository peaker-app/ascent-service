using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.RegisterAscent;

internal sealed class RegisterAscentCommandHandler(
    IAscentRepository ascentRepository,
    IPeakCatalog peakCatalog,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterAscentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterAscentCommand command, CancellationToken cancellationToken)
    {
        Result<PeakSnapshot> peak = await peakCatalog.GetSnapshotAsync(command.PeakId, cancellationToken);

        if (peak.IsFailure)
        {
            return Result.Failure<Guid>(peak.Error);
        }

        AscentDraft draft = new(command.UserId, peak.Value, command.ToDetails());
        Result<Ascent> ascent = Ascent.Create(draft, dateTimeProvider.Today);

        if (ascent.IsFailure)
        {
            return Result.Failure<Guid>(ascent.Error);
        }

        ascentRepository.Add(ascent.Value);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return ascent.Value.Id;
    }
}
