using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Exchanges;

namespace UniSwap.WebApi.Controllers
{
    [ApiController]
    [Route("api/exchange-requests")]
    public class ExchangeRequestsController : ControllerBase
    {
        private readonly SendExchangeRequest _sendExchangeRequest;
        private readonly AcceptExchangeRequest _acceptExchangeRequest;

        public ExchangeRequestsController(
            SendExchangeRequest sendExchangeRequest,
            AcceptExchangeRequest acceptExchangeRequest)
        {
            _sendExchangeRequest = sendExchangeRequest;
            _acceptExchangeRequest = acceptExchangeRequest;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            SendExchangeRequestDto request)
        {
            var id = await _sendExchangeRequest.ExecuteAsync(
                request.ListingId,
                request.OfferedListingId,
                request.RequesterId);

            return Ok(new { Id = id });
        }

        [HttpPost("{id:guid}/accept")]
        public async Task<IActionResult> Accept(Guid id, AcceptExchangeRequestDto request)
        {
            await _acceptExchangeRequest.ExecuteAsync(id, request.ActorId);
            return NoContent();
        }
    }

    public record SendExchangeRequestDto(
        Guid ListingId,
        Guid OfferedListingId,
        Guid RequesterId);

    public record AcceptExchangeRequestDto(Guid ActorId);
}
