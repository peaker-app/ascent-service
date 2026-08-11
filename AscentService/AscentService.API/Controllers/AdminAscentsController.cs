using AscentService.Application.Ascents.ReplayUserAscents;
using Common.API.Results;
using Common.API.Security;
using Common.Domain.Results;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AscentService.API.Controllers;

[ApiController]
[Route("api/admin/ascents")]
[Authorize(Policy = AuthorizationExtensions.AdminPolicyName)]
public sealed class AdminAscentsController(ISender sender) : ControllerBase
{
    [HttpPost("replay/{userId:guid}")]
    [ProducesResponseType(typeof(AscentReplayResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Replay(Guid userId, CancellationToken cancellationToken)
    {
        Result<AscentReplayResponse> result =
            await sender.Send(new ReplayUserAscentsCommand(userId), cancellationToken);

        return result.ToActionResult();
    }
}
