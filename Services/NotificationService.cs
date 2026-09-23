using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services;

public class CustomerNotification
{
    public string Id { get; set; } = $"NTF-{Guid.NewGuid().ToString("N")[..8]}";
    public string UserId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string Type { get; set; } = "Order"; // Order, System, Promo, Support
    public string SentVia { get; set; } = "Email & In-App"; // Email, SMS, In-App
    public bool IsRead { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public interface INotificationService
{
    Task SendNotificationAsync(string userId, string title, string message, string type = "Order", string sentVia = "Email & In-App");
    Task<List<CustomerNotification>> GetUserNotificationsAsync(string userId);
    Task<int> GetUnreadCountAsync(string userId);
    Task MarkAsReadAsync(string notificationId);
    Task MarkAllAsReadAsync(string userId);
}

public class NotificationService : INotificationService
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IDbConnectionFactory dbFactory, ILogger<NotificationService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task SendNotificationAsync(string userId, string title, string message, string type = "Order", string sentVia = "Email & In-App")
    {
        try
        {
            var notification = new CustomerNotification
            {
                Id = $"NTF-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}",
                UserId = userId ?? "ANONYMOUS",
                Title = title,
                Message = message,
                Type = type,
                SentVia = sentVia,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            using var conn = _dbFactory.CreateConnection();
            string sql = @"
                INSERT INTO CustomerNotifications (Id, UserId, Title, Message, Type, SentVia, IsRead, CreatedAt)
                VALUES (@Id, @UserId, @Title, @Message, @Type, @SentVia, @IsRead, @CreatedAt);
            ";
            await conn.ExecuteAsync(sql, notification);

            _logger.LogInformation("[SIMULATED EMAIL/SMS DISPATCH] Sent '{Type}' Notification to User '{UserId}' via '{SentVia}': Title: '{Title}'", type, userId, sentVia, title);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
        }
    }

    public async Task<List<CustomerNotification>> GetUserNotificationsAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<CustomerNotification>();

        using var conn = _dbFactory.CreateConnection();
        string sql = @"
            SELECT TOP 20 * FROM CustomerNotifications 
            WHERE UserId = @UserId 
            ORDER BY CreatedAt DESC;
        ";
        var result = await conn.QueryAsync<CustomerNotification>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<int> GetUnreadCountAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return 0;

        using var conn = _dbFactory.CreateConnection();
        string sql = "SELECT COUNT(*) FROM CustomerNotifications WHERE UserId = @UserId AND IsRead = 0;";
        return await conn.ExecuteScalarAsync<int>(sql, new { UserId = userId });
    }

    public async Task MarkAsReadAsync(string notificationId)
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = "UPDATE CustomerNotifications SET IsRead = 1 WHERE Id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = notificationId });
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return;

        using var conn = _dbFactory.CreateConnection();
        string sql = "UPDATE CustomerNotifications SET IsRead = 1 WHERE UserId = @UserId;";
        await conn.ExecuteAsync(sql, new { UserId = userId });
    }
}
