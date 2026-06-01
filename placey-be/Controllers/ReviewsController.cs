using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using placey_be.Extensions;
using placey_be.Models.DTOs.Reviews;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("reviews")]
public class ReviewsController(IReviewService reviewService) : ControllerBase
{
    [Authorize]
    [HttpPost]
    public async Task<IActionResult> Create(CreateReviewRequest request)
    {
        var result = await reviewService.CreateAsync(User.GetUserId(), request);
        return Ok(result);
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await reviewService.DeleteAsync(id, User.GetUserId());
        return NoContent();
    }
}
