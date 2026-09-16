using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class CatalogController : Controller
{
    private readonly IMockDataService _dataService;

    public CatalogController(IMockDataService dataService)
    {
        _dataService = dataService;
    }

    public IActionResult Categories()
    {
        var categories = _dataService.GetCategories();
        return View(categories);
    }

    public IActionResult Category(
        string slug,
        [FromQuery] List<string>? brands,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minRating,
        [FromQuery] int? energyRating,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] string sortBy = "popularity",
        [FromQuery] int page = 1)
    {
        var category = _dataService.GetCategoryBySlug(slug);
        if (category == null)
        {
            return RedirectToAction("Categories");
        }

        var filterModel = _dataService.FilterProducts(
            slug, null, brands, minPrice, maxPrice, minRating, energyRating, inStockOnly, sortBy, page, 9);

        ViewBag.Category = category;
        return View(filterModel);
    }

    public IActionResult Products(
        [FromQuery] List<string>? brands,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minRating,
        [FromQuery] int? energyRating,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] string sortBy = "popularity",
        [FromQuery] int page = 1)
    {
        var filterModel = _dataService.FilterProducts(
            null, null, brands, minPrice, maxPrice, minRating, energyRating, inStockOnly, sortBy, page, 12);

        return View(filterModel);
    }

    public IActionResult ProductDetail(string slug)
    {
        var product = _dataService.GetProductBySlug(slug);
        if (product == null)
        {
            return RedirectToAction("Products");
        }

        var related = _dataService.GetRelatedProducts(product.CategorySlug, product.Id);
        ViewBag.RelatedProducts = related;

        return View(product);
    }

    public IActionResult Search(
        [FromQuery] string? q,
        [FromQuery] List<string>? brands,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] double? minRating,
        [FromQuery] int? energyRating,
        [FromQuery] bool inStockOnly = false,
        [FromQuery] string sortBy = "popularity",
        [FromQuery] int page = 1)
    {
        var filterModel = _dataService.FilterProducts(
            null, q, brands, minPrice, maxPrice, minRating, energyRating, inStockOnly, sortBy, page, 9);

        return View(filterModel);
    }
}
