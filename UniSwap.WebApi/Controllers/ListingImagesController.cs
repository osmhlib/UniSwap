using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniSwap.Application.Common;
using UniSwap.Application.Listings;
using UniSwap.WebApi.Auth;

namespace UniSwap.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/listings/{id:guid}/images")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ListingImagesController(ListingImages images) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public Task<IReadOnlyList<ListingImageResponse>> List(Guid id, CancellationToken ct) =>
        images.ListAsync(id, User.Identity?.IsAuthenticated == true ? User.UserId() : null, ct);

    [HttpGet("{imageId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(Guid id, Guid imageId, CancellationToken ct)
    {
        Guid? actor = User.Identity?.IsAuthenticated == true ? User.UserId() : null;
        var (content, contentType) = await images.OpenAsync(id, imageId, actor, ct);
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(content, contentType);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ListingImages.MaxBytes + 64 * 1024)]
    [RequestFormLimits(MultipartBodyLengthLimit = ListingImages.MaxBytes + 64 * 1024)]
    [ProducesResponseType<ListingImageResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<ListingImageResponse>> Upload(Guid id, IFormFile file, CancellationToken ct)
    {
        if (file.Length is <= 0 or > ListingImages.MaxBytes || Request.Form.Files.Count != 1)
            throw new InvalidImageUploadException();
        await using var input = file.OpenReadStream();
        var uploaded = await images.UploadAsync(id, User.UserId(), input, ct);
        return CreatedAtAction(nameof(Get), new { id, imageId = uploaded.Id }, uploaded);
    }

    [HttpDelete("{imageId:guid}")]
    public async Task<IActionResult> Delete(Guid id, Guid imageId, CancellationToken ct)
    {
        await images.DeleteAsync(id, imageId, User.UserId(), ct);
        return NoContent();
    }
}
