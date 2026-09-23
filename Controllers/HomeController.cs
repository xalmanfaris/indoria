using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class HomeController : Controller
{
    private readonly IMockDataService _dataService;
    private readonly IProductService _productService;
    private readonly IInstagramService _instagramService;

    public HomeController(IMockDataService dataService, IProductService productService, IInstagramService instagramService)
    {
        _dataService = dataService;
        _productService = productService;
        _instagramService = instagramService;
    }

    public async Task<IActionResult> Index()
    {
        var dbProducts = await _productService.GetAllProductsAsync();
        var categories = _dataService.GetCategories();

        // Update category counts based on actual database products
        foreach (var cat in categories)
        {
            cat.ProductCount = dbProducts.Count(p => 
                (!string.IsNullOrEmpty(p.CategorySlug) && p.CategorySlug.Equals(cat.Slug, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Category) && p.Category.Equals(cat.Name, StringComparison.OrdinalIgnoreCase)));
        }

        var testimonials = await _productService.GetRecentReviewsAsync(6);
        var instaFeed = await _instagramService.GetFeedAsync();

        var model = new HomeViewModel
        {
            Categories = categories,
            FeaturedProducts = dbProducts,
            BestSellers = dbProducts.OrderByDescending(p => p.Rating).ToList(),
            FlashDeals = dbProducts.Where(p => p.DiscountPercent >= 10).ToList(),
            HeroProduct = dbProducts.FirstOrDefault() ?? new ProductViewModel(),
            Brands = dbProducts.Select(p => p.Brand).Where(b => !string.IsNullOrEmpty(b)).Distinct().OrderBy(b => b).ToList(),
            Testimonials = testimonials,
            InstagramFeed = instaFeed
        };

        return View(model);
    }

    public IActionResult About() => View();
    public IActionResult Contact() => View();
    public IActionResult Help() => View();
    public IActionResult Faq() => View();
    public IActionResult Blog() => View();
    public IActionResult Privacy() => View();
    public IActionResult Terms() => View();
    public IActionResult ShippingPolicy() => View();
    public IActionResult ReturnPolicy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
