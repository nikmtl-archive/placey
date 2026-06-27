using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using placey_be.Extensions;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.DTOs.Reviews;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("listings")]
public class ListingsController(IListingService listingService, IReviewService reviewService)
    : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] ListingSearchParams p)
    {
        var (items, total) = await listingService.SearchAsync(p);
        return Ok(new { items, total });
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await listingService.GetByIdAsync(id);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateListingRequest request)
    {
        var result = await listingService.CreateAsync(User.GetUserId(), request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [Authorize]
    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateListingRequest request)
    {
        var result = await listingService.UpdateAsync(id, User.GetUserId(), request);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await listingService.DeleteAsync(id, User.GetUserId());
        return NoContent();
    }

    [HttpGet("{id:guid}/blockings")]
    public async Task<IActionResult> GetBlockings(Guid id)
    {
        var result = await listingService.GetBlockingsAsync(id);
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/blockings")]
    public async Task<IActionResult> AddBlocking(Guid id, AddBlockingRequest request)
    {
        var result = await listingService.AddBlockingAsync(id, User.GetUserId(), request);
        return CreatedAtAction(nameof(GetBlockings), new { id }, result);
    }

    [Authorize]
    [HttpDelete("{id:guid}/blockings/{blockingId:guid}")]
    public async Task<IActionResult> DeleteBlocking(Guid id, Guid blockingId)
    {
        await listingService.DeleteBlockingAsync(id, blockingId, User.GetUserId());
        return NoContent();
    }

    [Authorize]
    [HttpGet("{id:guid}/photos")]
    public async Task<IActionResult> GetPhotos(Guid id)
    {
        var result = await listingService.GetPhotosAsync(id, User.GetUserId());
        return Ok(result);
    }

    [Authorize]
    [HttpPost("{id:guid}/photos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto(Guid id, IFormFile file)
    {
        var result = await listingService.UploadPhotoAsync(id, User.GetUserId(), file);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}/photos/{photoId:guid}")]
    public async Task<IActionResult> DeletePhoto(Guid id, Guid photoId)
    {
        await listingService.DeletePhotoAsync(id, photoId, User.GetUserId());
        return NoContent();
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id)
    {
        var result = await reviewService.GetByListingAsync(id);
        return Ok(result);
    }
}
