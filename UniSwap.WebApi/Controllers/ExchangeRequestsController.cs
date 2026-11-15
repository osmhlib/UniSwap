using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Exchanges;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/exchanges")]
[Route("api/exchange-requests")]
public sealed class ExchangeRequestsController(SendExchangeRequest send, AcceptExchangeRequest accept,
    ManageExchange workflow, IExchangeCatalog catalog) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreatedResponse>> Create(SendExchangeRequestDto request)
    {
        var id = await send.ExecuteAsync(request.ListingId, request.OfferedListingId, User.UserId());
        return Ok(new CreatedResponse(id));
    }

    [HttpGet("sent")]
    public Task<IReadOnlyList<ExchangeListItem>> Sent([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        catalog.ListAsync(User.UserId(), false, request.Page, request.PageSize, ct);

    [HttpGet("received")]
    public Task<IReadOnlyList<ExchangeListItem>> Received([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        catalog.ListAsync(User.UserId(), true, request.Page, request.PageSize, ct);

    [HttpPost("{id:guid}/accept")]
    public async Task<IActionResult> Accept(Guid id)
    {
        await accept.ExecuteAsync(id, User.UserId());
        return NoContent();
    }

    [HttpPost("{id:guid}/reject")]
    public Task<ExchangeResponse> Reject(Guid id, CancellationToken ct) => workflow.RejectAsync(id, User.UserId(), ct);

    [HttpPost("{id:guid}/cancel")]
    public Task<ExchangeResponse> Cancel(Guid id, CancellationToken ct) => workflow.CancelAsync(id, User.UserId(), ct);

    [HttpPost("{id:guid}/complete")]
    public Task<ExchangeResponse> Complete(Guid id, CancellationToken ct) => workflow.CompleteAsync(id, User.UserId(), ct);
}
