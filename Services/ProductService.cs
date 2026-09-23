using AuraLiving.Models;
using AuraLiving.Services.Repositories;

namespace AuraLiving.Services;

public interface IProductService
{
    Task<List<ProductViewModel>> GetAllProductsAsync();
    Task<ProductViewModel?> GetProductByIdAsync(string id);
    Task<ProductViewModel?> GetProductBySlugAsync(string slug);
    Task<CatalogFilterViewModel> FilterProductsAsync(string? categorySlug, string? query, List<string>? brands, decimal? minPrice, decimal? maxPrice, double? minRating, int? energyRating, bool inStockOnly, string sortBy, int page, int pageSize);
    Task<bool> AddProductAsync(ProductViewModel product);
    Task<bool> UpdateProductAsync(ProductViewModel product);
    Task<bool> DeleteProductAsync(string id);
    Task<List<ReviewViewModel>> GetProductReviewsAsync(string productId);
    Task<List<ReviewViewModel>> GetRecentReviewsAsync(int count = 6);
    Task<bool> AddProductReviewAsync(AddReviewRequestModel model, string? userId = null);
}

public class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly IMockDataService _mockDataService;
    private readonly IReviewRepository _reviewRepository;

    public ProductService(IProductRepository productRepository, IMockDataService mockDataService, IReviewRepository reviewRepository)
    {
        _productRepository = productRepository;
        _mockDataService = mockDataService;
        _reviewRepository = reviewRepository;
    }

    public async Task<List<ProductViewModel>> GetAllProductsAsync()
    {
        return await _productRepository.GetAllProductsAsync();
    }

    public async Task<ProductViewModel?> GetProductByIdAsync(string id)
    {
        return await _productRepository.GetByIdAsync(id);
    }

    public async Task<ProductViewModel?> GetProductBySlugAsync(string slug)
    {
        return await _productRepository.GetBySlugAsync(slug);
    }

    public async Task<CatalogFilterViewModel> FilterProductsAsync(string? categorySlug, string? query, List<string>? brands, decimal? minPrice, decimal? maxPrice, double? minRating, int? energyRating, bool inStockOnly, string sortBy, int page, int pageSize)
    {
        var allProducts = await _productRepository.GetAllProductsAsync();
        var items = allProducts.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug) && categorySlug != "all")
        {
            var targetSlug = categorySlug.ToLower().Trim();
            items = items.Where(p => (p.CategorySlug != null && p.CategorySlug.Equals(targetSlug, StringComparison.OrdinalIgnoreCase)) ||
                                     (p.Category != null && p.Category.ToLower().Replace(" ", "-").Replace("/", "") == targetSlug) ||
                                     (p.Category != null && p.Category.Equals(categorySlug, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLower();
            items = items.Where(p => p.Name.ToLower().Contains(q) ||
                                     p.Brand.ToLower().Contains(q) ||
                                     p.Category.ToLower().Contains(q) ||
                                     (p.ShortDescription != null && p.ShortDescription.ToLower().Contains(q)));
        }

        if (brands != null && brands.Any())
        {
            items = items.Where(p => brands.Contains(p.Brand, StringComparer.OrdinalIgnoreCase));
        }

        if (minPrice.HasValue)
        {
            items = items.Where(p => p.Price >= minPrice.Value);
        }

        if (maxPrice.HasValue)
        {
            items = items.Where(p => p.Price <= maxPrice.Value);
        }

        if (minRating.HasValue)
        {
            items = items.Where(p => p.Rating >= minRating.Value);
        }

        if (energyRating.HasValue)
        {
            items = items.Where(p => p.EnergyRating >= energyRating.Value);
        }

        if (inStockOnly)
        {
            items = items.Where(p => p.InStock);
        }

        items = sortBy switch
        {
            "price-low" => items.OrderBy(p => p.Price),
            "price-high" => items.OrderByDescending(p => p.Price),
            "rating" => items.OrderByDescending(p => p.Rating),
            "newest" => items.OrderByDescending(p => p.Id),
            _ => items.OrderByDescending(p => p.Rating)
        };

        var filteredList = items.ToList();
        var paginated = filteredList.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        var categories = _mockDataService.GetCategories();

        return new CatalogFilterViewModel
        {
            CategorySlug = categorySlug,
            CategoryName = categories.FirstOrDefault(c => c.Slug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase))?.Name ?? categorySlug,
            SearchQuery = query,
            SelectedBrands = brands ?? new List<string>(),
            MinPrice = minPrice,
            MaxPrice = maxPrice,
            MinRating = minRating,
            EnergyRating = energyRating,
            InStockOnly = inStockOnly,
            SortBy = sortBy,
            Page = page,
            PageSize = pageSize,
            TotalCount = filteredList.Count,
            AvailableBrands = allProducts.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList(),
            Products = paginated
        };
    }

    public async Task<bool> AddProductAsync(ProductViewModel product)
    {
        if (string.IsNullOrEmpty(product.Id))
        {
            product.Id = "IND-" + Guid.NewGuid().ToString("N")[..6].ToUpper();
        }
        if (string.IsNullOrEmpty(product.Slug))
        {
            product.Slug = product.Name.ToLower().Replace(" ", "-").Replace("/", "");
        }
        
        // Map category slug reliably
        product.CategorySlug = MapCategoryToSlug(product.Category);
        product.InStock = product.StockCount > 0;
        ParseSpecificationsAndHighlights(product);

        return await _productRepository.AddProductAsync(product);
    }

    public async Task<bool> UpdateProductAsync(ProductViewModel product)
    {
        product.CategorySlug = MapCategoryToSlug(product.Category);
        product.InStock = product.StockCount > 0;
        ParseSpecificationsAndHighlights(product);
        return await _productRepository.UpdateProductAsync(product);
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        return await _productRepository.DeleteProductAsync(id);
    }

    private void ParseSpecificationsAndHighlights(ProductViewModel product)
    {
        if (!string.IsNullOrWhiteSpace(product.RawKeyFeatures))
        {
            product.KeyFeatures = product.RawKeyFeatures
                .Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(f => f.Trim())
                .Where(f => !string.IsNullOrWhiteSpace(f))
                .ToList();
        }

        if (!string.IsNullOrWhiteSpace(product.RawSpecifications))
        {
            var specsDict = new Dictionary<string, string>();
            var lines = product.RawSpecifications.Split(new[] { '\r', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ':', '=' }, 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var val = parts[1].Trim();
                    if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                    {
                        specsDict[key] = val;
                    }
                }
            }
            if (specsDict.Count > 0)
            {
                product.Specifications = specsDict;
            }
        }
    }

    private string MapCategoryToSlug(string category)
    {
        if (string.IsNullOrWhiteSpace(category)) return "appliances";
        var catLower = category.ToLower().Trim();
        if (catLower.Contains("tv") || catLower.Contains("oled") || catLower.Contains("smart")) return "smart-tvs";
        if (catLower.Contains("fridge") || catLower.Contains("refrigerator") || catLower.Contains("cooling")) return "refrigerators";
        if (catLower.Contains("wash") || catLower.Contains("laundry")) return "washing-machines";
        if (catLower.Contains("air") || catLower.Contains("ac") || catLower.Contains("climate")) return "air-conditioners";
        if (catLower.Contains("kitchen") || catLower.Contains("hood") || catLower.Contains("chimney")) return "kitchen-appliances";
        if (catLower.Contains("dish")) return "dishwashers";
        if (catLower.Contains("micro")) return "microwaves";
        return catLower.Replace(" ", "-").Replace("/", "");
    }

    public async Task<List<ReviewViewModel>> GetProductReviewsAsync(string productId)
    {
        return await _reviewRepository.GetReviewsByProductIdAsync(productId);
    }

    public async Task<List<ReviewViewModel>> GetRecentReviewsAsync(int count = 6)
    {
        return await _reviewRepository.GetRecentReviewsAsync(count);
    }

    public async Task<bool> AddProductReviewAsync(AddReviewRequestModel model, string? userId = null)
    {
        if (model == null || string.IsNullOrWhiteSpace(model.ProductId)) return false;

        var product = await _productRepository.GetByIdAsync(model.ProductId);
        if (product == null)
        {
            product = await _productRepository.GetBySlugAsync(model.ProductId);
        }

        var review = new ReviewViewModel
        {
            Id = "REV-" + Guid.NewGuid().ToString("N")[..8].ToUpper(),
            ProductId = product?.Id ?? model.ProductId,
            ProductSlug = product?.Slug ?? model.ProductId,
            ProductName = product?.Name ?? "Appliance Product",
            UserId = userId ?? "VIP-USER-001",
            Author = string.IsNullOrWhiteSpace(model.Author) ? "Community Reviewer" : model.Author.Trim(),
            City = string.IsNullOrWhiteSpace(model.City) ? "India" : model.City.Trim(),
            Rating = Math.Clamp(model.Rating, 1, 5),
            Title = string.IsNullOrWhiteSpace(model.Title) ? "Product Review" : model.Title.Trim(),
            Comment = model.Comment?.Trim() ?? string.Empty,
            ImageUrl = model.ImageUrl?.Trim(),
            VerifiedBuyer = true,
            HelpfulCount = 0,
            IsActive = true,
            CreatedAt = DateTime.Now,
            Date = DateTime.Now.ToString("MMM dd, yyyy")
        };

        return await _reviewRepository.AddReviewAsync(review);
    }
}

