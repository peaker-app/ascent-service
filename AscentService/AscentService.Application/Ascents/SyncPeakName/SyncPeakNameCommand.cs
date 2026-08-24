using Common.Application.Messaging;

namespace AscentService.Application.Ascents.SyncPeakName;

public sealed record SyncPeakNameCommand(Guid PeakId, string PeakName, int PeakAltitudeMeters) : ICommand;
