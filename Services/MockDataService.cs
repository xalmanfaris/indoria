using AuraLiving.Models;

namespace AuraLiving.Services;

public class MockDataService : IMockDataService
{
    private readonly List<CategoryViewModel> _categories;
    private readonly List<ProductViewModel> _products;
    private readonly List<AddressViewModel> _addresses;
    private readonly List<OrderViewModel> _orders;
    private readonly List<ReturnRequestViewModel> _returns;
    private readonly List<ReviewItemViewModel> _accountReviews;
    private readonly List<CouponViewModel> _coupons;

    public MockDataService()
    {
        _categories = InitializeCategories();
        _products = new List<ProductViewModel>();
        _addresses = new List<AddressViewModel>();
        _orders = new List<OrderViewModel>();
        _returns = new List<ReturnRequestViewModel>();
        _accountReviews = new List<ReviewItemViewModel>();
        _coupons = new List<CouponViewModel>();
    }

    public List<CategoryViewModel> GetCategories() => _categories;

    public CategoryViewModel? GetCategoryBySlug(string slug) =>
        _categories.FirstOrDefault(c => c.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public List<ProductViewModel> GetAllProducts() => _products;

    public ProductViewModel? GetProductBySlug(string slug) =>
        _products.FirstOrDefault(p => p.Slug.Equals(slug, StringComparison.OrdinalIgnoreCase));

    public List<ProductViewModel> GetFeaturedProducts()
    {
        return _products.Where(p => p.Badges.Contains("Featured") || p.Badges.Contains("New Launch") || p.Badges.Contains("Best Seller")).Take(8).ToList();
    }

    public List<ProductViewModel> GetBestSellers()
    {
        return _products.Where(p => p.Badges.Contains("Best Seller")).Take(8).ToList();
    }

    public List<ProductViewModel> GetFlashDeals() =>
        _products.Where(p => p.DiscountPercent >= 20).Take(4).ToList();

    public List<ProductViewModel> GetRelatedProducts(string categorySlug, string excludeProductId) =>
        _products.Where(p => p.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase) && p.Id != excludeProductId)
                 .Take(4)
                 .ToList();

    public List<string> GetAllBrands() =>
        _products.Select(p => p.Brand).Distinct().OrderBy(b => b).ToList();

