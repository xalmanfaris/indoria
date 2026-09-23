using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/admin")]
[Authorize(Roles = "Admin")]
public class AdminApiController : ControllerBase
{
    private readonly IAdminService _adminService;
    private readonly IUserRepository _userRepository;

    public AdminApiController(IAdminService adminService, IUserRepository userRepository)
    {
        _adminService = adminService;
        _userRepository = userRepository;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard()
    {
        var dashboardData = await _adminService.GetDashboardOverviewAsync();
        return Ok(dashboardData);
    }

    [HttpGet("coupons")]
    public async Task<IActionResult> GetCoupons()
    {
        var coupons = await _adminService.GetCouponsAsync();
        return Ok(coupons);
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpPost("users/block/{id}")]
    public async Task<IActionResult> BlockUser(string id, [FromQuery] bool block)
    {
        bool success = await _userRepository.ToggleUserBlockAsync(id, block);
        return Ok(new { success, isBlocked = block });
    }

    [HttpPost("users/role/{id}")]
    public async Task<IActionResult> ChangeRole(string id, [FromQuery] string role)
    {
        bool success = await _userRepository.UpdateUserRoleAsync(id, role);
        return Ok(new { success, role });
    }

    [HttpGet("users/details/{id}")]
    public async Task<IActionResult> GetUserDetails(string id)
    {
        var details = await _userRepository.GetUserDetailsAsync(id);
        if (details == null) return NotFound(new { success = false, message = "User not found" });
        return Ok(details);
    }

    [HttpPost("reviews/toggle-status/{id}")]
    public async Task<IActionResult> ToggleReviewStatus(string id, [FromQuery] bool isActive)
    {
        var reviewRepo = HttpContext.RequestServices.GetRequiredService<IReviewRepository>();
        bool success = await reviewRepo.ToggleReviewStatusAsync(id, isActive);
        return Ok(new { success, isActive, message = isActive ? "Review published successfully." : "Review deactivated and hidden from storefront." });
    }

    [HttpPost("reviews/delete/{id}")]
    public async Task<IActionResult> DeleteReview(string id)
    {
        var reviewRepo = HttpContext.RequestServices.GetRequiredService<IReviewRepository>();
        bool success = await reviewRepo.DeleteReviewAsync(id);
        return Ok(new { success, message = "Review deleted permanently." });
    }
}
