using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Dtos;
using UniSwap.Application.UseCases.Listings;
using UniSwap.WebApi.Auth;
using UniSwap.WebApi.Dtos;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/listings")]
public sealed class ListingsController(CreateListing create, PublishListing publish,
    GetListing get, ListingManagement management, IListingCatalog catalog) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<CreatedResponse>> Create(CreateListingRequest request)
    {
        var id = await create.ExecuteAsync(User.UserId(), request.Title, request.Description,
            request.Category, request.Condition);
        return Ok(new CreatedResponse(id));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ListingDto>> Get(Guid id)
    {
        var listing = await get.ExecuteAsync(id);
        return listing is null ? NotFound() : Ok(listing);
    }

    [HttpGet]
    [AllowAnonymous]
    public Task<ListingPage> Browse([FromQuery] BrowseListingsRequest request, CancellationToken ct) =>
        catalog.BrowseAsync(new ListingQuery(request.Search, request.Category, request.Condition, request.Available,
            request.OldestFirst, request.Page, request.PageSize), ct);

    [HttpGet("mine")]
    public Task<ListingPage> Mine([FromQuery] ListingPagingRequest request, CancellationToken ct) =>
        catalog.BrowseOwnedAsync(User.UserId(), request.Page, request.PageSize, ct);

    [HttpGet("mine/{id:guid}")]
    public Task<ListingDto> MineById(Guid id) => management.GetOwnedAsync(id, User.UserId());

    [HttpPut("{id:guid}")]
    public Task<ListingDto> Update(Guid id, UpdateListingRequest request, CancellationToken ct) =>
        management.UpdateAsync(id, User.UserId(), request.Title, request.Description, request.Category, request.Condition, ct);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await management.DeleteAsync(id, User.UserId(), ct);
        return NoContent();
    }

    [HttpPost("{id:guid}/publish")]
    public async Task<IActionResult> Publish(Guid id)
    {
        await publish.ExecuteAsync(id, User.UserId());
        return NoContent();
    }
}
