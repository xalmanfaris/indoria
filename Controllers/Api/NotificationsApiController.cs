using AuraLiving.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/notifications")]
public class NotificationsApiController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsApiController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ANONYMOUS";
        var notifications = await _notificationService.GetUserNotificationsAsync(userId);
        var unreadCount = await _notificationService.GetUnreadCountAsync(userId);

        return Ok(new
        {
            success = true,
            unreadCount,
            notifications
        });
    }

    [HttpPost("mark-read/{id}")]
    public async Task<IActionResult> MarkRead(string id)
    {
        await _notificationService.MarkAsReadAsync(id);
        return Ok(new { success = true });
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ANONYMOUS";
        await _notificationService.MarkAllAsReadAsync(userId);
        return Ok(new { success = true });
    }
}
