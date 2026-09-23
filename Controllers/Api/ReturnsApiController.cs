using System.Security.Claims;
using AuraLiving.Models;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/returns")]
public class ReturnsApiController : ControllerBase
{
    private readonly IReturnRepository _returnRepository;
    private readonly IOrderRepository _orderRepository;
    private readonly IAuthService _authService;

    public ReturnsApiController(IReturnRepository returnRepository, IOrderRepository orderRepository, IAuthService authService)
    {
        _returnRepository = returnRepository;
        _orderRepository = orderRepository;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? userId)
    {
        if (!string.IsNullOrEmpty(userId))
        {
            var userReturns = await _returnRepository.GetReturnRequestsByUserIdAsync(userId);
            return Ok(userReturns);
        }

        var allReturns = await _returnRepository.GetAllReturnRequestsAsync();
        return Ok(allReturns);
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreateReturnRequest([FromBody] CreateReturnRequestDto req)
    {
        if (req == null || string.IsNullOrWhiteSpace(req.OrderId))
        {
            return BadRequest(new { success = false, message = "Invalid request data. Order ID is required." });
        }

        var currentUser = await _authService.GetCurrentUserAsync(User);
        var userId = currentUser?.Id ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "GUEST";
        var userName = currentUser?.FullName ?? User.Identity?.Name ?? "Customer";
        var userEmail = currentUser?.Email ?? User.FindFirst(ClaimTypes.Email)?.Value ?? "";

        // Verify Order exists
        var order = await _orderRepository.GetOrderByIdAsync(req.OrderId);
        string productName = req.ProductName;
        string productImage = req.ProductImage;
        decimal refundAmt = req.RefundAmount;

        if (order != null)
        {
            if (string.IsNullOrEmpty(userEmail)) userEmail = order.CustomerEmail;
            if (string.IsNullOrEmpty(userName)) userName = order.CustomerName;

            var targetItem = order.Items.FirstOrDefault(i => i.ProductId == req.ProductId || i.ProductName.Equals(req.ProductName, StringComparison.OrdinalIgnoreCase)) ?? order.Items.FirstOrDefault();
            if (targetItem != null)
            {
                if (string.IsNullOrEmpty(productName)) productName = targetItem.ProductName;
                if (string.IsNullOrEmpty(productImage)) productImage = targetItem.ProductImage;
                if (refundAmt <= 0) refundAmt = targetItem.Total;
            }

            if (refundAmt <= 0) refundAmt = order.GrandTotal;
        }

        var model = new ReturnRequestViewModel
        {
            UserId = userId,
            CustomerName = userName,
            CustomerEmail = userEmail,
            OrderId = req.OrderId,
            ProductId = req.ProductId ?? "",
            ProductName = string.IsNullOrWhiteSpace(productName) ? "Appliance Suite Item" : productName,
            ProductImage = productImage ?? "",
            Type = string.IsNullOrWhiteSpace(req.Type) ? "Return" : req.Type,
            Reason = req.Reason ?? "Defective / Size discrepancy",
            Description = req.Description ?? "",
            Status = "Under Review",
            RefundAmount = refundAmt
        };

        string returnId = await _returnRepository.CreateReturnRequestAsync(model, userId, userName, userEmail);

        return Ok(new { 
            success = true, 
            message = $"{model.Type} request #{returnId} submitted successfully. An executive will inspect and schedule pickup.", 
            returnId = returnId 
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateReturnStatusDto dto)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(dto.Status))
        {
            return BadRequest(new { success = false, message = "Return ID and Status are required." });
        }

        bool success = await _returnRepository.UpdateReturnStatusAsync(id, dto.Status, dto.AdminNotes);
        if (success)
        {
            return Ok(new { success = true, message = $"Return request #{id} status updated to '{dto.Status}'." });
        }
        return BadRequest(new { success = false, message = "Failed to update status." });
    }
}

public class CreateReturnRequestDto
{
    public string OrderId { get; set; } = string.Empty;
    public string? ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductImage { get; set; } = string.Empty;
    public string Type { get; set; } = "Return";
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal RefundAmount { get; set; }
}

public class UpdateReturnStatusDto
{
    public string Status { get; set; } = "Under Review";
    public string? AdminNotes { get; set; }
}
