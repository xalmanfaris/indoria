using System.Security.Claims;
using AuraLiving.Models;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers;

[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly IAdminService _adminService;
    private readonly IProductService _productService;
    private readonly IMockDataService _mockDataService;
    private readonly IUserRepository _userRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuthService _authService;
    private readonly IReviewRepository _reviewRepository;
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IReturnRepository _returnRepository;
    private readonly ICouponRepository _couponRepository;

    public AdminController(
        IAdminService adminService, 
        IProductService productService, 
        IMockDataService mockDataService,
        IUserRepository userRepository,
        IOrderRepository orderRepository,
        IAuthService authService,
        IReviewRepository reviewRepository,
        ISupportTicketRepository ticketRepository,
        IReturnRepository returnRepository,
        ICouponRepository couponRepository)
    {
        _adminService = adminService;
        _productService = productService;
        _mockDataService = mockDataService;
        _userRepository = userRepository;
        _orderRepository = orderRepository;
        _authService = authService;
        _reviewRepository = reviewRepository;
        _ticketRepository = ticketRepository;
        _returnRepository = returnRepository;
        _couponRepository = couponRepository;
    }

    [HttpGet("returns")]
    public async Task<IActionResult> Returns(string? status)
    {
        var returns = await _returnRepository.GetAllReturnRequestsAsync();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            returns = returns.Where(r => r.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.SelectedStatus = status ?? "all";
        return View(returns);
    }

    [HttpGet("reviews")]
    public async Task<IActionResult> Reviews()
    {
        var reviews = await _reviewRepository.GetAllReviewsAdminAsync();
        return View(reviews);
    }

    [HttpGet("tickets")]
    public async Task<IActionResult> Tickets()
    {
        var tickets = await _ticketRepository.GetAllTicketsAsync();
        return View(tickets);
    }

    [HttpGet("")]
    [HttpGet("dashboard")]
    public async Task<IActionResult> Index()
    {
        var dashboardData = await _adminService.GetDashboardOverviewAsync();
        return View(dashboardData);
    }

    [HttpGet("products")]
    public async Task<IActionResult> Products(string? search, string? category)
    {
        var products = await _productService.GetAllProductsAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            products = products.Where(p => p.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                          p.Brand.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                                          p.Id.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(category) && category != "all")
        {
            products = products.Where(p => p.CategorySlug.Equals(category, StringComparison.OrdinalIgnoreCase) ||
                                          p.Category.Equals(category, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        ViewBag.Search = search;
        ViewBag.Category = category;
        ViewBag.Categories = _mockDataService.GetCategories();

        return View(products);
    }

    [HttpPost("products/save")]
    public async Task<IActionResult> SaveProduct(ProductViewModel model)
    {
        if (string.IsNullOrEmpty(model.Id))
        {
            await _productService.AddProductAsync(model);
            TempData["SuccessMessage"] = $"Product '{model.Name}' successfully added to catalog.";
        }
        else
        {
            await _productService.UpdateProductAsync(model);
            TempData["SuccessMessage"] = $"Product '{model.Name}' updated successfully.";
        }
        return RedirectToAction("Products");
    }

    [HttpPost("products/delete/{id}")]
    public async Task<IActionResult> DeleteProduct(string id)
    {
        await _productService.DeleteProductAsync(id);
        TempData["SuccessMessage"] = "Product deleted successfully.";
        return RedirectToAction("Products");
    }

    [HttpGet("orders")]
    public async Task<IActionResult> Orders(string? status)
    {
        var orders = await _orderRepository.GetAllOrdersAsync();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            orders = orders.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        ViewBag.SelectedStatus = status ?? "all";
        return View(orders);
    }

    [HttpGet("users")]
    public async Task<IActionResult> Users(string? search, string? role, string? status)
    {
        var users = await _userRepository.GetAllUsersAsync();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            users = users.Where(u => 
                (u.FullName != null && u.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (u.Email != null && u.Email.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (u.Phone != null && u.Phone.Contains(term, StringComparison.OrdinalIgnoreCase)) ||
                (u.Id != null && u.Id.Contains(term, StringComparison.OrdinalIgnoreCase))).ToList();
        }

        if (!string.IsNullOrWhiteSpace(role) && role != "all")
        {
            users = users.Where(u => u.Role.Equals(role, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            if (status.Equals("blocked", StringComparison.OrdinalIgnoreCase))
                users = users.Where(u => u.IsBlocked).ToList();
            else if (status.Equals("active", StringComparison.OrdinalIgnoreCase))
                users = users.Where(u => !u.IsBlocked).ToList();
        }

        ViewBag.Search = search;
        ViewBag.SelectedRole = role ?? "all";
        ViewBag.SelectedStatus = status ?? "all";

        return View(users);
    }

    [HttpPost("users/block/{id}")]
    public async Task<IActionResult> ToggleBlockUser(string id, [FromQuery] bool block)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var currentUserId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (id.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "You cannot block your own admin account." });
        }

        bool result = await _userRepository.ToggleUserBlockAsync(id, block);
        if (!result)
        {
            return BadRequest(new { success = false, message = "User not found or operation failed." });
        }

        string actionText = block ? "blocked" : "unblocked";
        return Ok(new { success = true, message = $"User has been successfully {actionText}.", isBlocked = block });
    }

    [HttpPost("users/role/{id}")]
    public async Task<IActionResult> ChangeUserRole(string id, [FromQuery] string newRole)
    {
        var currentUser = await _authService.GetCurrentUserAsync(User);
        var currentUserId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (id.Equals(currentUserId, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { success = false, message = "You cannot modify your own admin role." });
        }

        if (string.IsNullOrWhiteSpace(newRole) || (!newRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) && !newRole.Equals("Customer", StringComparison.OrdinalIgnoreCase)))
        {
            return BadRequest(new { success = false, message = "Invalid role specified. Only Admin or Customer roles are permitted." });
        }

        string formattedRole = newRole.Equals("Admin", StringComparison.OrdinalIgnoreCase) ? "Admin" : "Customer";
        bool result = await _userRepository.UpdateUserRoleAsync(id, formattedRole);
        if (!result)
        {
            return BadRequest(new { success = false, message = "User not found or operation failed." });
        }

        return Ok(new { success = true, message = $"User role updated to '{formattedRole}'.", newRole = formattedRole });
    }

    [HttpGet("users/details/{id}")]
    public async Task<IActionResult> GetUserDetails(string id)
    {
        var details = await _userRepository.GetUserDetailsAsync(id);
        if (details == null)
        {
            return NotFound(new { success = false, message = "User details not found." });
        }
        return Ok(details);
    }

    [HttpGet("categories")]
    public IActionResult Categories()
    {
        var categories = _mockDataService.GetCategories();
        return View(categories);
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> Coupons()
    {
        var coupons = await _adminService.GetCouponsAsync();
        return View(coupons);
    }

    [HttpPost("coupons/create")]
    public async Task<IActionResult> SaveCoupon(CouponViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            TempData["ErrorMessage"] = "Coupon code cannot be empty.";
            return RedirectToAction("Coupons");
        }

        if (model.DiscountAmount <= 0)
        {
            TempData["ErrorMessage"] = "Discount amount must be greater than zero.";
            return RedirectToAction("Coupons");
        }

        model.Code = model.Code.Trim().ToUpperInvariant();
        bool created = await _couponRepository.AddCouponAsync(model);
        if (created)
        {
            TempData["SuccessMessage"] = $"Voucher code '{model.Code}' successfully created and activated.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to create voucher code '{model.Code}'. Code may already exist.";
        }

        return RedirectToAction("Coupons");
    }

    [HttpPost("coupons/toggle/{code}")]
    public async Task<IActionResult> ToggleCouponStatus(string code, [FromQuery] bool active)
    {
        bool success = await _couponRepository.ToggleCouponStatusAsync(code, active);
        if (!success)
        {
            return BadRequest(new { success = false, message = "Coupon not found or status update failed." });
        }

        string statusText = active ? "activated" : "deactivated";
        return Ok(new { success = true, message = $"Voucher '{code}' has been {statusText}.", isActive = active });
    }

    [HttpPost("coupons/delete/{code}")]
    public async Task<IActionResult> DeleteCoupon(string code)
    {
        bool success = await _couponRepository.DeleteCouponAsync(code);
        if (success)
        {
            TempData["SuccessMessage"] = $"Voucher code '{code}' has been deleted.";
        }
        else
        {
            TempData["ErrorMessage"] = $"Failed to delete voucher code '{code}'.";
        }

        return RedirectToAction("Coupons");
    }

    [HttpGet("settings")]
    public IActionResult Settings()
    {
        return View();
    }
}

