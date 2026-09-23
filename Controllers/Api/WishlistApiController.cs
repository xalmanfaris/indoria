using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/wishlist")]
public class WishlistApiController : ControllerBase
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IAuthService _authService;

    public WishlistApiController(IWishlistRepository wishlistRepository, IAuthService authService)
    {
        _wishlistRepository = wishlistRepository;
        _authService = authService;
    }

    private async Task<string?> GetCurrentUserIdAsync()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        return currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(new { isAuthenticated = false, count = 0, productIds = new List<string>() });
        }

        var productIds = await _wishlistRepository.GetWishlistProductIdsByUserIdAsync(userId);
        var count = await _wishlistRepository.GetWishlistCountAsync(userId);
        return Ok(new { isAuthenticated = true, count, productIds });
    }

    [HttpPost("toggle/{productId}")]
    public async Task<IActionResult> ToggleWishlist(string productId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Please sign in to save items to your wishlist.", redirectUrl = "/login" });
        }

        bool isWishlisted = await _wishlistRepository.ToggleWishlistItemAsync(userId, productId);
        int count = await _wishlistRepository.GetWishlistCountAsync(userId);

        return Ok(new { success = true, isWishlisted, count });
    }
}
