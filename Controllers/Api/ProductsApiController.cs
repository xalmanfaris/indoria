using AuraLiving.Models;
using AuraLiving.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/products")]
public class ProductsApiController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IWebHostEnvironment _env;

    public ProductsApiController(IProductService productService, IWebHostEnvironment env)
    {
        _productService = productService;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var products = await _productService.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var product = await _productService.GetProductByIdAsync(id);
        if (product == null) return NotFound(new { message = "Product not found" });
        return Ok(product);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProductViewModel model)
    {
        bool success = await _productService.AddProductAsync(model);
        if (!success) return BadRequest(new { message = "Failed to add product" });
        return CreatedAtAction(nameof(GetById), new { id = model.Id }, model);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ProductViewModel model)
    {
        model.Id = id;
        bool success = await _productService.UpdateProductAsync(model);
        if (!success) return BadRequest(new { message = "Failed to update product" });
        return Ok(new { success = true, message = "Product updated successfully", product = model });
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        bool success = await _productService.DeleteProductAsync(id);
        if (!success) return BadRequest(new { message = "Failed to delete product" });
        return Ok(new { success = true, message = "Product deleted successfully" });
    }

    [HttpGet("{id}/reviews")]
    public async Task<IActionResult> GetReviews(string id)
    {
        var reviews = await _productService.GetProductReviewsAsync(id);
        return Ok(reviews);
    }

    [HttpPost("reviews/upload-image")]
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

    [HttpPost("{id}/reviews")]
    public async Task<IActionResult> AddReview(string? id, [FromBody] AddReviewRequestModel model)
    {
        if (model == null) return BadRequest(new { success = false, message = "Invalid review data" });

        if (!string.IsNullOrEmpty(id))
        {
            model.ProductId = id;
        }

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

