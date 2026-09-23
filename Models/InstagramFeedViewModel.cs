namespace AuraLiving.Models;

public class InstagramFeedViewModel
{
    public string Username { get; set; } = "ebsor_infosystems";
    public string ProfileUrl { get; set; } = "https://www.instagram.com/ebsor_infosystems/";
    public string ProfilePicUrl { get; set; } = "/images/homixa_hero_appliances.jpg";
    public string FollowersCount { get; set; } = "28.4K";
    public int PostsCount { get; set; } = 142;
    public string Bio { get; set; } = "Official Instagram • Luxury Modern Home Appliances & Smart Tech Solutions";
    public List<InstagramPost> Posts { get; set; } = new();
    public bool HasError { get; set; } = false;
    public string? ErrorMessage { get; set; }
    public DateTime LastRefreshedAt { get; set; } = DateTime.UtcNow;
}
