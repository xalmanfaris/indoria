using AuraLiving.Services;
using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuraLiving.Controllers.Api;

[ApiController]
[Route("api/support")]
public class SupportApiController : ControllerBase
{
    private readonly ISupportTicketRepository _ticketRepo;
    private readonly INotificationService _notificationService;

    public SupportApiController(ISupportTicketRepository ticketRepo, INotificationService notificationService)
    {
        _ticketRepo = ticketRepo;
        _notificationService = notificationService;
    }

    [HttpPost("chat-bot")]
    public async Task<IActionResult> ChatBot([FromBody] ChatBotRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Message))
        {
            return BadRequest(new { reply = "Please type a question or inquiry." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "GUEST";
        var userName = User.Identity?.Name ?? "Live Chat Guest";
        var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? "guest@indoria.com";

        // Check if there is an existing Open Live Chat ticket for this user
        var userTickets = await _ticketRepo.GetUserTicketsAsync(userId);
        var activeTicket = userTickets.FirstOrDefault(t => t.Category == "Live Chat" && (t.Status == "Open" || t.Status == "In Progress" || t.Status == "Customer Reply"));

        if (activeTicket == null)
        {
            activeTicket = new SupportTicket
            {
                UserId = userId,
                CustomerName = userName,
                Email = userEmail,
                Subject = $"Live Chat: {req.Message[..Math.Min(35, req.Message.Length)]}...",
                Category = "Live Chat",
                Priority = "Medium"
            };
            activeTicket = await _ticketRepo.CreateTicketAsync(activeTicket, req.Message);
        }
        else
        {
            await _ticketRepo.AddMessageAsync(activeTicket.Id, "Customer", userName, req.Message, "Customer Reply");
        }

        // Notify Admin Console of the live message
        await _notificationService.SendNotificationAsync(
            "USR-ADMIN-1", 
            $"Incoming Support Message from {userName}", 
            $"Live Chat #{activeTicket.TicketNumber}: {req.Message}", 
            "Support", 
            "In-App"
        );

        string msg = req.Message.ToLowerInvariant();
        string reply;

        if (msg.Contains("order") || msg.Contains("track") || msg.Contains("status"))
        {
            reply = "You can track your order status anytime under 'My Orders' in your Account Dashboard. I have dispatched your inquiry to our live support team!";
        }
        else if (msg.Contains("shipping") || msg.Contains("delivery") || msg.Contains("days") || msg.Contains("cost"))
        {
            reply = "We offer standard shipping (3-5 business days) and express shipping (1-2 business days). Shipping fees vary by country and are calculated at checkout.";
        }
        else if (msg.Contains("return") || msg.Contains("refund") || msg.Contains("exchange"))
        {
            reply = "Indoria offers a 30-day hassle-free return policy on all eligible appliances. You can request a return directly from your Order Details page.";
        }
        else if (msg.Contains("warranty") || msg.Contains("guarantee"))
        {
            reply = "All Indoria appliances come with a 2-Year Comprehensive Brand Warranty + optional Extended Protection Plans available at purchase.";
        }
        else if (msg.Contains("payment") || msg.Contains("cod") || msg.Contains("card") || msg.Contains("upi"))
        {
            reply = "We accept Credit/Debit Cards, UPI, NetBanking, and Cash on Delivery (COD) across all supported regions.";
        }
        else
        {
            reply = $"Thank you! Your message has been logged under Support Ticket #{activeTicket.TicketNumber}. Our live team has been notified and will assist you shortly!";
        }

        return Ok(new
        {
            success = true,
            reply,
            ticketNumber = activeTicket.TicketNumber,
            timestamp = DateTime.UtcNow.ToString("HH:mm")
        });
    }

    [HttpPost("tickets/create")]
    public async Task<IActionResult> CreateTicket([FromBody] CreateTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
        {
            return BadRequest(new { success = false, message = "Subject and Message are required." });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "GUEST";
        var userName = req.CustomerName ?? User.Identity?.Name ?? "Valued Patron";

        var ticket = new SupportTicket
        {
            UserId = userId,
            CustomerName = userName,
            Email = req.Email ?? User.FindFirstValue(ClaimTypes.Email) ?? "customer@indoria.com",
            Subject = req.Subject,
            Category = req.Category ?? "General Support",
            Priority = req.Priority ?? "Medium"
        };

        var created = await _ticketRepo.CreateTicketAsync(ticket, req.Message);

        // Notify Admin Console of the new ticket
        await _notificationService.SendNotificationAsync(
            "USR-ADMIN-1",
            $"New Support Ticket #{created.TicketNumber}: {created.Subject}",
            $"Customer {userName} submitted a new inquiry regarding '{created.Category}'.",
            "Support",
            "Email & In-App"
        );

        return Ok(new
        {
            success = true,
            message = "Support Ticket created successfully!",
            ticketId = created.Id,
            ticketNumber = created.TicketNumber
        });
    }

    [HttpPost("tickets/reply")]
    public async Task<IActionResult> ReplyTicket([FromBody] ReplyTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TicketId) || string.IsNullOrWhiteSpace(req.Message))
        {
            return BadRequest(new { success = false, message = "Ticket ID and Message are required." });
        }

        bool isAdmin = User.IsInRole("Admin");
        string role = isAdmin ? "Support Agent" : "Customer";
        string senderName = isAdmin ? "Indoria Support Desk" : (User.Identity?.Name ?? "Customer");

        await _ticketRepo.AddMessageAsync(req.TicketId, role, senderName, req.Message, req.Status);

        var ticket = await _ticketRepo.GetTicketByIdAsync(req.TicketId);
        if (ticket != null)
        {
            // If reply is from customer, notify admin. If from admin, notify customer.
            string targetUserId = isAdmin ? ticket.UserId : "USR-ADMIN-1";
            string title = isAdmin ? $"Reply to Ticket #{ticket.TicketNumber}" : $"Customer Reply on Ticket #{ticket.TicketNumber}";
            await _notificationService.SendNotificationAsync(targetUserId, title, req.Message, "Support", "Email & In-App");
        }

        return Ok(new { success = true, message = "Reply sent successfully." });
    }

    [HttpGet("live-session")]
    public async Task<IActionResult> GetLiveSession()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "GUEST";
        var activeTicket = await _ticketRepo.GetActiveLiveChatSessionAsync(userId);
        bool isAdminReady = _ticketRepo.GetAdminReadyStatus();

        if (activeTicket == null)
        {
            return Ok(new
            {
                success = true,
                hasActiveSession = false,
                isAdminReady,
                ticketId = (string?)null,
                status = "None",
                messages = new List<object>()
            });
        }

        var messages = activeTicket.GetMessages().Select(m => new
        {
            id = m.Id,
            senderRole = m.SenderRole,
            senderName = m.SenderName,
            messageText = m.MessageText,
            timestamp = m.Timestamp.ToString("HH:mm")
        });

        return Ok(new
        {
            success = true,
            hasActiveSession = true,
            isAdminReady,
            ticketId = activeTicket.Id,
            ticketNumber = activeTicket.TicketNumber,
            status = activeTicket.Status,
            messages
        });
    }

    [HttpGet("agent-status")]
    public IActionResult GetAgentStatus()
    {
        return Ok(new
        {
            success = true,
            isReady = _ticketRepo.GetAdminReadyStatus()
        });
    }

    [HttpPost("admin/ready-status")]
    public IActionResult ToggleAdminReadyStatus([FromBody] AdminReadyStatusRequest req)
    {
        _ticketRepo.SetAdminReadyStatus(req.IsReady);
        return Ok(new
        {
            success = true,
            isReady = _ticketRepo.GetAdminReadyStatus()
        });
    }

    [HttpPost("tickets/cancel")]
    public async Task<IActionResult> CancelTicket([FromBody] CancelTicketRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.TicketId))
        {
            return BadRequest(new { success = false, message = "Ticket ID is required." });
        }

        var ticket = await _ticketRepo.GetTicketByIdAsync(req.TicketId);
        if (ticket == null)
        {
            return NotFound(new { success = false, message = "Ticket not found." });
        }

        bool isAdmin = User.IsInRole("Admin");
        string role = isAdmin ? "Support Agent" : "Customer";
        string senderName = isAdmin ? "Indoria Support Agent" : (User.Identity?.Name ?? "Customer");
        string message = isAdmin 
            ? "Conversation was closed by the Support Agent. Feel free to start a new chat if you need further help!" 
            : "Conversation was ended by the customer.";

        await _ticketRepo.AddMessageAsync(req.TicketId, role, senderName, message, "Cancelled");

        // Notify user/admin
        string targetUserId = isAdmin ? ticket.UserId : "USR-ADMIN-1";
        await _notificationService.SendNotificationAsync(targetUserId, $"Live Chat Ended (#{ticket.TicketNumber})", message, "Support", "In-App");

        return Ok(new { success = true, message = "Conversation closed successfully.", status = "Cancelled" });
    }
}

public class ChatBotRequest
{
    public string Message { get; set; } = "";
}

public class CreateTicketRequest
{
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "General Support";
    public string Priority { get; set; } = "Medium";
    public string Message { get; set; } = "";
}

public class ReplyTicketRequest
{
    public string TicketId { get; set; } = "";
    public string Message { get; set; } = "";
    public string Status { get; set; } = "";
}

public class AdminReadyStatusRequest
{
    public bool IsReady { get; set; }
}

public class CancelTicketRequest
{
    public string TicketId { get; set; } = "";
}
