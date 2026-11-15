using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Dtos;
using UniSwap.Application.Moderation;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Application.Users;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize(Policy = AccountIdentity.AdminPolicy)]
[Route("api/moderation")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ModerationController(IModerationCatalog catalog, ModerateListing moderate) : ControllerBase
{
    [HttpGet("users")]
    public Task<IReadOnlyList<ProfileResponse>> Users([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        catalog.ListUsersAsync(request.Page, request.PageSize, ct);

    [HttpGet("listings")]
    public Task<ListingPage> Listings([FromQuery] ModerationQueueRequest request, CancellationToken ct) =>
        catalog.ListListingsAsync(request.Status, request.Page, request.PageSize, ct);

    [HttpPost("listings/{id:guid}/hide")]
    public Task<ListingDto> Hide(Guid id, ModerationReasonRequest request, CancellationToken ct) =>
        moderate.ExecuteAsync(id, ModerationDecision.Hide, request.Reason, ct);

    [HttpPost("listings/{id:guid}/reject")]
    public Task<ListingDto> Reject(Guid id, ModerationReasonRequest request, CancellationToken ct) =>
        moderate.ExecuteAsync(id, ModerationDecision.Reject, request.Reason, ct);

    [HttpPost("listings/{id:guid}/return-to-draft")]
    public Task<ListingDto> Return(Guid id, CancellationToken ct) =>
        moderate.ExecuteAsync(id, ModerationDecision.ReturnToDraft, null, ct);

    [HttpPost("listings/{id:guid}/remove")]
    public async Task<IActionResult> Remove(Guid id, ModerationReasonRequest request, CancellationToken ct)
    {
        await moderate.ExecuteAsync(id, ModerationDecision.Remove, request.Reason, ct);
        return NoContent();
    }
}
