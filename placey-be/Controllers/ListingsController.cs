using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using placey_be.Extensions;
using placey_be.Models.DTOs.Listings;
using placey_be.Models.DTOs.Reviews;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("listings")]
public class ListingsController(
    IListingService listingService,
    IReviewService reviewService) : ControllerBase
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

    [HttpGet("{id:guid}/availability")]
    public async Task<IActionResult> GetAvailability(Guid id)
    {
        var result = await listingService.GetAvailabilityAsync(id);
        return Ok(result);
    }

    [Authorize]
    [HttpPatch("{id:guid}/availability")]
    public async Task<IActionResult> SetAvailability(Guid id, SetAvailabilityRequest request)
    {
        var result = await listingService.SetAvailabilityAsync(id, User.GetUserId(), request);
        return Ok(result);
    }

    [HttpGet("{id:guid}/reviews")]
    public async Task<IActionResult> GetReviews(Guid id)
    {
        var result = await reviewService.GetByListingAsync(id);
        return Ok(result);
    }
}
