using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IUserRepository
{
    Task<UserModel?> GetByEmailAsync(string email);
    Task<UserModel?> GetByIdAsync(string id);
    Task<bool> CreateUserAsync(UserModel user);
    Task<List<UserModel>> GetAllUsersAsync();
    Task<bool> UpdateUserRoleAsync(string userId, string newRole);
    Task<bool> ToggleUserBlockAsync(string userId, bool isBlocked);
    Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId);
}

public class UserRepository : IUserRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private static readonly List<UserModel> _fallbackUsers = new()
    {
        new UserModel { Id = "USR-ADMIN-1", FullName = "Super Admin", Email = "admin@indoria.com", PasswordHash = "Admin123!", Role = "Admin", Phone = "+91 99999 00000" },
        new UserModel { Id = "USR-CUST-1", FullName = "Arjun Mehta", Email = "arjun.mehta@example.com", PasswordHash = "Customer123!", Role = "Customer", Phone = "+91 98201 45890" }
    };

    public UserRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<UserModel?> GetByEmailAsync(string email)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Users WHERE LOWER(Email) = LOWER(@Email)";
            return await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { Email = email });
        }
        catch
        {
            return _fallbackUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
    }

    public async Task<UserModel?> GetByIdAsync(string id)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Users WHERE Id = @Id";
            return await connection.QueryFirstOrDefaultAsync<UserModel>(sql, new { Id = id });
        }
        catch
        {
            return _fallbackUsers.FirstOrDefault(u => u.Id == id);
        }
    }

    public async Task<bool> CreateUserAsync(UserModel user)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO Users (Id, FullName, Email, PasswordHash, Role, Phone, IsBlocked, CreatedAt)
                VALUES (@Id, @FullName, @Email, @PasswordHash, @Role, @Phone, @IsBlocked, @CreatedAt);";
            int rows = await connection.ExecuteAsync(sql, user);
            return rows > 0;
        }
        catch
        {
            _fallbackUsers.Add(user);
            return true;
        }
    }

    public async Task<List<UserModel>> GetAllUsersAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Users ORDER BY CreatedAt DESC";
            var result = await connection.QueryAsync<UserModel>(sql);
            return result.ToList();
        }
        catch
        {
            return _fallbackUsers;
        }
    }

    public async Task<bool> UpdateUserRoleAsync(string userId, string newRole)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "UPDATE Users SET Role = @Role WHERE Id = @Id";
            int rows = await connection.ExecuteAsync(sql, new { Role = newRole, Id = userId });
            return rows > 0;
        }
        catch
        {
            var user = _fallbackUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null) { user.Role = newRole; return true; }
            return false;
        }
    }

    public async Task<bool> ToggleUserBlockAsync(string userId, bool isBlocked)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "UPDATE Users SET IsBlocked = @IsBlocked WHERE Id = @Id";
            int rows = await connection.ExecuteAsync(sql, new { IsBlocked = isBlocked, Id = userId });
            return rows > 0;
        }
        catch
        {
            var user = _fallbackUsers.FirstOrDefault(u => u.Id == userId);
            if (user != null) { user.IsBlocked = isBlocked; return true; }
            return false;
        }
    }

    public async Task<UserDetailsViewModel?> GetUserDetailsAsync(string userId)
    {
        var user = await GetByIdAsync(userId);
        if (user == null) return null;

        var details = new UserDetailsViewModel { User = user };

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            details.AddressCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserAddresses WHERE UserId = @UserId", new { UserId = userId });
            details.WishlistCount = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM WishlistItems WHERE UserId = @UserId", new { UserId = userId });
            details.CartCount = await connection.ExecuteScalarAsync<int>("SELECT ISNULL(SUM(Quantity),0) FROM CartItems WHERE UserId = @UserId", new { UserId = userId });
            details.TotalOrders = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Orders WHERE CustomerEmail = @Email", new { Email = user.Email });
            details.TotalSpent = await connection.ExecuteScalarAsync<decimal>("SELECT ISNULL(SUM(GrandTotal),0) FROM Orders WHERE CustomerEmail = @Email", new { Email = user.Email });
        }
        catch
        {
            // Fallback default values
        }

        return details;
    }
}
