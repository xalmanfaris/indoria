using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IWishlistRepository
{
    Task<List<ProductViewModel>> GetWishlistProductsByUserIdAsync(string userId);
    Task<bool> ToggleWishlistItemAsync(string userId, string productId);
    Task<int> GetWishlistCountAsync(string userId);
    Task<List<string>> GetWishlistProductIdsByUserIdAsync(string userId);
}

public class WishlistRepository : IWishlistRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public WishlistRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<ProductViewModel>> GetWishlistProductsByUserIdAsync(string userId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                SELECT p.* 
                FROM Products p
                INNER JOIN WishlistItems w ON p.Id = w.ProductId
                WHERE w.UserId = @UserId
                ORDER BY w.CreatedAt DESC;";
            var products = await connection.QueryAsync<ProductViewModel>(sql, new { UserId = userId });
            return products.ToList();
        }
        catch
        {
            return new List<ProductViewModel>();
        }
    }

    public async Task<List<string>> GetWishlistProductIdsByUserIdAsync(string userId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT ProductId FROM WishlistItems WHERE UserId = @UserId;";
            var productIds = await connection.QueryAsync<string>(sql, new { UserId = userId });
            return productIds.ToList();
        }
        catch
        {
            return new List<string>();
        }
    }

    public async Task<bool> ToggleWishlistItemAsync(string userId, string productId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string checkSql = "SELECT Id FROM WishlistItems WHERE UserId = @UserId AND ProductId = @ProductId;";
            var existingId = await connection.QueryFirstOrDefaultAsync<string>(checkSql, new { UserId = userId, ProductId = productId });

            if (existingId != null)
            {
                await connection.ExecuteAsync("DELETE FROM WishlistItems WHERE Id = @Id;", new { Id = existingId });
                return false;
            }
            else
            {
                string insertSql = @"
                    INSERT INTO WishlistItems (Id, UserId, ProductId)
                    VALUES (@Id, @UserId, @ProductId);";
                string newId = "WISH-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
                await connection.ExecuteAsync(insertSql, new { Id = newId, UserId = userId, ProductId = productId });
                return true;
            }
        }
        catch
        {
            return false;
        }
    }

    public async Task<int> GetWishlistCountAsync(string userId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT COUNT(*) FROM WishlistItems WHERE UserId = @UserId;";
            return await connection.ExecuteScalarAsync<int>(sql, new { UserId = userId });
        }
        catch
        {
            return 0;
        }
    }
}
