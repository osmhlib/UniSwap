using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.UseCases.Listings;
using UniSwap.Application.Dtos;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers
{
    [ApiController]
    [Route("api/listings")]
    public class ListingsController : ControllerBase
    {
        private readonly CreateListing _createListing;
        private readonly PublishListing _publishListing;
        private readonly GetListing _getListing;

        public ListingsController(CreateListing createListing, PublishListing publishListing, GetListing getListing)
        {
            _createListing = createListing;
            _publishListing = publishListing;
            _getListing = getListing;
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

            return Ok(new CreatedResponse(id));
        }

        [HttpGet("{id:guid}")]
        public async Task<ActionResult<ListingDto>> Get(Guid id)
        {
            var listing = await _getListing.ExecuteAsync(id);
            return listing is null ? NotFound() : Ok(listing);
        }

        [HttpPost("{id:guid}/publish")]
        public async Task<IActionResult> Publish(Guid id)
        {
            await _publishListing.ExecuteAsync(id);
            return NoContent();
        }
    }

}
