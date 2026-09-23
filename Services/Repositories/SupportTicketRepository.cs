using AuraLiving.Services.Database;
using Dapper;
using System.Text.Json;

namespace AuraLiving.Services.Repositories;

public class TicketChatMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string SenderRole { get; set; } = "Customer"; // Customer or Support Agent
    public string SenderName { get; set; } = "";
    public string MessageText { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class SupportTicket
{
    public string Id { get; set; } = $"TCK-{Random.Shared.Next(10000, 99999)}";
    public string TicketNumber { get; set; } = "";
    public string UserId { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Category { get; set; } = "General Support";
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Urgent
    public string Status { get; set; } = "Open"; // Open, In Progress, Customer Reply, Resolved, Closed
    public string MessagesJson { get; set; } = "[]";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public List<TicketChatMessage> GetMessages()
    {
        if (string.IsNullOrEmpty(MessagesJson)) return new List<TicketChatMessage>();
        try
        {
            return JsonSerializer.Deserialize<List<TicketChatMessage>>(MessagesJson) ?? new List<TicketChatMessage>();
        }
        catch
        {
            return new List<TicketChatMessage>();
        }
    }

    public void SetMessages(List<TicketChatMessage> messages)
    {
        MessagesJson = JsonSerializer.Serialize(messages);
    }
}

public interface ISupportTicketRepository
{
    Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, string initialMessage);
    Task<List<SupportTicket>> GetUserTicketsAsync(string userId);
    Task<List<SupportTicket>> GetAllTicketsAsync();
    Task<SupportTicket?> GetTicketByIdAsync(string ticketId);
    Task<SupportTicket?> GetActiveLiveChatSessionAsync(string userId);
    Task AddMessageAsync(string ticketId, string senderRole, string senderName, string messageText, string? newStatus = null);
    Task UpdateTicketStatusAsync(string ticketId, string status);
    bool GetAdminReadyStatus();
    void SetAdminReadyStatus(bool isReady);
}

public class SupportTicketRepository : ISupportTicketRepository
{
    private readonly IDbConnectionFactory _dbFactory;
    private static bool _isAdminReadyToChat = true;

    public SupportTicketRepository(IDbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public bool GetAdminReadyStatus() => _isAdminReadyToChat;
    public void SetAdminReadyStatus(bool isReady) => _isAdminReadyToChat = isReady;

    public async Task<SupportTicket?> GetActiveLiveChatSessionAsync(string userId)
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = @"
            SELECT * FROM SupportTickets 
            WHERE UserId = @UserId AND Category = 'Live Chat'
            ORDER BY UpdatedAt DESC;";
        return await conn.QueryFirstOrDefaultAsync<SupportTicket>(sql, new { UserId = userId });
    }

    public async Task<SupportTicket> CreateTicketAsync(SupportTicket ticket, string initialMessage)
    {
        if (string.IsNullOrEmpty(ticket.Id))
        {
            ticket.Id = $"TCK-{Random.Shared.Next(10000, 99999)}";
        }
        ticket.TicketNumber = ticket.Id;
        ticket.CreatedAt = DateTime.UtcNow;
        ticket.UpdatedAt = DateTime.UtcNow;
        ticket.Status = "Open";

        var initialMsgList = new List<TicketChatMessage>
        {
            new TicketChatMessage
            {
                SenderRole = "Customer",
                SenderName = ticket.CustomerName,
                MessageText = initialMessage,
                Timestamp = DateTime.UtcNow
            }
        };
        ticket.SetMessages(initialMsgList);

        using var conn = _dbFactory.CreateConnection();
        string sql = @"
            INSERT INTO SupportTickets (Id, TicketNumber, UserId, CustomerName, Email, Subject, Category, Priority, Status, MessagesJson, CreatedAt, UpdatedAt)
            VALUES (@Id, @TicketNumber, @UserId, @CustomerName, @Email, @Subject, @Category, @Priority, @Status, @MessagesJson, @CreatedAt, @UpdatedAt);
        ";
        await conn.ExecuteAsync(sql, ticket);
        return ticket;
    }

    public async Task<List<SupportTicket>> GetUserTicketsAsync(string userId)
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = "SELECT * FROM SupportTickets WHERE UserId = @UserId ORDER BY UpdatedAt DESC;";
        var result = await conn.QueryAsync<SupportTicket>(sql, new { UserId = userId });
        return result.ToList();
    }

    public async Task<List<SupportTicket>> GetAllTicketsAsync()
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = "SELECT * FROM SupportTickets ORDER BY UpdatedAt DESC;";
        var result = await conn.QueryAsync<SupportTicket>(sql);
        return result.ToList();
    }

    public async Task<SupportTicket?> GetTicketByIdAsync(string ticketId)
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = "SELECT * FROM SupportTickets WHERE Id = @Id OR TicketNumber = @Id;";
        return await conn.QueryFirstOrDefaultAsync<SupportTicket>(sql, new { Id = ticketId });
    }

    public async Task AddMessageAsync(string ticketId, string senderRole, string senderName, string messageText, string? newStatus = null)
    {
        var ticket = await GetTicketByIdAsync(ticketId);
        if (ticket == null) return;

        var messages = ticket.GetMessages();
        messages.Add(new TicketChatMessage
        {
            SenderRole = senderRole,
            SenderName = senderName,
            MessageText = messageText,
            Timestamp = DateTime.UtcNow
        });

        ticket.SetMessages(messages);
        ticket.UpdatedAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(newStatus))
        {
            ticket.Status = newStatus;
        }
        else if (senderRole == "Customer")
        {
            ticket.Status = "Customer Reply";
        }
        else
        {
            ticket.Status = "In Progress";
        }

        using var conn = _dbFactory.CreateConnection();
        string sql = @"
            UPDATE SupportTickets 
            SET MessagesJson = @MessagesJson, Status = @Status, UpdatedAt = @UpdatedAt 
            WHERE Id = @Id;
        ";
        await conn.ExecuteAsync(sql, new { ticket.MessagesJson, ticket.Status, ticket.UpdatedAt, ticket.Id });
    }

    public async Task UpdateTicketStatusAsync(string ticketId, string status)
    {
        using var conn = _dbFactory.CreateConnection();
        string sql = "UPDATE SupportTickets SET Status = @Status, UpdatedAt = @UpdatedAt WHERE Id = @Id;";
        await conn.ExecuteAsync(sql, new { Id = ticketId, Status = status, UpdatedAt = DateTime.UtcNow });
    }
}
