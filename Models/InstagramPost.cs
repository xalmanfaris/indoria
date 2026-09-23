namespace AuraLiving.Models;

public class InstagramPost
{
    public string Id { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public string Permalink { get; set; } = string.Empty;
    public string EmbedUrl => !string.IsNullOrEmpty(Permalink) 
        ? (Permalink.EndsWith("/") ? $"{Permalink}embed/" : $"{Permalink}/embed/")
        : "https://www.instagram.com/ebsor_infosystems/embed/";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public bool IsReel { get; set; } = false;
    public int LikesCount { get; set; } = 0;
    public int CommentsCount { get; set; } = 0;
    public int ClickCount { get; set; } = 0;
    public string FormattedDate => Timestamp.ToString("MMM dd, yyyy");
    public string RelativeTimeString
    {
        get
        {
            var span = DateTime.UtcNow - Timestamp;
            if (span.TotalDays >= 30) return FormattedDate;
            if (span.TotalDays >= 1) return $"{(int)span.TotalDays}d ago";
            if (span.TotalHours >= 1) return $"{(int)span.TotalHours}h ago";
            if (span.TotalMinutes >= 1) return $"{(int)span.TotalMinutes}m ago";
            return "Just now";
        }
    }
}
