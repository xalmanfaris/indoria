using AuraLiving.Models;
using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/v1/orders")]
public class OrdersApiController : ControllerBase
{
    private readonly IOrderRepository _orderRepository;

    public OrdersApiController(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var orders = await _orderRepository.GetAllOrdersAsync();
        if (!string.IsNullOrWhiteSpace(status) && status != "all")
        {
            orders = orders.Where(o => o.Status.Equals(status, StringComparison.OrdinalIgnoreCase)).ToList();
        }
        return Ok(orders);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "Order not found" });
        return Ok(order);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateStatusRequest statusReq)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { message = "Order not found" });
        await _orderRepository.UpdateOrderStatusAsync(id, statusReq.Status);
        order.Status = statusReq.Status;
        return Ok(new { success = true, message = $"Order status updated to '{statusReq.Status}'", order });
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelOrder(string id, [FromBody] CancelOrderRequestDto cancelReq)
    {
        var order = await _orderRepository.GetOrderByIdAsync(id);
        if (order == null) return NotFound(new { success = false, message = "Order not found." });

        if (order.Status == "Cancelled")
        {
            return BadRequest(new { success = false, message = "This order has already been cancelled." });
        }

        if (order.Status == "Delivered")
        {
            return BadRequest(new { success = false, message = "Delivered orders cannot be cancelled directly. Please request a return." });
        }

        string reasonText = cancelReq.Reason;
        if (!string.IsNullOrWhiteSpace(cancelReq.CustomReason))
        {
            reasonText = string.IsNullOrWhiteSpace(reasonText) || reasonText == "Other"
                ? cancelReq.CustomReason.Trim()
                : $"{reasonText}: {cancelReq.CustomReason.Trim()}";
        }

        if (string.IsNullOrWhiteSpace(reasonText))
        {
            reasonText = "Customer requested cancellation";
        }

        bool success = await _orderRepository.CancelOrderAsync(id, reasonText);
        if (success)
        {
            return Ok(new { success = true, message = "Order cancelled successfully.", reason = reasonText });
        }

        return BadRequest(new { success = false, message = "Failed to cancel order. Please try again." });
    }
}

public class UpdateStatusRequest
{
    public string Status { get; set; } = "Processing";
}

public class CancelOrderRequestDto
{
    public string Reason { get; set; } = "Changed my mind";
    public string? CustomReason { get; set; }
}
