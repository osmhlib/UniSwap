using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Reservations;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/reservations")]
public sealed class ReservationsController(ReservationWorkflow workflow, IReservationRepository reservations) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<ReservationResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ReservationResponse>> Create(CreateReservationRequest request, CancellationToken ct) =>
        StatusCode(StatusCodes.Status201Created, await workflow.CreateAsync(request.ListingId, User.UserId(), ct));

    [HttpGet("sent")]
    public Task<IReadOnlyList<ReservationListItem>> Sent([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        reservations.ListAsync(User.UserId(), false, request.Page, request.PageSize, ct);

    [HttpGet("received")]
    public Task<IReadOnlyList<ReservationListItem>> Received([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        reservations.ListAsync(User.UserId(), true, request.Page, request.PageSize, ct);

    [HttpPost("{id:guid}/accept")]
    public Task<ReservationResponse> Accept(Guid id, CancellationToken ct) => workflow.AcceptAsync(id, User.UserId(), ct);

    [HttpPost("{id:guid}/reject")]
    public Task<ReservationResponse> Reject(Guid id, CancellationToken ct) => workflow.RejectAsync(id, User.UserId(), ct);

    [HttpPost("{id:guid}/cancel")]
    public Task<ReservationResponse> Cancel(Guid id, CancellationToken ct) => workflow.CancelAsync(id, User.UserId(), ct);
}

public sealed record CreateReservationRequest(Guid ListingId);

public sealed class CreateReservationValidator : AbstractValidator<CreateReservationRequest>
{
    public CreateReservationValidator() => RuleFor(x => x.ListingId).NotEmpty();
}
