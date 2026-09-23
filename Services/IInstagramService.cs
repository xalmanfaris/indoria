using AuraLiving.Models;

namespace AuraLiving.Services;

public interface IInstagramService
{
    Task<InstagramFeedViewModel> GetFeedAsync();
    Task RefreshFeedCacheAsync();
    Task TrackClickAsync(string postId, string permalink, string? userAgent, string? ipAddress);
    Task<InstagramAnalyticsSummary> GetAnalyticsSummaryAsync();
}
