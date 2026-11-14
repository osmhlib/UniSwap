using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Listings;

namespace UniSwap.WebApi.Controllers
{
    [ApiController]
    [Route("api/listings")]
    public class ListingsController : ControllerBase
    {
        private readonly CreateListing _createListing;

        public ListingsController(CreateListing createListing)
        {
            _createListing = createListing;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateListingRequest request)
        {
            var id = await _createListing.ExecuteAsync(
                request.OwnerId,
                request.Title,
                request.Description);

            return Ok(new { Id = id });
        }
    }

    public record CreateListingRequest(
        Guid OwnerId,
        string Title,
        string Description);
}
