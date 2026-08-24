using Common.Application.Messaging;

namespace AscentService.Application.Ascents.ExportMyData;

public sealed record ExportMyAscentsQuery(Guid UserId) : IQuery<AscentExportResponse>;
