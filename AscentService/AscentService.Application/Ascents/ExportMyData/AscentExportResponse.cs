using AscentService.Application.Ascents.GetAscentById;

namespace AscentService.Application.Ascents.ExportMyData;

public sealed record AscentExportResponse(
    Guid UserId,
    int TotalAscents,
    IReadOnlyList<AscentResponse> Ascents);
