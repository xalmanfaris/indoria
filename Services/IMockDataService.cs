using AuraLiving.Models;

namespace AuraLiving.Services;

public interface IMockDataService
{
    List<CategoryViewModel> GetCategories();
    CategoryViewModel? GetCategoryBySlug(string slug);
    List<ProductViewModel> GetAllProducts();
    ProductViewModel? GetProductBySlug(string slug);
    List<ProductViewModel> GetFeaturedProducts();
    List<ProductViewModel> GetBestSellers();
    List<ProductViewModel> GetFlashDeals();
    List<ProductViewModel> GetRelatedProducts(string categorySlug, string excludeProductId);
    CatalogFilterViewModel FilterProducts(string? categorySlug, string? query, List<string>? brands, decimal? minPrice, decimal? maxPrice, double? minRating, int? energyRating, bool inStockOnly, string sortBy, int page, int pageSize);
    List<string> GetAllBrands();
    CartViewModel GetSampleCart();
    List<AddressViewModel> GetSampleAddresses();
    List<OrderViewModel> GetSampleOrders();
    OrderViewModel? GetOrderById(string id);
    ProfileViewModel GetSampleProfile();
    List<ReturnRequestViewModel> GetSampleReturns();
    List<ReviewItemViewModel> GetSampleReviews();
    NotificationSettingsViewModel GetNotificationSettings();
}
