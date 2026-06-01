using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using placey_be.Extensions;
using placey_be.Services.Interfaces;

namespace placey_be.Controllers;

[ApiController]
[Route("favorites")]
[Authorize]
public class FavoritesController(IFavoriteService favoriteService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetFavorites()
    {
        var result = await favoriteService.GetFavoritesAsync(User.GetUserId());
        return Ok(result);
    }

    [HttpPost("{listingId:guid}")]
    public async Task<IActionResult> Add(Guid listingId)
    {
        await favoriteService.AddAsync(User.GetUserId(), listingId);
        return NoContent();
    }

    [HttpDelete("{listingId:guid}")]
    public async Task<IActionResult> Remove(Guid listingId)
    {
        await favoriteService.RemoveAsync(User.GetUserId(), listingId);
        return NoContent();
    }
}
