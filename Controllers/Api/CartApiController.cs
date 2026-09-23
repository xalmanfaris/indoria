using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/cart")]
public class CartApiController : ControllerBase
{
    private readonly ICartRepository _cartRepository;
    private readonly ICouponRepository _couponRepository;
    private readonly IAuthService _authService;

    public CartApiController(ICartRepository cartRepository, ICouponRepository couponRepository, IAuthService authService)
    {
        _cartRepository = cartRepository;
        _couponRepository = couponRepository;
        _authService = authService;
    }


    private async Task<string?> GetCurrentUserIdAsync()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        return currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public class AddToCartRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public string Capacity { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
    }

    public class UpdateQuantityRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Ok(new { isAuthenticated = false, count = 0 });
        }

        var count = await _cartRepository.GetCartCountAsync(userId);
        return Ok(new { isAuthenticated = true, count });
    }

    [HttpPost("add")]
    public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest req)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Please sign in to add items to your shopping bag.", redirectUrl = "/login" });
        }

        if (string.IsNullOrEmpty(req.ProductId))
        {
            return BadRequest(new { success = false, message = "Invalid Product ID." });
        }

        bool success = await _cartRepository.AddToCartAsync(userId, req.ProductId, req.Quantity, req.Capacity, req.Color);
        int count = await _cartRepository.GetCartCountAsync(userId);

        return Ok(new { success, count });
    }

    [HttpPost("update")]
    public async Task<IActionResult> UpdateQuantity([FromBody] UpdateQuantityRequest req)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Please sign in first.", redirectUrl = "/login" });
        }

        bool success = await _cartRepository.UpdateQuantityAsync(userId, req.ProductId, req.Quantity);
        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        int count = await _cartRepository.GetCartCountAsync(userId);

        return Ok(new { success, count, cart });
    }

    [HttpPost("remove/{productId}")]
    public async Task<IActionResult> RemoveFromCart(string productId)
    {
        var userId = await GetCurrentUserIdAsync();
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { success = false, message = "Please sign in first.", redirectUrl = "/login" });
        }

        bool success = await _cartRepository.RemoveFromCartAsync(userId, productId);
        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        int count = await _cartRepository.GetCartCountAsync(userId);

        return Ok(new { success, count, cart });
    }

    public class ApplyCouponRequest
    {
        public string Code { get; set; } = string.Empty;
        public decimal Subtotal { get; set; }
    }

    [HttpPost("apply-coupon")]
    public async Task<IActionResult> ApplyCoupon([FromBody] ApplyCouponRequest req)
    {
        var userId = await GetCurrentUserIdAsync();
        decimal subtotal = req.Subtotal;
        
        if (subtotal <= 0 && !string.IsNullOrEmpty(userId))
        {
            var cart = await _cartRepository.GetCartByUserIdAsync(userId);
            subtotal = cart?.SubTotal ?? 0;
        }

        var (valid, message, discountAmount, coupon) = await _couponRepository.ValidateCouponAsync(req?.Code ?? "", subtotal);

        if (!valid)
        {
            return BadRequest(new { success = false, message });
        }

        return Ok(new
        {
            success = true,
            message,
            code = coupon?.Code,
            discountAmount,
            newGrandTotal = Math.Max(0, subtotal - discountAmount)
        });
    }
}

