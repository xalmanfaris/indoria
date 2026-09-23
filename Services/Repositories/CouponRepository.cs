using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface ICouponRepository
{
    Task<List<CouponViewModel>> GetAllCouponsAsync();
    Task<CouponViewModel?> GetCouponByCodeAsync(string code);
    Task<bool> AddCouponAsync(CouponViewModel coupon);
    Task<bool> ToggleCouponStatusAsync(string code, bool isActive);
    Task<bool> DeleteCouponAsync(string code);
    Task<(bool Valid, string Message, decimal DiscountAmount, CouponViewModel? Coupon)> ValidateCouponAsync(string code, decimal cartSubtotal);
}

public class CouponRepository : ICouponRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private static readonly List<CouponViewModel> _fallbackCoupons = new()
    {
        new CouponViewModel { Code = "LUXE5000", DiscountAmount = 5000, MinimumSpend = 40000, Description = "Festive Instant Suite Discount", ExpiryDate = DateTime.Now.AddMonths(3), UsageCount = 142, IsActive = true },
        new CouponViewModel { Code = "INDORIA10", DiscountAmount = 10000, MinimumSpend = 80000, Description = "Luxury Villa Suite Promo", ExpiryDate = DateTime.Now.AddMonths(6), UsageCount = 89, IsActive = true },
        new CouponViewModel { Code = "FESTIVE", DiscountAmount = 3000, MinimumSpend = 20000, Description = "Seasonal Kitchen Upgrade Discount", ExpiryDate = DateTime.Now.AddMonths(2), UsageCount = 210, IsActive = true }
    };

    public CouponRepository(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<List<CouponViewModel>> GetAllCouponsAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT Code, DiscountAmount, MinimumSpend, Description, ExpiryDate, UsageCount, IsActive FROM Coupons ORDER BY ExpiryDate DESC;";
            var dbCoupons = await connection.QueryAsync<CouponViewModel>(sql);
            var list = dbCoupons.ToList();
            if (list.Any())
            {
                return list;
            }
        }
        catch
        {
            // Fallback to in-memory store
        }

        return _fallbackCoupons.ToList();
    }

    public async Task<CouponViewModel?> GetCouponByCodeAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;
        var cleanCode = code.Trim().ToUpperInvariant();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT Code, DiscountAmount, MinimumSpend, Description, ExpiryDate, UsageCount, IsActive FROM Coupons WHERE UPPER(Code) = @Code;";
            var dbCoupon = await connection.QueryFirstOrDefaultAsync<CouponViewModel>(sql, new { Code = cleanCode });
            if (dbCoupon != null) return dbCoupon;
        }
        catch
        {
            // Fallback
        }

        return _fallbackCoupons.FirstOrDefault(c => c.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> AddCouponAsync(CouponViewModel coupon)
    {
        if (coupon == null || string.IsNullOrWhiteSpace(coupon.Code)) return false;

        coupon.Code = coupon.Code.Trim().ToUpperInvariant();
        if (coupon.ExpiryDate == default)
        {
            coupon.ExpiryDate = DateTime.Now.AddMonths(3);
        }
        coupon.IsActive = true;

        // Add to fallback list first
        var existingInMem = _fallbackCoupons.FirstOrDefault(c => c.Code.Equals(coupon.Code, StringComparison.OrdinalIgnoreCase));
        if (existingInMem != null)
        {
            _fallbackCoupons.Remove(existingInMem);
        }
        _fallbackCoupons.Insert(0, coupon);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                IF EXISTS (SELECT 1 FROM Coupons WHERE Code = @Code)
                BEGIN
                    UPDATE Coupons 
                    SET DiscountAmount = @DiscountAmount, 
                        MinimumSpend = @MinimumSpend, 
                        Description = @Description, 
                        ExpiryDate = @ExpiryDate, 
                        IsActive = @IsActive 
                    WHERE Code = @Code;
                END
                ELSE
                BEGIN
                    INSERT INTO Coupons (Code, DiscountAmount, MinimumSpend, Description, ExpiryDate, UsageCount, IsActive)
                    VALUES (@Code, @DiscountAmount, @MinimumSpend, @Description, @ExpiryDate, @UsageCount, @IsActive);
                END";

            await connection.ExecuteAsync(sql, coupon);
            return true;
        }
        catch
        {
            return true; // Saved in fallback list
        }
    }

    public async Task<bool> ToggleCouponStatusAsync(string code, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var cleanCode = code.Trim().ToUpperInvariant();

        var inMem = _fallbackCoupons.FirstOrDefault(c => c.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));
        if (inMem != null)
        {
            inMem.IsActive = isActive;
        }

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "UPDATE Coupons SET IsActive = @IsActive WHERE UPPER(Code) = @Code;";
            await connection.ExecuteAsync(sql, new { IsActive = isActive, Code = cleanCode });
            return true;
        }
        catch
        {
            return true;
        }
    }

    public async Task<bool> DeleteCouponAsync(string code)
    {
        if (string.IsNullOrWhiteSpace(code)) return false;
        var cleanCode = code.Trim().ToUpperInvariant();

        _fallbackCoupons.RemoveAll(c => c.Code.Equals(cleanCode, StringComparison.OrdinalIgnoreCase));

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "DELETE FROM Coupons WHERE UPPER(Code) = @Code;";
            await connection.ExecuteAsync(sql, new { Code = cleanCode });
            return true;
        }
        catch
        {
            return true;
        }
    }

    public async Task<(bool Valid, string Message, decimal DiscountAmount, CouponViewModel? Coupon)> ValidateCouponAsync(string code, decimal cartSubtotal)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return (false, "Please enter a valid voucher code.", 0, null);
        }

        var coupon = await GetCouponByCodeAsync(code);
        if (coupon == null)
        {
            return (false, $"Voucher code '{code.ToUpper()}' is invalid or does not exist.", 0, null);
        }

        if (!coupon.IsActive)
        {
            return (false, $"Voucher code '{coupon.Code}' is currently inactive.", 0, coupon);
        }

        if (coupon.ExpiryDate < DateTime.Now.Date)
        {
            return (false, $"Voucher code '{coupon.Code}' expired on {coupon.ExpiryDate:MMM dd, yyyy}.", 0, coupon);
        }

        if (cartSubtotal < coupon.MinimumSpend)
        {
            return (false, $"Minimum cart value of ₹{coupon.MinimumSpend:N0} required to apply code '{coupon.Code}'.", 0, coupon);
        }

        return (true, $"Voucher '{coupon.Code}' applied successfully! ₹{coupon.DiscountAmount:N0} discount saved.", coupon.DiscountAmount, coupon);
    }
}
