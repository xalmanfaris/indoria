namespace AuraLiving.Models;

public class ProductViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CategorySlug { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public int DiscountPercent => OriginalPrice > 0 ? (int)Math.Round((1 - (Price / OriginalPrice)) * 100) : 0;
    public decimal EmiPerMonth { get; set; }
    public double Rating { get; set; } = 4.5;
    public int ReviewCount { get; set; } = 0;
    public bool InStock { get; set; } = true;
    public int StockCount { get; set; } = 15;
    public int EnergyRating { get; set; } = 5; // 3, 4, 5 stars
    public List<string> Badges { get; set; } = new();
    public string MainImage { get; set; } = string.Empty;
    public List<string> GalleryImages { get; set; } = new();
    public string ShortDescription { get; set; } = string.Empty;
    public string FullDescription { get; set; } = string.Empty;
    public List<string> KeyFeatures { get; set; } = new();
    public Dictionary<string, string> Specifications { get; set; } = new();
    public string DeliveryEstimate { get; set; } = "Tomorrow, 2:00 PM - 6:00 PM";
    public int WarrantyYears { get; set; } = 2;
    public List<string> ColorVariants { get; set; } = new();
    public List<string> CapacityVariants { get; set; } = new();
    public List<ReviewViewModel> Reviews { get; set; } = new();
}

public class ReviewViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string Date { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public bool VerifiedBuyer { get; set; } = true;
    public int HelpfulCount { get; set; } = 0;
}

public class CategoryViewModel
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public int ProductCount { get; set; } = 0;
    public List<string> SubCategories { get; set; } = new();
    public bool Featured { get; set; } = false;
}

public class CartItemViewModel
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductSlug { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Image { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OriginalPrice { get; set; }
    public string SelectedCapacity { get; set; } = string.Empty;
    public string SelectedColor { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal Total => Price * Quantity;
}

public class CartViewModel
{
    public List<CartItemViewModel> Items { get; set; } = new();
    public decimal SubTotal => Items.Sum(x => x.Total);
    public decimal Discount { get; set; } = 0;
    public string CouponCode { get; set; } = string.Empty;
    public decimal DeliveryCharge => SubTotal > 1000 ? 0 : 499;
    public decimal InstallationCharge { get; set; } = 0; // Free installation
    public decimal GrandTotal => Math.Max(0, SubTotal - Discount + DeliveryCharge + InstallationCharge);
}

public class AddressViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Pincode { get; set; } = string.Empty;
    public string Landmark { get; set; } = string.Empty;
    public string Type { get; set; } = "Home"; // Home or Work
    public bool IsDefault { get; set; } = false;
}

public class CheckoutViewModel
{
    public AddressViewModel Address { get; set; } = new();
    public List<AddressViewModel> SavedAddresses { get; set; } = new();
    public string ShippingMethod { get; set; } = "express"; // express, scheduled, white-glove
    public string PaymentMethod { get; set; } = "upi"; // upi, card, netbanking, emi, cod
    public CartViewModel Cart { get; set; } = new();
    public int Step { get; set; } = 1;
}

public class OrderViewModel
{
    public string Id { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; } = DateTime.Now;
    public string Status { get; set; } = "In Transit"; // Confirmed, Processing, In Transit, Delivered, Cancelled
    public string EstimatedDelivery { get; set; } = string.Empty;
    public List<CartItemViewModel> Items { get; set; } = new();
    public AddressViewModel ShippingAddress { get; set; } = new();
    public string PaymentMethod { get; set; } = "UPI (Google Pay)";
    public decimal SubTotal { get; set; }
    public decimal Discount { get; set; }
    public decimal GrandTotal { get; set; }
    public List<OrderTimelineStep> Timeline { get; set; } = new();
}

public class OrderTimelineStep
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public bool IsCompleted { get; set; } = false;
    public bool IsCurrent { get; set; } = false;
}

public class CatalogFilterViewModel
{
    public string? CategorySlug { get; set; }
    public string? CategoryName { get; set; }
    public string? SearchQuery { get; set; }
    public List<string> SelectedBrands { get; set; } = new();
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public double? MinRating { get; set; }
    public int? EnergyRating { get; set; }
    public bool InStockOnly { get; set; }
    public string SortBy { get; set; } = "popularity";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 9;
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public List<string> AvailableBrands { get; set; } = new();
    public List<ProductViewModel> Products { get; set; } = new();
}

public class ProfileViewModel
{
    public string FullName { get; set; } = "Arjun Mehta";
    public string Email { get; set; } = "arjun.mehta@example.com";
    public string Phone { get; set; } = "+91 98201 45890";
    public string Gender { get; set; } = "Male";
    public string DateOfBirth { get; set; } = "1992-08-14";
    public string MemberSince { get; set; } = "September 2023";
    public int RewardsPoints { get; set; } = 3850;
    public int TotalOrders { get; set; } = 6;
    public int WishlistCount { get; set; } = 4;
}

public class ReturnRequestViewModel
{
    public string Id { get; set; } = string.Empty;
    public string OrderId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Under Review"; // Under Review, Pickup Scheduled, Refund Initiated, Completed
    public string RequestDate { get; set; } = string.Empty;
    public decimal RefundAmount { get; set; }
}

public class ReviewItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public int Rating { get; set; } = 5;
    public string Title { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public bool IsSubmitted { get; set; } = true;
}

public class NotificationSettingsViewModel
{
    public bool OrderUpdatesEmail { get; set; } = true;
    public bool OrderUpdatesSms { get; set; } = true;
    public bool WhatsAppDeliveryAlerts { get; set; } = true;
    public bool PromotionalOffers { get; set; } = false;
    public bool NewsletterSubscription { get; set; } = true;
}

public class HomeViewModel
{
    public List<CategoryViewModel> Categories { get; set; } = new();
    public List<ProductViewModel> FeaturedProducts { get; set; } = new();
    public List<ProductViewModel> BestSellers { get; set; } = new();
    public List<ProductViewModel> FlashDeals { get; set; } = new();
    public ProductViewModel HeroProduct { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public List<ReviewViewModel> Testimonials { get; set; } = new();
}
