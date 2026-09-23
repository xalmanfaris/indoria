using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IAddressRepository
{
    Task<List<AddressViewModel>> GetAddressesByUserIdAsync(string userId);
    Task<bool> AddAddressAsync(AddressViewModel address);
    Task<bool> DeleteAddressAsync(string id, string userId);
}

public class AddressRepository : IAddressRepository
{
    private readonly IDbConnectionFactory _connectionFactory;

    public AddressRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<AddressViewModel>> GetAddressesByUserIdAsync(string userId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM UserAddresses WHERE UserId = @UserId ORDER BY IsDefault DESC, CreatedAt DESC";
            var addresses = await connection.QueryAsync<AddressViewModel>(sql, new { UserId = userId });
            return addresses.ToList();
        }
        catch
        {
            return new List<AddressViewModel>();
        }
    }

    public async Task<bool> AddAddressAsync(AddressViewModel address)
    {
        try
        {
            if (string.IsNullOrEmpty(address.Id))
            {
                address.Id = "ADDR-" + Guid.NewGuid().ToString("N")[..8].ToUpper();
            }

            using var connection = _connectionFactory.CreateConnection();

            if (address.IsDefault)
            {
                await connection.ExecuteAsync("UPDATE UserAddresses SET IsDefault = 0 WHERE UserId = @UserId", new { UserId = address.UserId });
            }
            else
            {
                int count = await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM UserAddresses WHERE UserId = @UserId", new { UserId = address.UserId });
                if (count == 0) address.IsDefault = true;
            }

            string sql = @"
                INSERT INTO UserAddresses (Id, UserId, FullName, Phone, Email, AddressLine1, AddressLine2, City, State, Pincode, Landmark, Type, IsDefault)
                VALUES (@Id, @UserId, @FullName, @Phone, @Email, @AddressLine1, @AddressLine2, @City, @State, @Pincode, @Landmark, @Type, @IsDefault);";

            int rows = await connection.ExecuteAsync(sql, address);
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteAddressAsync(string id, string userId)
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM UserAddresses WHERE Id = @Id AND UserId = @UserId;";
            int rows = await connection.ExecuteAsync(sql, new { Id = id, UserId = userId });
            return rows > 0;
        }
        catch
        {
            return false;
        }
    }
}
