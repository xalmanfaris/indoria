using System.Security.Claims;
using AuraLiving.Models;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers;

public class AccountController : Controller
{
    private readonly IAuthService _authService;
    private readonly IMockDataService _dataService;
    private readonly IAddressRepository _addressRepository;
    private readonly IWishlistRepository _wishlistRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IReturnRepository _returnRepository;
    private readonly IReviewRepository _reviewRepository;

    public AccountController(
        IAuthService authService, 
        IMockDataService dataService, 
        IAddressRepository addressRepository,
        IWishlistRepository wishlistRepository,
        IOrderRepository orderRepository,
        IReturnRepository returnRepository,
        IReviewRepository reviewRepository)
    {
        _authService = authService;
        _dataService = dataService;
        _addressRepository = addressRepository;
        _wishlistRepository = wishlistRepository;
        _orderRepository = orderRepository;
        _returnRepository = returnRepository;
        _reviewRepository = reviewRepository;
    }

    [HttpGet]
    public IActionResult Login() => View();

    [HttpPost]
    public async Task<IActionResult> Login(LoginRequestDto model)
    {
        var result = await _authService.LoginAsync(model, HttpContext);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }
        return Redirect(result.RedirectUrl);
    }

    [HttpGet]
    public IActionResult Register() => View();

    [HttpPost]
    public async Task<IActionResult> Register(RegisterRequestDto model)
    {
        var result = await _authService.RegisterAsync(model, HttpContext);
        if (!result.Success)
        {
            ViewBag.ErrorMessage = result.Message;
            return View(model);
        }
        return Redirect(result.RedirectUrl);
    }

    [Route("logout")]
    [Route("account/logout")]
    public async Task<IActionResult> Logout()
    {
        await _authService.LogoutAsync(HttpContext);
        return Redirect("/");
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";

        var wishlistCount = !string.IsNullOrEmpty(userId)
            ? await _wishlistRepository.GetWishlistCountAsync(userId)
            : 0;

        var userAddresses = !string.IsNullOrEmpty(userId)
            ? await _addressRepository.GetAddressesByUserIdAsync(userId)
            : new List<AddressViewModel>();

        var userOrders = !string.IsNullOrEmpty(userId)
            ? await _orderRepository.GetOrdersByUserIdAsync(userId)
            : new List<OrderViewModel>();

        var recentOrders = userOrders.Take(2).ToList();

        var profile = new ProfileViewModel
        {
            FullName = currentUser?.FullName ?? User.Identity?.Name ?? "VIP Member",
            Email = currentUser?.Email ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            Phone = !string.IsNullOrEmpty(currentUser?.Phone) ? currentUser.Phone : "+91 98201 45890",
            MemberSince = currentUser?.CreatedAt.ToString("MMMM yyyy") ?? "September 2026",
            RewardsPoints = userOrders.Count * 250,
            TotalOrders = userOrders.Count,
            WishlistCount = wishlistCount
        };

        ViewBag.RecentOrders = recentOrders;
        ViewBag.Addresses = userAddresses;
        return View(profile);
    }

    [Authorize]
    public async Task<IActionResult> Profile()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var userOrders = !string.IsNullOrEmpty(userId)
            ? await _orderRepository.GetOrdersByUserIdAsync(userId)
            : new List<OrderViewModel>();
        var wishlistCount = !string.IsNullOrEmpty(userId)
            ? await _wishlistRepository.GetWishlistCountAsync(userId)
            : 0;

        var profile = new ProfileViewModel
        {
            FullName = currentUser?.FullName ?? User.Identity?.Name ?? "VIP Member",
            Email = currentUser?.Email ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "",
            Phone = !string.IsNullOrEmpty(currentUser?.Phone) ? currentUser.Phone : "+91 98201 45890",
            MemberSince = currentUser?.CreatedAt.ToString("MMMM yyyy") ?? "September 2026",
            RewardsPoints = userOrders.Count * 250,
            TotalOrders = userOrders.Count,
            WishlistCount = wishlistCount
        };
        return View(profile);
    }

    [Authorize]
    public async Task<IActionResult> Addresses()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var addresses = await _addressRepository.GetAddressesByUserIdAsync(userId);
        return View(addresses);
    }

    [Authorize]
    [HttpPost("account/addresses/save")]
    public async Task<IActionResult> SaveAddress(AddressViewModel model)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        if (!string.IsNullOrEmpty(userId))
        {
            model.UserId = userId;
            await _addressRepository.AddAddressAsync(model);
            TempData["SuccessMessage"] = "Delivery address saved successfully.";
        }
        return RedirectToAction("Addresses");
    }

    [Authorize]
    [HttpPost("account/addresses/delete/{id}")]
    public async Task<IActionResult> DeleteAddress(string id)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(id))
        {
            await _addressRepository.DeleteAddressAsync(id, userId);
            TempData["SuccessMessage"] = "Address removed.";
        }
        return RedirectToAction("Addresses");
    }

    [Authorize]
    public async Task<IActionResult> Orders()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var orders = await _orderRepository.GetOrdersByUserIdAsync(userId);
        return View(orders);
    }

    [Authorize]
    public async Task<IActionResult> OrderDetails(string id)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id) ?? _dataService.GetOrderById(id) ?? new OrderViewModel();
        return View(order);
    }

    [Authorize]
    [HttpGet("account/orders/{id}/invoice")]
    public async Task<IActionResult> Invoice(string id)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id) ?? _dataService.GetOrderById(id);
        if (order == null) return NotFound("Order not found");
        return View("Invoice", order);
    }

    [Authorize]
    public async Task<IActionResult> Returns()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var userReturns = await _returnRepository.GetReturnRequestsByUserIdAsync(userId);
        var userOrders = await _orderRepository.GetOrdersByUserIdAsync(userId);
        ViewBag.UserOrders = userOrders;
        return View(userReturns);
    }

    [Authorize]
    public async Task<IActionResult> Reviews()
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "";
        var userName = currentUser?.FullName ?? User.Identity?.Name ?? "";

        var userReviews = await _reviewRepository.GetReviewsByUserIdAsync(userId);
        if (!userReviews.Any() && !string.IsNullOrEmpty(userName))
        {
            var byName = await _reviewRepository.GetReviewsByUserIdAsync(userName);
            if (byName.Any()) userReviews = byName;
        }

        var userOrders = await _orderRepository.GetOrdersByUserIdAsync(userId);
        ViewBag.UserOrders = userOrders;
        return View(userReviews);
    }

    public IActionResult Notifications()
    {
        var settings = _dataService.GetNotificationSettings();
        return View(settings);
    }

    public IActionResult Settings() => View();
}
