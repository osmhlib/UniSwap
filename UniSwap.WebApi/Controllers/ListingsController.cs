using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Listings;

namespace UniSwap.WebApi.Controllers
{
    [ApiController]
    [Route("api/listings")]
    public class ListingsController : ControllerBase
    {
        private readonly CreateListing _createListing;
        private readonly PublishListing _publishListing;

        public ListingsController(CreateListing createListing, PublishListing publishListing)
        {
            _createListing = createListing;
            _publishListing = publishListing;
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateListingRequest request)
        {
            var id = await _createListing.ExecuteAsync(
                request.OwnerId,
                request.Title,
                request.Description,
                request.Category,
                request.Condition);

            return Ok(new { Id = id });
        }

        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            await _publishListing.ExecuteAsync(id);
            return NoContent();
        }
    }

    public record CreateListingRequest(
        Guid OwnerId,
        string Title,
        string Description,
        string Category,
        string Condition);
}
