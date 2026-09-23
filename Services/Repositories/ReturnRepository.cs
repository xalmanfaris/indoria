using System.Text.Json;
using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IReturnRepository
{
    Task<string> CreateReturnRequestAsync(ReturnRequestViewModel model, string userId, string customerName = "", string customerEmail = "");
    Task<List<ReturnRequestViewModel>> GetReturnRequestsByUserIdAsync(string userId);
    Task<List<ReturnRequestViewModel>> GetAllReturnRequestsAsync();
    Task<ReturnRequestViewModel?> GetReturnRequestByIdAsync(string id);
    Task<bool> UpdateReturnStatusAsync(string id, string status, string? adminNotes = null);
}

public class ReturnRepository : IReturnRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IOrderRepository _orderRepository;
    private readonly INotificationService _notificationService;
    private readonly IProductRepository _productRepository;

    public ReturnRepository(
        IDbConnectionFactory connectionFactory, 
        IOrderRepository orderRepository, 
        INotificationService notificationService,
        IProductRepository productRepository)
    {
        _connectionFactory = connectionFactory;
        _orderRepository = orderRepository;
        _notificationService = notificationService;
        _productRepository = productRepository;
    }

    public async Task<string> CreateReturnRequestAsync(ReturnRequestViewModel model, string userId, string customerName = "", string customerEmail = "")
    {
        if (string.IsNullOrEmpty(model.Id))
        {
            model.Id = "RET-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Random.Shared.Next(1000, 9999);
        }

        if (model.RequestDate == default)
        {
            model.RequestDate = DateTime.Now;
        }

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO ReturnRequests (
                    Id, UserId, CustomerName, CustomerEmail, OrderId, ProductId, ProductName, ProductImage,
                    Type, Reason, Description, Status, RequestDate, RefundAmount, AdminNotes
                )
                VALUES (
                    @Id, @UserId, @CustomerName, @CustomerEmail, @OrderId, @ProductId, @ProductName, @ProductImage,
                    @Type, @Reason, @Description, @Status, @RequestDate, @RefundAmount, @AdminNotes
                );";

            await connection.ExecuteAsync(sql, new
            {
                Id = model.Id,
                UserId = userId ?? model.UserId ?? "",
                CustomerName = !string.IsNullOrEmpty(customerName) ? customerName : model.CustomerName,
                CustomerEmail = !string.IsNullOrEmpty(customerEmail) ? customerEmail : model.CustomerEmail,
                OrderId = model.OrderId ?? "",
                ProductId = model.ProductId ?? "",
                ProductName = model.ProductName ?? "Appliance Product",
                ProductImage = model.ProductImage ?? "",
                Type = string.IsNullOrEmpty(model.Type) ? "Return" : model.Type,
                Reason = model.Reason ?? "Customer request",
                Description = model.Description ?? "",
                Status = string.IsNullOrEmpty(model.Status) ? "Under Review" : model.Status,
                RequestDate = model.RequestDate,
                RefundAmount = model.RefundAmount,
                AdminNotes = model.AdminNotes ?? ""
            });

            // Update associated order status if needed
            if (!string.IsNullOrEmpty(model.OrderId))
            {
                string newOrderStatus = model.Type == "Replacement" ? "Replacement Requested" : "Return Requested";
                await _orderRepository.UpdateOrderStatusAsync(model.OrderId, newOrderStatus);
            }

            // Notify Customer
            await _notificationService.SendNotificationAsync(
                userId ?? "ANONYMOUS",
                $"{model.Type} Request Submitted (#{model.Id})",
                $"Your {model.Type.ToLower()} request for Order #{model.OrderId} has been received and is under executive review.",
                "Return",
                "Email & In-App"
            );

            // Notify Admin
            await _notificationService.SendNotificationAsync(
                "USR-ADMIN-1",
                $"New {model.Type} Request #{model.Id}",
                $"Customer requested {model.Type.ToLower()} for Order #{model.OrderId}. Reason: '{model.Reason}'.",
                "AdminAlert",
                "In-App"
            );

            return model.Id;
        }
        catch
        {
            return model.Id;
        }
    }

    public async Task<List<ReturnRequestViewModel>> GetReturnRequestsByUserIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<ReturnRequestViewModel>();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM ReturnRequests WHERE UserId = @UserId ORDER BY RequestDate DESC;";
            var rows = await connection.QueryAsync<ReturnRequestEntityRow>(sql, new { UserId = userId });
            return rows.Select(MapRowToViewModel).ToList();
        }
        catch
        {
            return new List<ReturnRequestViewModel>();
        }
    }

    public async Task<List<ReturnRequestViewModel>> GetAllReturnRequestsAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM ReturnRequests ORDER BY RequestDate DESC;";
            var rows = await connection.QueryAsync<ReturnRequestEntityRow>(sql);
            return rows.Select(MapRowToViewModel).ToList();
        }
        catch
        {
            return new List<ReturnRequestViewModel>();
        }
    }

    public async Task<ReturnRequestViewModel?> GetReturnRequestByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM ReturnRequests WHERE Id = @Id;";
            var raw = await connection.QueryFirstOrDefaultAsync<ReturnRequestEntityRow>(sql, new { Id = id });
            return raw != null ? MapRowToViewModel(raw) : null;
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> UpdateReturnStatusAsync(string id, string status, string? adminNotes = null)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var existing = await GetReturnRequestByIdAsync(id);
            if (existing == null) return false;

            bool wasNotApproved = !existing.Status.Equals("Approved", StringComparison.OrdinalIgnoreCase) &&
                                  !existing.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase) &&
                                  !existing.Status.Equals("Refunded", StringComparison.OrdinalIgnoreCase) &&
                                  !existing.Status.Equals("Refund Initiated", StringComparison.OrdinalIgnoreCase);

            bool isNowApproved = status.Equals("Approved", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Completed", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Refunded", StringComparison.OrdinalIgnoreCase) ||
                                 status.Equals("Refund Initiated", StringComparison.OrdinalIgnoreCase);

            string sql = @"
                UPDATE ReturnRequests 
                SET Status = @Status, 
                    AdminNotes = ISNULL(@AdminNotes, AdminNotes) 
                WHERE Id = @Id;";

            int affected = await connection.ExecuteAsync(sql, new { Id = id, Status = status, AdminNotes = adminNotes });

            if (affected > 0)
            {
                if (wasNotApproved && isNowApproved)
                {
                    if (!string.IsNullOrEmpty(existing.ProductId))
                    {
                        await _productRepository.AdjustStockAsync(existing.ProductId, 1);
                    }
                    else if (!string.IsNullOrEmpty(existing.OrderId))
                    {
                        var assocOrder = await _orderRepository.GetOrderByIdAsync(existing.OrderId);
                        if (assocOrder?.Items != null && assocOrder.Items.Any())
                        {
                            var targetItem = assocOrder.Items.FirstOrDefault(i => i.ProductName.Equals(existing.ProductName, StringComparison.OrdinalIgnoreCase)) ?? assocOrder.Items.First();
                            await _productRepository.AdjustStockAsync(targetItem.ProductId, targetItem.Quantity > 0 ? targetItem.Quantity : 1);
                        }
                    }
                }

                if (!string.IsNullOrEmpty(existing.UserId))
                {
                    await _notificationService.SendNotificationAsync(
                        existing.UserId,
                        $"{existing.Type} Request Status Updated",
                        $"Your {existing.Type.ToLower()} request (#{id}) for Order #{existing.OrderId} is now '{status}'.",
                        "Return",
                        "Email & In-App"
                    );
                }
            }

            return affected > 0;
        }
        catch
        {
            return false;
        }
    }

    private static ReturnRequestViewModel MapRowToViewModel(ReturnRequestEntityRow row)
    {
        return new ReturnRequestViewModel
        {
            Id = row.Id,
            UserId = row.UserId ?? "",
            CustomerName = row.CustomerName ?? "",
            CustomerEmail = row.CustomerEmail ?? "",
            OrderId = row.OrderId ?? "",
            ProductId = row.ProductId ?? "",
            ProductName = row.ProductName ?? "Appliance Product",
            ProductImage = row.ProductImage ?? "",
            Type = row.Type ?? "Return",
            Reason = row.Reason ?? "",
            Description = row.Description ?? "",
            Status = row.Status ?? "Under Review",
            RequestDate = row.RequestDate,
            RefundAmount = row.RefundAmount,
            AdminNotes = row.AdminNotes ?? ""
        };
    }

    private class ReturnRequestEntityRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string OrderId { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public decimal RefundAmount { get; set; }
        public string AdminNotes { get; set; } = string.Empty;
    }
}
