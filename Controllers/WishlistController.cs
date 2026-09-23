using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;

namespace AuraLiving.Controllers;

public class WishlistController : Controller
{
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IAuthService _authService;

    public WishlistController(IWishlistRepository wishlistRepository, IAuthService authService)
    {
        _wishlistRepository = wishlistRepository;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account", new { returnUrl = "/wishlist" });
        }

        var wishlistItems = await _wishlistRepository.GetWishlistProductsByUserIdAsync(userId);
        return View(wishlistItems);
    }
}

