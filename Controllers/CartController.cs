using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuraLiving.Models;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;

namespace AuraLiving.Controllers;

public class CartController : Controller
{
    private readonly ICartRepository _cartRepository;
    private readonly IMockDataService _dataService;
    private readonly IAddressRepository _addressRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuthService _authService;

    public CartController(
        ICartRepository cartRepository,
        IMockDataService dataService,
        IAddressRepository addressRepository,
        IOrderRepository orderRepository,
        IAuthService authService)
    {
        _cartRepository = cartRepository;
        _dataService = dataService;
        _addressRepository = addressRepository;
        _orderRepository = orderRepository;
        _authService = authService;
    }

    public async Task<IActionResult> Index()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        var cart = !string.IsNullOrEmpty(userId)
            ? await _cartRepository.GetCartByUserIdAsync(userId)
            : new CartViewModel();

        return View(cart);
    }

    [Authorize]
    public async Task<IActionResult> Checkout([FromQuery] int step = 1)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        
        var addresses = !string.IsNullOrEmpty(userId) 
            ? await _addressRepository.GetAddressesByUserIdAsync(userId) 
            : new List<AddressViewModel>();

        var cart = !string.IsNullOrEmpty(userId)
            ? await _cartRepository.GetCartByUserIdAsync(userId)
            : new CartViewModel();

        var model = new CheckoutViewModel
        {
            Address = addresses.FirstOrDefault(a => a.IsDefault) ?? addresses.FirstOrDefault() ?? new AddressViewModel(),
            SavedAddresses = addresses,
            Cart = cart,
            Step = Math.Clamp(step, 1, 4)
        };

        return View(model);
    }

    [HttpPost, Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> PlaceOrder(string? selectedAddressId, string? shippingSpeed, string? paymentMethod)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Account");
        }

        var cart = await _cartRepository.GetCartByUserIdAsync(userId);
        if (cart == null || !cart.Items.Any())
        {
            return RedirectToAction("Index", "Cart");
        }

        // Get selected or default address
        var addresses = await _addressRepository.GetAddressesByUserIdAsync(userId);
        var address = addresses.FirstOrDefault(a => a.Id == selectedAddressId)
                      ?? addresses.FirstOrDefault(a => a.IsDefault)
                      ?? addresses.FirstOrDefault()
                      ?? new AddressViewModel
                      {
                          FullName = currentUser?.FullName ?? "Valued Patron",
                          Phone = currentUser?.Phone ?? "",
                          Email = currentUser?.Email ?? "",
                          AddressLine1 = "14B Horizon Royale",
                          City = "Mumbai",
                          State = "Maharashtra",
                          Pincode = "400705",
                          Type = "Home"
                      };

        var order = new OrderViewModel
        {
            Id = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Random.Shared.Next(1000, 9999),
            OrderDate = DateTime.Now,
            Status = "Confirmed",
            EstimatedDelivery = DateTime.Now.AddDays(2).ToString("dddd, MMM dd"),
            PaymentMethod = string.IsNullOrWhiteSpace(paymentMethod) ? "UPI Transfer (Google Pay)" : paymentMethod,
            Items = cart.Items,
            ShippingAddress = address,
            SubTotal = cart.SubTotal,
            Discount = cart.Discount,
            GrandTotal = cart.GrandTotal
        };

        var orderId = await _orderRepository.CreateOrderAsync(order, userId, currentUser?.Email ?? address.Email ?? "", currentUser?.FullName ?? address.FullName ?? "");

        return RedirectToAction("OrderConfirmation", new { id = orderId });
    }

    public async Task<IActionResult> OrderConfirmation(string id)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id) 
                    ?? _dataService.GetOrderById(id) 
                    ?? _dataService.GetSampleOrders().FirstOrDefault() 
                    ?? new OrderViewModel();
        return View(order);
    }
}
