using AuraLiving.Services;
using Microsoft.AspNetCore.Mvc;

namespace AuraLiving.Controllers;

[ApiController]
[Route("api/instagram")]
public class InstagramApiController : ControllerBase
{
    private readonly IInstagramService _instagramService;
    private readonly ILogger<InstagramApiController> _logger;

    public InstagramApiController(IInstagramService instagramService, ILogger<InstagramApiController> logger)
    {
        _instagramService = instagramService;
        _logger = logger;
    }

    public class TrackClickRequest
    {
        public string PostId { get; set; } = string.Empty;
        public string Permalink { get; set; } = string.Empty;
    }

    [HttpPost("track-click")]
    public async Task<IActionResult> TrackClick([FromBody] TrackClickRequest request)
    {
        if (request == null || string.IsNullOrEmpty(request.PostId))
        {
            return BadRequest(new { success = false, message = "PostId is required." });
        }

        var userAgent = Request.Headers["User-Agent"].ToString();
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        await _instagramService.TrackClickAsync(request.PostId, request.Permalink, userAgent, ipAddress);

        return Ok(new { success = true });
    }

    [HttpGet("analytics")]
    public async Task<IActionResult> GetAnalytics()
    {
        var summary = await _instagramService.GetAnalyticsSummaryAsync();
        return Ok(summary);
    }
}
