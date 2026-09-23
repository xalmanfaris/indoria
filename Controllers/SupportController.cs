using AuraLiving.Services.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AuraLiving.Controllers;

public class SupportController : Controller
{
    private readonly ISupportTicketRepository _ticketRepo;

    public SupportController(ISupportTicketRepository ticketRepo)
    {
        _ticketRepo = ticketRepo;
    }

    [HttpGet("account/tickets")]
    public async Task<IActionResult> CustomerTickets()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "ANONYMOUS";
        var tickets = await _ticketRepo.GetUserTicketsAsync(userId);
        return View("~/Views/Support/Index.cshtml", tickets);
    }

    [HttpGet("account/tickets/{id}")]
    public async Task<IActionResult> TicketDetail(string id)
    {
        var ticket = await _ticketRepo.GetTicketByIdAsync(id);
        if (ticket == null) return NotFound();

        return View("~/Views/Support/Detail.cshtml", ticket);
    }
}
