using AscentService.Application.Abstractions;
using AscentService.Domain.Ascents;
using Common.Application.Abstractions;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.RegisterAscent;

internal sealed class RegisterAscentCommandHandler(
    IAscentRepository ascentRepository,
    IPeakCatalog peakCatalog,
    IConfirmedUserDirectory confirmedUserDirectory,
    IDeletedUserDirectory deletedUserDirectory,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider) : ICommandHandler<RegisterAscentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(RegisterAscentCommand command, CancellationToken cancellationToken)
    {
        Result eligibility = await CheckAccountAsync(command.UserId, cancellationToken);

        if (eligibility.IsFailure)
        {
            return Result.Failure<Guid>(eligibility.Error);
        }

        Ascent? alreadyRegistered = await FindAlreadyRegisteredAsync(command, cancellationToken);

        return alreadyRegistered is not null
            ? alreadyRegistered.Id
            : await RegisterNewAsync(command, cancellationToken);
    }

    private async Task<Result> CheckAccountAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (await deletedUserDirectory.IsDeletedAsync(userId, cancellationToken))
        {
            return Result.Failure(AscentErrors.AccountDeleted);
        }

        return await confirmedUserDirectory.IsConfirmedAsync(userId, cancellationToken)
            ? Result.Success()
            : Result.Failure(AscentErrors.EmailNotConfirmed);
    }

    private Task<Ascent?> FindAlreadyRegisteredAsync(
        RegisterAscentCommand command,
        CancellationToken cancellationToken) =>
        command.DeduplicationKey is { } key
            ? ascentRepository.GetByClientAscentIdAsync(command.UserId, key, cancellationToken)
            : Task.FromResult<Ascent?>(null);

    private async Task<Result<Guid>> RegisterNewAsync(
        RegisterAscentCommand command,
        CancellationToken cancellationToken)
    {
        Result<PeakSnapshot> peak = await peakCatalog.GetSnapshotAsync(command.PeakId, cancellationToken);

        if (peak.IsFailure)
        {
            return Result.Failure<Guid>(peak.Error);
        }

        AscentDraft draft = new(
            command.UserId, peak.Value, command.ToDetails(), command.DeduplicationKey);

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
