using Microsoft.AspNetCore.Mvc;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class WishlistController : Controller
{
    private readonly IMockDataService _dataService;

    public WishlistController(IMockDataService dataService)
    {
        _dataService = dataService;
    }

    public IActionResult Index()
    {
        var allProducts = _dataService.GetAllProducts();
        // Return 4 sample wishlisted items
        var wishlistItems = allProducts.Take(4).ToList();
        return View(wishlistItems);
    }
}
