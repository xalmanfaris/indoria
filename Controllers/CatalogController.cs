using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;

namespace AuraLiving.Controllers;

public class CatalogController : Controller
{
    private readonly IProductService _productService;
    private readonly IMockDataService _dataService;
    private readonly IWebHostEnvironment _env;

    public CatalogController(IProductService productService, IMockDataService dataService, IWebHostEnvironment env)
    {
        _productService = productService;
        _dataService = dataService;
        _env = env;
    }

    public async Task<IActionResult> Categories()
    {
        var categories = _dataService.GetCategories();
        var allProducts = await _productService.GetAllProductsAsync();

        foreach (var cat in categories)
        {
            cat.ProductCount = allProducts.Count(p =>
                (!string.IsNullOrEmpty(p.CategorySlug) && p.CategorySlug.Equals(cat.Slug, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Category) && p.Category.Equals(cat.Name, StringComparison.OrdinalIgnoreCase)));
        }

        return View(categories);
    }

    public async Task<IActionResult> Category(string slug)
    {
        var category = _dataService.GetCategoryBySlug(slug);
        if (category == null)
        {
            return RedirectToAction("Categories");
        }

        var filterModel = await _productService.FilterProductsAsync(
            slug, null, null, null, null, null, null, false, "popularity", 1, 12);

        ViewBag.Category = category;
        return View(filterModel);
    }

    public async Task<IActionResult> Products(
        [FromQuery] string? category,
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
        var filterModel = await _productService.FilterProductsAsync(
            category, q, brands, minPrice, maxPrice, minRating, energyRating, inStockOnly, sortBy, page, 12);

        return View(filterModel);
    }

    public async Task<IActionResult> ProductDetail(string slug)
    {
        var product = await _productService.GetProductBySlugAsync(slug);
        if (product == null)
        {
            return RedirectToAction("Products");
        }

        var allProducts = await _productService.GetAllProductsAsync();
        var related = allProducts
            .Where(p => p.Id != product.Id && (
                (!string.IsNullOrEmpty(p.CategorySlug) && p.CategorySlug.Equals(product.CategorySlug, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrEmpty(p.Category) && p.Category.Equals(product.Category, StringComparison.OrdinalIgnoreCase))))
            .Take(4)
            .ToList();

        ViewBag.RelatedProducts = related;

        return View(product);
    }

    public async Task<IActionResult> Search(
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
        var filterModel = await _productService.FilterProductsAsync(
            null, q, brands, minPrice, maxPrice, minRating, energyRating, inStockOnly, sortBy, page, 9);

        return View(filterModel);
    }

    [HttpGet("api/search/live")]
    public async Task<IActionResult> LiveSearch([FromQuery] string? q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return Json(new List<object>());
        }

        var results = await _productService.FilterProductsAsync(
            null, q, null, null, null, null, null, false, "popularity", 1, 8);

        var json = results.Products.Select(p => new
        {
            id = p.Id,
            name = p.Name,
            slug = p.Slug,
            brand = p.Brand,
            category = p.Category,
            price = "₹" + p.Price.ToString("N0"),
            image = p.MainImage
        });

        return Json(json);
    }

    [HttpPost("/catalog/reviews/upload-image")]
    public async Task<IActionResult> UploadReviewImage(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { success = false, message = "No image file provided." });
        }

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext) || !allowedExtensions.Contains(ext))
        {
            return BadRequest(new { success = false, message = "Invalid image file type. Allowed formats: JPG, PNG, WEBP, GIF." });
        }

        if (file.Length > 5 * 1024 * 1024)
        {
            return BadRequest(new { success = false, message = "Image size cannot exceed 5MB." });
        }

        try
        {
            string webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string uploadsFolder = Path.Combine(webRoot, "uploads", "reviews");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            string uniqueFileName = $"review_{Guid.NewGuid():N}{ext}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            string relativeUrl = $"/uploads/reviews/{uniqueFileName}";
            return Ok(new { success = true, imageUrl = relativeUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = "An error occurred while saving the image: " + ex.Message });
        }
    }

    [HttpPost("/catalog/reviews/add")]
    public async Task<IActionResult> AddReview([FromBody] AddReviewRequestModel model)
    {
        if (model == null) return BadRequest(new { success = false, message = "Invalid review data" });

        if (string.IsNullOrWhiteSpace(model.ProductId))
        {
            return BadRequest(new { success = false, message = "Product ID is required." });
        }

        if (string.IsNullOrWhiteSpace(model.Comment))
        {
            return BadRequest(new { success = false, message = "Review comment is required." });
        }

        string? userId = User.Identity?.IsAuthenticated == true ? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value : null;

        bool success = await _productService.AddProductReviewAsync(model, userId);
        if (!success)
        {
            return BadRequest(new { success = false, message = "Could not submit review." });
        }

        var product = await _productService.GetProductByIdAsync(model.ProductId);
        if (product == null)
        {
            product = await _productService.GetProductBySlugAsync(model.ProductId);
        }

        string targetId = product?.Id ?? model.ProductId;
        var reviews = await _productService.GetProductReviewsAsync(targetId);

        return Ok(new
        {
            success = true,
            message = "Review submitted successfully!",
            rating = product?.Rating ?? (reviews.Any() ? Math.Round(reviews.Average(r => r.Rating), 1) : 5.0),
            reviewCount = product?.ReviewCount ?? reviews.Count,
            reviews = reviews
        });
    }
}
