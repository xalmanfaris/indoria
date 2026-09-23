using System.Text.Json;
using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IProductRepository
{
    Task<List<ProductViewModel>> GetAllProductsAsync();
    Task<ProductViewModel?> GetByIdAsync(string id);
    Task<ProductViewModel?> GetBySlugAsync(string slug);
    Task<bool> AddProductAsync(ProductViewModel product);
    Task<bool> UpdateProductAsync(ProductViewModel product);
    Task<bool> DeleteProductAsync(string id);
    Task<bool> AdjustStockAsync(string productId, int delta);
}

public class ProductRepository : IProductRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IMockDataService _mockDataService;
    private readonly IReviewRepository _reviewRepository;

    public ProductRepository(IDbConnectionFactory connectionFactory, IMockDataService mockDataService, IReviewRepository reviewRepository)
    {
        _connectionFactory = connectionFactory;
        _mockDataService = mockDataService;
        _reviewRepository = reviewRepository;
    }

    private void PopulateJsonFields(ProductViewModel p)
    {
        if (p == null) return;
        if (!string.IsNullOrWhiteSpace(p.KeyFeaturesJson))
        {
            try { p.KeyFeatures = JsonSerializer.Deserialize<List<string>>(p.KeyFeaturesJson) ?? new List<string>(); } catch { }
        }
        if (p.KeyFeatures.Any())
        {
            p.RawKeyFeatures = string.Join("\n", p.KeyFeatures);
        }

        if (!string.IsNullOrWhiteSpace(p.SpecificationsJson))
        {
            try { p.Specifications = JsonSerializer.Deserialize<Dictionary<string, string>>(p.SpecificationsJson) ?? new Dictionary<string, string>(); } catch { }
        }
        if (p.Specifications.Any())
        {
            p.RawSpecifications = string.Join("\n", p.Specifications.Select(kv => $"{kv.Key}: {kv.Value}"));
        }

        if (!string.IsNullOrWhiteSpace(p.GalleryImagesJson))
        {
            try { p.GalleryImages = JsonSerializer.Deserialize<List<string>>(p.GalleryImagesJson) ?? new List<string>(); } catch { }
        }
        if (p.GalleryImages.Count > 0) p.Image2 = p.GalleryImages[0];
        if (p.GalleryImages.Count > 1) p.Image3 = p.GalleryImages[1];
    }

    private async Task AttachReviewsAsync(ProductViewModel? p)
    {
        if (p == null || string.IsNullOrEmpty(p.Id)) return;
        var reviews = await _reviewRepository.GetReviewsByProductIdAsync(p.Id);
        if ((reviews == null || !reviews.Any()) && !string.IsNullOrEmpty(p.Slug))
        {
            reviews = await _reviewRepository.GetReviewsByProductIdAsync(p.Slug);
        }

        p.Reviews = reviews ?? new List<ReviewViewModel>();
        p.ReviewCount = p.Reviews.Count;
        if (p.Reviews.Any())
        {
            p.Rating = Math.Round(p.Reviews.Average(r => r.Rating), 1);
        }
    }

    public async Task<List<ProductViewModel>> GetAllProductsAsync()
    {
        List<ProductViewModel> products;
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Products ORDER BY Price DESC";
            products = (await connection.QueryAsync<ProductViewModel>(sql)).ToList();
            foreach (var p in products)
            {
                PopulateJsonFields(p);
            }
        }
        catch
        {
            products = _mockDataService.GetAllProducts();
        }

        foreach (var p in products)
        {
            await AttachReviewsAsync(p);
        }
        return products;
    }

    public async Task<ProductViewModel?> GetByIdAsync(string id)
    {
        ProductViewModel? product = null;
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Products WHERE Id = @Id";
            product = await connection.QueryFirstOrDefaultAsync<ProductViewModel>(sql, new { Id = id });
            if (product != null)
            {
                PopulateJsonFields(product);
            }
            else
            {
                product = _mockDataService.GetAllProducts().FirstOrDefault(p => p.Id == id);
            }
        }
        catch
        {
            product = _mockDataService.GetAllProducts().FirstOrDefault(p => p.Id == id);
        }

        await AttachReviewsAsync(product);
        return product;
    }

    public async Task<ProductViewModel?> GetBySlugAsync(string slug)
    {
        ProductViewModel? product = null;
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Products WHERE Slug = @Slug";
            product = await connection.QueryFirstOrDefaultAsync<ProductViewModel>(sql, new { Slug = slug });
            if (product != null)
            {
                PopulateJsonFields(product);
            }
            else
            {
                product = _mockDataService.GetProductBySlug(slug);
            }
        }
        catch
        {
            product = _mockDataService.GetProductBySlug(slug);
        }

        await AttachReviewsAsync(product);
        return product;
    }

    public async Task<bool> AddProductAsync(ProductViewModel product)
    {
        try
        {
            product.KeyFeaturesJson = JsonSerializer.Serialize(product.KeyFeatures);
            product.SpecificationsJson = JsonSerializer.Serialize(product.Specifications);

            product.GalleryImages = new List<string>();
            if (!string.IsNullOrWhiteSpace(product.Image2)) product.GalleryImages.Add(product.Image2.Trim());
            if (!string.IsNullOrWhiteSpace(product.Image3)) product.GalleryImages.Add(product.Image3.Trim());
            product.GalleryImagesJson = JsonSerializer.Serialize(product.GalleryImages);

            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO Products (Id, Name, Slug, Brand, Category, CategorySlug, Price, OriginalPrice, StockCount, InStock, MainImage, ShortDescription, FullDescription, KeyFeaturesJson, SpecificationsJson, GalleryImagesJson, VideoUrl)
                VALUES (@Id, @Name, @Slug, @Brand, @Category, @CategorySlug, @Price, @OriginalPrice, @StockCount, @InStock, @MainImage, @ShortDescription, @FullDescription, @KeyFeaturesJson, @SpecificationsJson, @GalleryImagesJson, @VideoUrl);";
            int rows = await connection.ExecuteAsync(sql, product);
            _mockDataService.AddProduct(product);
            return rows > 0;
        }
        catch
        {
            _mockDataService.AddProduct(product);
            return true;
        }
    }

    public async Task<bool> UpdateProductAsync(ProductViewModel product)
    {
        try
        {
            product.KeyFeaturesJson = JsonSerializer.Serialize(product.KeyFeatures);
            product.SpecificationsJson = JsonSerializer.Serialize(product.Specifications);

            product.GalleryImages = new List<string>();
            if (!string.IsNullOrWhiteSpace(product.Image2)) product.GalleryImages.Add(product.Image2.Trim());
            if (!string.IsNullOrWhiteSpace(product.Image3)) product.GalleryImages.Add(product.Image3.Trim());
            product.GalleryImagesJson = JsonSerializer.Serialize(product.GalleryImages);

            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                UPDATE Products 
                SET Name = @Name, Brand = @Brand, Category = @Category, Price = @Price, 
                    OriginalPrice = @OriginalPrice, StockCount = @StockCount, InStock = @InStock, MainImage = @MainImage,
                    ShortDescription = @ShortDescription, FullDescription = @FullDescription,
                    KeyFeaturesJson = @KeyFeaturesJson, SpecificationsJson = @SpecificationsJson,
                    GalleryImagesJson = @GalleryImagesJson, VideoUrl = @VideoUrl
                WHERE Id = @Id;";
            int rows = await connection.ExecuteAsync(sql, product);
            _mockDataService.UpdateProduct(product);
            return rows > 0;
        }
        catch
        {
            _mockDataService.UpdateProduct(product);
            return true;
        }
    }

    public async Task<bool> DeleteProductAsync(string id)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM Products WHERE Id = @Id;";
            int rows = await connection.ExecuteAsync(sql, new { Id = id });
            _mockDataService.DeleteProduct(id);
            return rows > 0;
        }
        catch
        {
            _mockDataService.DeleteProduct(id);
            return true;
        }
    }

    public async Task<bool> AdjustStockAsync(string productId, int delta)
    {
        if (string.IsNullOrEmpty(productId) || delta == 0) return false;

        bool updatedInDb = false;
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string selectSql = "SELECT * FROM Products WHERE Id = @Id OR Slug = @Id;";
            var product = await connection.QueryFirstOrDefaultAsync<ProductViewModel>(selectSql, new { Id = productId });
            if (product != null)
            {
                int newStock = Math.Max(0, product.StockCount + delta);
                bool inStock = newStock > 0;
                string updateSql = "UPDATE Products SET StockCount = @StockCount, InStock = @InStock WHERE Id = @Id;";
                await connection.ExecuteAsync(updateSql, new { StockCount = newStock, InStock = inStock, Id = product.Id });
                updatedInDb = true;
            }
        }
        catch { }

        // Also update in-memory mock data list
        var mockProducts = _mockDataService.GetAllProducts();
        var mockProd = mockProducts.FirstOrDefault(p => p.Id.Equals(productId, StringComparison.OrdinalIgnoreCase) || 
                                                        (p.Slug != null && p.Slug.Equals(productId, StringComparison.OrdinalIgnoreCase)));
        if (mockProd != null)
        {
            mockProd.StockCount = Math.Max(0, mockProd.StockCount + delta);
            mockProd.InStock = mockProd.StockCount > 0;
        }

        return updatedInDb || mockProd != null;
    }
}
