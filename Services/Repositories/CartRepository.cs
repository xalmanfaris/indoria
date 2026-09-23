using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface ICartRepository
{
    Task<CartViewModel> GetCartByUserIdAsync(string userId);
    Task<bool> AddToCartAsync(string userId, string productId, int quantity = 1, string selectedCapacity = "", string selectedColor = "");
    Task<bool> UpdateQuantityAsync(string userId, string productId, int quantity);
    Task<bool> RemoveFromCartAsync(string userId, string productId);
    Task<int> GetCartCountAsync(string userId);
    Task<bool> ClearCartAsync(string userId);
}

public class CartRepository : ICartRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public CartRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<CartViewModel> GetCartByUserIdAsync(string userId)
    {
        var cart = new CartViewModel();
        if (string.IsNullOrEmpty(userId)) return cart;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT 
                    c.ProductId,
                    p.Name AS ProductName,
                    p.Slug AS ProductSlug,
                    p.Brand,
                    p.MainImage AS Image,
                    p.Price,
                    p.OriginalPrice,
                    c.SelectedCapacity,
                    c.SelectedColor,
                    c.Quantity
                FROM CartItems c
                INNER JOIN Products p ON c.ProductId = p.Id
                WHERE c.UserId = @UserId
                ORDER BY c.CreatedAt DESC;";

            var items = await connection.QueryAsync<CartItemViewModel>(sql, new { UserId = userId });
            cart.Items = items.ToList();

            if (cart.Items.Any())
            {
                cart.CouponCode = string.Empty;
                cart.Discount = 0;
            }


            return cart;
        }
        catch
        {
            return cart;
        }
    }

    public async Task<bool> AddToCartAsync(string userId, string productId, int quantity = 1, string selectedCapacity = "", string selectedColor = "")
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productId)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string checkSql = "SELECT Id, Quantity FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;";
            var existing = await connection.QueryFirstOrDefaultAsync<(string Id, int Quantity)?>(checkSql, new { UserId = userId, ProductId = productId });

            if (existing.HasValue)
            {
                string updateSql = "UPDATE CartItems SET Quantity = Quantity + @AddQuantity WHERE Id = @Id;";
                await connection.ExecuteAsync(updateSql, new { AddQuantity = Math.Max(1, quantity), Id = existing.Value.Id });
            }
            else
            {
                string insertSql = @"
                    INSERT INTO CartItems (Id, UserId, ProductId, Quantity, SelectedCapacity, SelectedColor)
                    VALUES (@Id, @UserId, @ProductId, @Quantity, @SelectedCapacity, @SelectedColor);";
                string newId = "CART-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                await connection.ExecuteAsync(insertSql, new {
                    Id = newId,
                    UserId = userId,
                    ProductId = productId,
                    Quantity = Math.Max(1, quantity),
                    SelectedCapacity = selectedCapacity ?? "",
                    SelectedColor = selectedColor ?? ""
                });
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> UpdateQuantityAsync(string userId, string productId, int quantity)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productId)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            if (quantity <= 0)
            {
                return await RemoveFromCartAsync(userId, productId);
            }

            string sql = "UPDATE CartItems SET Quantity = @Quantity WHERE UserId = @UserId AND ProductId = @ProductId;";
            await connection.ExecuteAsync(sql, new { Quantity = quantity, UserId = userId, ProductId = productId });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> RemoveFromCartAsync(string userId, string productId)
    {
        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(productId)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM CartItems WHERE UserId = @UserId AND ProductId = @ProductId;";
            await connection.ExecuteAsync(sql, new { UserId = userId, ProductId = productId });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetCartCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return 0;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT ISNULL(SUM(Quantity), 0) FROM CartItems WHERE UserId = @UserId;";
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> ClearCartAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM CartItems WHERE UserId = @UserId;";
            await connection.ExecuteAsync(sql, new { UserId = userId });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
