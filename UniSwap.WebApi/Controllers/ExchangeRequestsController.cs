using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Exchanges;

namespace UniSwap.WebApi.Controllers
{
    [ApiController]
    [Route("api/exchange-requests")]
    public class ExchangeRequestsController : ControllerBase
    {
        private readonly SendExchangeRequest _sendExchangeRequest;

        public ExchangeRequestsController(
            SendExchangeRequest sendExchangeRequest)
        {
            _sendExchangeRequest = sendExchangeRequest;
        }

        [HttpPost]
        public async Task<IActionResult> Create(
            SendExchangeRequestDto request)
        {
            var id = await _sendExchangeRequest.ExecuteAsync(
                request.ListingId,
                request.RequesterId);

            return Ok(new { Id = id });
        }
    }

    public record SendExchangeRequestDto(
        Guid ListingId,
        Guid RequesterId);
}
