namespace AuraLiving.Models;

public class InstagramClickAnalytics
{
    public int Id { get; set; }
    public string PostId { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string? IpAddress { get; set; }
    public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
}

public class InstagramAnalyticsSummary
{
    public int TotalClicks { get; set; }
    public int UniquePostsClicked { get; set; }
    public List<TopClickedPostSummary> TopClickedPosts { get; set; } = new();
}

public class TopClickedPostSummary
{
    public string PostId { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public int ClickCount { get; set; }
}