    public CatalogFilterViewModel FilterProducts(
        string? categorySlug,
        string? query,
        List<string>? brands,
        decimal? minPrice,
        decimal? maxPrice,
        double? minRating,
        int? energyRating,
        bool inStockOnly,
        string sortBy,
        int page,
        int pageSize)
    {
        var items = _products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categorySlug))
        {
            items = items.Where(p => p.CategorySlug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            var q = query.ToLower();
            items = items.Where(p => p.Name.ToLower().Contains(q) ||
                                     p.Brand.ToLower().Contains(q) ||
                                     p.Category.ToLower().Contains(q) ||
                                     p.ShortDescription.ToLower().Contains(q));
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

        return new CatalogFilterViewModel
        {
            CategorySlug = categorySlug,
            CategoryName = _categories.FirstOrDefault(c => c.Slug.Equals(categorySlug, StringComparison.OrdinalIgnoreCase))?.Name,
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
            AvailableBrands = GetAllBrands(),
            Products = paginated
        };
    }

    public CartViewModel GetSampleCart()
    {
        return new CartViewModel
        {
            Items = new List<CartItemViewModel>()
        };
    }

    public List<AddressViewModel> GetSampleAddresses() => new List<AddressViewModel>();

    public List<OrderViewModel> GetSampleOrders() => _orders;

    public OrderViewModel? GetOrderById(string id) =>
        _orders.FirstOrDefault(o => o.Id == id);

    public ProfileViewModel GetSampleProfile() =>
        new ProfileViewModel();

    public List<ReturnRequestViewModel> GetSampleReturns() => _returns;

    public List<ReviewItemViewModel> GetSampleReviews() => _accountReviews;

    public NotificationSettingsViewModel GetNotificationSettings() => new NotificationSettingsViewModel();

    public List<CouponViewModel> GetCoupons() => _coupons;

    public AdminDashboardViewModel GetAdminDashboard()
    {
        return new AdminDashboardViewModel
        {
            TotalRevenue = _orders.Sum(o => o.GrandTotal),
            TotalOrders = _orders.Count,
            TotalProducts = _products.Count,
            TotalCustomers = 1,
            RecentOrders = _orders,
            LowStockProducts = _products.Where(p => p.StockCount <= 10).ToList(),
            TopSellingProducts = _products.Take(5).ToList(),
            AllProducts = _products,
            AllCategories = _categories,
            AllCoupons = _coupons
        };
    }

    public void AddProduct(ProductViewModel product)
    {
        if (string.IsNullOrEmpty(product.Id))
        {
            product.Id = "IND-" + (_products.Count + 101);
        }
        if (string.IsNullOrEmpty(product.Slug))
        {
            product.Slug = product.Name.ToLower().Replace(" ", "-").Replace("/", "");
        }
        _products.RemoveAll(p => p.Id == product.Id);
        _products.Insert(0, product);

        // Update category count
        var cat = _categories.FirstOrDefault(c => c.Name.Equals(product.Category, StringComparison.OrdinalIgnoreCase));
        if (cat != null)
        {
            cat.ProductCount = _products.Count(p => p.Category.Equals(product.Category, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void UpdateProduct(ProductViewModel product)
    {
        var existing = _products.FirstOrDefault(p => p.Id == product.Id || p.Slug == product.Slug);
        if (existing != null)
        {
            existing.Name = product.Name;
            existing.Price = product.Price;
            existing.OriginalPrice = product.OriginalPrice;
            existing.StockCount = product.StockCount;
            existing.InStock = product.StockCount > 0;
            existing.Category = product.Category;
            existing.Brand = product.Brand;
            if (!string.IsNullOrEmpty(product.MainImage)) existing.MainImage = product.MainImage;
        }
        else
        {
            AddProduct(product);
        }
    }

    public void DeleteProduct(string id)
    {
        var existing = _products.FirstOrDefault(p => p.Id == id);
        if (existing != null)
        {
            _products.Remove(existing);
        }
    }

    private List<CategoryViewModel> InitializeCategories()
    {
        return new List<CategoryViewModel>
        {
            new CategoryViewModel { Id = "CAT-1", Name = "Smart OLED & QLED TVs", Slug = "smart-tvs", Description = "4K Cinema OLEDs, QLED Flagships & Ultra-HD Entertainment Displays", Icon = "bi-tv-fill", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "4K OLED", "QLED Cinema", "Mini-LED", "8K Ultra-HD" }, Featured = true },
            new CategoryViewModel { Id = "CAT-2", Name = "Refrigerators", Slug = "refrigerators", Description = "Multi-Door, French Door, Convertible & Side-by-Side Cooling Suites", Icon = "bi-snow2", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "French Door", "Side-by-Side", "Triple Door", "Bottom Mount" }, Featured = true },
            new CategoryViewModel { Id = "CAT-3", Name = "Washing Machines", Slug = "washing-machines", Description = "Front Load Steam, AI Washer-Dryers & Smart Inverter Washers", Icon = "bi-tsunami", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "Front Load", "Washer Dryer Combo", "Top Load Inverter", "Steam Care" }, Featured = true },
            new CategoryViewModel { Id = "CAT-4", Name = "Air Conditioners", Slug = "air-conditioners", Description = "Heavy-Duty Tropical Inverter Split & Multi-Zone Climate Systems", Icon = "bi-wind", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "5 Star Inverter", "Heavy-Duty Split", "Multi-Zone", "Hot & Cold" } },
            new CategoryViewModel { Id = "CAT-5", Name = "Kitchen Appliances", Slug = "kitchen-appliances", Description = "Filterless Auto-Clean Chimneys, Induction Hobs & Cooking Suites", Icon = "bi-fire", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "Auto-Clean Chimneys", "Built-in Hobs", "Air Fryers", "Juicer Mixers" } },
            new CategoryViewModel { Id = "CAT-6", Name = "Dishwashers", Slug = "dishwashers", Description = "Built-in & Freestanding Intensive Hygiene Washers with Steam Sanitization", Icon = "bi-water", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "14 Place Settings", "Built-In Integrated", "Countertop Compact", "Hygiene Wash" } },
            new CategoryViewModel { Id = "CAT-7", Name = "Microwaves & Ovens", Slug = "microwaves", Description = "Convection Ovens, Smart Grills & Built-in Micro-Combi Suites", Icon = "bi-box-seam-fill", Image = "/images/hero-appliances.jpg", ProductCount = 0, SubCategories = new List<string>{ "Convection Ovens", "Built-in Microwave", "Grill & Solo", "Auto-Cook Suites" } }
        };
    }
}
