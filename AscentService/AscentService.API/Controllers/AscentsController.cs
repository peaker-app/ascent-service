using AscentService.API.Requests;
using AscentService.Application.Abstractions;
using AscentService.Application.Ascents.AddAscentPhoto;
using AscentService.Application.Ascents.DeleteAscent;
using AscentService.Application.Ascents.ExportMyData;
using AscentService.Application.Ascents.GetAscentById;
using AscentService.Application.Ascents.ListMyAscents;
using AscentService.Application.Ascents.RemoveAscentPhoto;
using Common.API.Responses;
using Common.API.Results;
using Common.Application.Abstractions;
using Common.Application.Pagination;
using Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AscentService.API.Controllers;

[ApiController]
[Route("api/ascents")]
public sealed class AscentsController(ISender sender, IUserContext userContext) : ControllerBase
{
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Register(
        RegisterAscentRequest request,
        CancellationToken cancellationToken)
    {
        Result<Guid> result = await sender.Send(request.ToCommand(userContext.UserId), cancellationToken);

        return result.ToActionResult(id => CreatedAtAction(nameof(GetById), new { id }, id));
    }

    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedResponse<AscentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ListMine(
        [FromQuery] ListAscentsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedResult<AscentSummaryResponse>> result =
            await sender.Send(request.ToQuery(userContext.UserId), cancellationToken);

        return result.ToActionResult(paged => Ok(paged.ToPagedResponse()));
    }

    [HttpGet("me/export")]
    [Authorize]
    [ProducesResponseType(typeof(AscentExportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ExportMine(CancellationToken cancellationToken)
    {
        Result<AscentExportResponse> result = await sender.Send(
            new ExportMyAscentsQuery(userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AscentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        Result<AscentResponse> result = await sender.Send(
            new GetAscentByIdQuery(id, CurrentUserOrNull), cancellationToken);

        return result.ToActionResult();
    }

    [HttpGet("by-user/{userId:guid}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponse<AscentSummaryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ListByUser(
        Guid userId,
        [FromQuery] ListAscentsRequest request,
        CancellationToken cancellationToken)
    {
        Result<PagedResult<AscentSummaryResponse>> result =
            await sender.Send(request.ToQuery(userId, CurrentUserOrNull), cancellationToken);

        return result.ToActionResult(paged => Ok(paged.ToPagedResponse()));
    }

    [HttpPut("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        UpdateAscentRequest request,
        CancellationToken cancellationToken)
    {
        Result result = await sender.Send(request.ToCommand(id, userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(new DeleteAscentCommand(id, userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/photos")]
    [Authorize]
#pragma warning disable S5693 // Motivo: RF-FOT-01 fija 10 MB por foto, por encima del umbral por
    // defecto de la regla; el margen extra de 64 KiB cubre la sobrecarga del multipart.
    [RequestSizeLimit(AddAscentPhotoCommand.MaxRequestSizeInBytes)]
#pragma warning restore S5693
    [ProducesResponseType(typeof(AscentPhotoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> AddPhoto(Guid id, IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest();
        }

        PhotoFile photo = await ReadPhotoAsync(file, cancellationToken);
        Result<AscentPhotoResponse> result = await sender.Send(
            new AddAscentPhotoCommand(id, userContext.UserId, photo), cancellationToken);

        return result.ToActionResult(added => CreatedAtAction(nameof(GetById), new { id }, added));
    }

    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePhoto(Guid id, Guid photoId, CancellationToken cancellationToken)
    {
        Result result = await sender.Send(
            new RemoveAscentPhotoCommand(id, photoId, userContext.UserId), cancellationToken);

        return result.ToActionResult();
    }

    private Guid? CurrentUserOrNull => userContext.IsAuthenticated ? userContext.UserId : null;

    private static async Task<PhotoFile> ReadPhotoAsync(IFormFile file, CancellationToken cancellationToken)
    {
        using MemoryStream stream = new();
        await file.CopyToAsync(stream, cancellationToken);

        return new PhotoFile(stream.ToArray(), file.ContentType, file.FileName);
    }
}
