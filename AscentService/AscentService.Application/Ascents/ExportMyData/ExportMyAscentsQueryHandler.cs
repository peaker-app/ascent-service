using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.Mappings;
using AscentService.Domain.Ascents;
using Common.Application.Messaging;
using Common.Domain.Results;

namespace AscentService.Application.Ascents.ExportMyData;

internal sealed class ExportMyAscentsQueryHandler(
    IAscentRepository ascentRepository,
    IPhotoUrlSigner photoUrlSigner) : IQueryHandler<ExportMyAscentsQuery, AscentExportResponse>
{
    public async Task<Result<AscentExportResponse>> Handle(
        ExportMyAscentsQuery query,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Ascent> ascents =
            await ascentRepository.GetByUserIdAsync(query.UserId, cancellationToken);

        return new AscentExportResponse(
            query.UserId,
            ascents.Count,
            [.. ascents
                .OrderByDescending(ascent => ascent.AscentDate)
                .Select(ascent => ascent.ToResponse(photoUrlSigner))]);
    }
}
