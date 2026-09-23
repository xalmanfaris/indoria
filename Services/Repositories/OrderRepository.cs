using System.Text.Json;
using AuraLiving.Models;
using AuraLiving.Services.Database;
using Dapper;

namespace AuraLiving.Services.Repositories;

public interface IOrderRepository
{
    Task<string> CreateOrderAsync(OrderViewModel order, string userId, string customerEmail = "", string customerName = "");
    Task<OrderViewModel?> GetOrderByIdAsync(string id);
    Task<List<OrderViewModel>> GetOrdersByUserIdAsync(string userId);
    Task<List<OrderViewModel>> GetAllOrdersAsync();
    Task<bool> UpdateOrderStatusAsync(string id, string status);
    Task<bool> CancelOrderAsync(string orderId, string reason, string userId = "");
}

public class OrderRepository : IOrderRepository
{
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly ICartRepository _cartRepository;
    private readonly INotificationService _notificationService;
    private readonly IProductRepository _productRepository;

    public OrderRepository(
        IDbConnectionFactory connectionFactory, 
        ICartRepository cartRepository, 
        INotificationService notificationService,
        IProductRepository productRepository)
    {
        _connectionFactory = connectionFactory;
        _cartRepository = cartRepository;
        _notificationService = notificationService;
        _productRepository = productRepository;
    }

    public async Task<string> CreateOrderAsync(OrderViewModel order, string userId, string customerEmail = "", string customerName = "")
    {
        if (string.IsNullOrEmpty(order.Id))
        {
            order.Id = "ORD-" + DateTime.Now.ToString("yyyyMMdd") + "-" + Random.Shared.Next(1000, 9999);
        }

        if (order.OrderDate == default)
        {
            order.OrderDate = DateTime.Now;
        }

        if (string.IsNullOrWhiteSpace(order.EstimatedDelivery))
        {
            order.EstimatedDelivery = DateTime.Now.AddDays(2).ToString("dddd, MMM dd");
        }

        if (order.Timeline == null || !order.Timeline.Any())
        {
            order.Timeline = new List<OrderTimelineStep>
            {
                new() { Title = "Order Confirmed", Description = "Your order has been placed and payment confirmed.", Timestamp = order.OrderDate.ToString("MMM dd, yyyy - hh:mm tt"), IsCompleted = true, IsCurrent = false },
                new() { Title = "Processing & Quality Check", Description = "Appliance undergoes pre-dispatch multi-point check.", Timestamp = order.OrderDate.AddHours(2).ToString("MMM dd, yyyy - hh:mm tt"), IsCompleted = true, IsCurrent = true },
                new() { Title = "Out for White-Glove Express Delivery", Description = "Dispatched via specialized appliance logistics.", Timestamp = "Scheduled for " + order.EstimatedDelivery, IsCompleted = false, IsCurrent = false },
                new() { Title = "Delivered & Technician Installation", Description = "Unboxed, installed, and tested by certified team.", Timestamp = "Pending", IsCompleted = false, IsCurrent = false }
            };
        }

        string itemsJson = JsonSerializer.Serialize(order.Items ?? new List<CartItemViewModel>());
        string shippingAddressJson = JsonSerializer.Serialize(order.ShippingAddress ?? new AddressViewModel());
        string timelineJson = JsonSerializer.Serialize(order.Timeline);

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = @"
                INSERT INTO Orders (
                    Id, UserId, OrderDate, Status, EstimatedDelivery, CustomerEmail, CustomerName,
                    ShippingAddressJson, PaymentMethod, SubTotal, Discount, GrandTotal, ItemsJson, TimelineJson
                )
                VALUES (
                    @Id, @UserId, @OrderDate, @Status, @EstimatedDelivery, @CustomerEmail, @CustomerName,
                    @ShippingAddressJson, @PaymentMethod, @SubTotal, @Discount, @GrandTotal, @ItemsJson, @TimelineJson
                );";

            await connection.ExecuteAsync(sql, new
            {
                Id = order.Id,
                UserId = userId ?? "",
                OrderDate = order.OrderDate,
                Status = string.IsNullOrEmpty(order.Status) ? "Confirmed" : order.Status,
                EstimatedDelivery = order.EstimatedDelivery,
                CustomerEmail = customerEmail ?? order.ShippingAddress?.Email ?? "",
                CustomerName = customerName ?? order.ShippingAddress?.FullName ?? "",
                ShippingAddressJson = shippingAddressJson,
                PaymentMethod = order.PaymentMethod ?? "UPI",
                SubTotal = order.SubTotal,
                Discount = order.Discount,
                GrandTotal = order.GrandTotal > 0 ? order.GrandTotal : order.SubTotal,
                ItemsJson = itemsJson,
                TimelineJson = timelineJson
            });

            // Clear the user's shopping cart after order is successfully recorded in DB
            if (!string.IsNullOrEmpty(userId))
            {
                await _cartRepository.ClearCartAsync(userId);
            }

            // Real-time stock reduction for ordered items
            if (order.Items != null && order.Items.Any())
            {
                foreach (var item in order.Items)
                {
                    int qty = item.Quantity > 0 ? item.Quantity : 1;
                    await _productRepository.AdjustStockAsync(item.ProductId, -qty);
                }
            }

            // Send Email / SMS / In-App Notification
            await _notificationService.SendNotificationAsync(
                userId ?? "ANONYMOUS",
                $"Order #{order.Id} Confirmed!",
                $"Thank you for your order! Total amount: ${order.GrandTotal:N2}. Estimated delivery: {order.EstimatedDelivery}.",
                "Order",
                "Email & SMS & In-App"
            );

            return order.Id;
        }
        catch
        {
            return order.Id;
        }
    }

    public async Task<OrderViewModel?> GetOrderByIdAsync(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Orders WHERE Id = @Id;";
            var raw = await connection.QueryFirstOrDefaultAsync<OrderEntityRow>(sql, new { Id = id });

            if (raw == null) return null;

            return MapRowToViewModel(raw);
        }
        catch
        {
            return null;
        }
    }

    public async Task<List<OrderViewModel>> GetOrdersByUserIdAsync(string userId)
    {
        if (string.IsNullOrEmpty(userId)) return new List<OrderViewModel>();

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Orders WHERE UserId = @UserId ORDER BY OrderDate DESC;";
            var rows = await connection.QueryAsync<OrderEntityRow>(sql, new { UserId = userId });

            return rows.Select(MapRowToViewModel).ToList();
        }
        catch
        {
            return new List<OrderViewModel>();
        }
    }

    public async Task<List<OrderViewModel>> GetAllOrdersAsync()
    {
        try
        {
            using var connection = _connectionFactory.CreateConnection();
            string sql = "SELECT * FROM Orders ORDER BY OrderDate DESC;";
            var rows = await connection.QueryAsync<OrderEntityRow>(sql);

            return rows.Select(MapRowToViewModel).ToList();
        }
        catch
        {
            return new List<OrderViewModel>();
        }
    }

    public async Task<bool> UpdateOrderStatusAsync(string id, string status)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(status)) return false;

        try
        {
            var existingOrder = await GetOrderByIdAsync(id);
            if (existingOrder == null) return false;

            var updatedTimeline = SynchronizeTimelineSteps(existingOrder.Timeline, status, existingOrder.OrderDate, existingOrder.EstimatedDelivery, existingOrder.CancellationReason);
            string timelineJson = JsonSerializer.Serialize(updatedTimeline);

            using var connection = _connectionFactory.CreateConnection();
            string sql = "UPDATE Orders SET Status = @Status, TimelineJson = @TimelineJson WHERE Id = @Id;";
            int affected = await connection.ExecuteAsync(sql, new { Id = id, Status = status, TimelineJson = timelineJson });

            if (affected > 0)
            {
                // Restore stock if transitioning to Cancelled or Refunded from an active state
                bool wasActive = !existingOrder.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) && 
                                 !existingOrder.Status.Equals("Refunded", StringComparison.OrdinalIgnoreCase);
                bool isNowCancelledOrRefunded = status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase) || 
                                                status.Equals("Refunded", StringComparison.OrdinalIgnoreCase);

                if (wasActive && isNowCancelledOrRefunded && existingOrder.Items != null)
                {
                    foreach (var item in existingOrder.Items)
                    {
                        int qty = item.Quantity > 0 ? item.Quantity : 1;
                        await _productRepository.AdjustStockAsync(item.ProductId, qty);
                    }
                }

                if (!string.IsNullOrEmpty(existingOrder.UserId))
                {
                    await _notificationService.SendNotificationAsync(
                        existingOrder.UserId,
                        $"Order #{id} Status Updated",
                        $"Your order status has been updated to '{status}'.",
                        "Order",
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

    public async Task<bool> CancelOrderAsync(string orderId, string reason, string userId = "")
    {
        if (string.IsNullOrEmpty(orderId)) return false;

        try
        {
            using var connection = _connectionFactory.CreateConnection();
            var order = await GetOrderByIdAsync(orderId);
            if (order == null) return false;

            if (order.Status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            var timeline = order.Timeline ?? new List<OrderTimelineStep>();
            foreach (var step in timeline) { step.IsCurrent = false; }
            timeline.Add(new OrderTimelineStep
            {
                Title = "Order Cancelled",
                Description = $"Cancelled by customer. Reason: {reason}",
                Timestamp = DateTime.Now.ToString("MMM dd, yyyy - hh:mm tt"),
                IsCompleted = true,
                IsCurrent = true
            });
            string timelineJson = JsonSerializer.Serialize(timeline);

            string sql = @"
                UPDATE Orders 
                SET Status = 'Cancelled', 
                    CancellationReason = @Reason, 
                    CancellationDate = @CancellationDate,
                    TimelineJson = @TimelineJson 
                WHERE Id = @Id;";

            int affected = await connection.ExecuteAsync(sql, new 
            { 
                Id = orderId, 
                Reason = reason, 
                CancellationDate = DateTime.Now,
                TimelineJson = timelineJson
            });

            if (affected > 0)
            {
                // Restore stock count for items in cancelled order
                if (order.Items != null)
                {
                    foreach (var item in order.Items)
                    {
                        int qty = item.Quantity > 0 ? item.Quantity : 1;
                        await _productRepository.AdjustStockAsync(item.ProductId, qty);
                    }
                }

                await _notificationService.SendNotificationAsync(
                    !string.IsNullOrEmpty(order.UserId) ? order.UserId : userId,
                    $"Order #{orderId} Cancelled",
                    $"Your order #{orderId} has been successfully cancelled. Reason: '{reason}'. If paid online, your refund will be processed within 3-5 business days.",
                    "Order",
                    "Email & In-App"
                );
            }

            return affected > 0;
        }
        catch
        {
            return false;
        }
    }

    private static OrderViewModel MapRowToViewModel(OrderEntityRow row)
    {
        var vm = new OrderViewModel
        {
            Id = row.Id,
            UserId = row.UserId ?? "",
            CustomerEmail = row.CustomerEmail ?? "",
            CustomerName = row.CustomerName ?? "",
            OrderDate = row.OrderDate,
            Status = row.Status ?? "Confirmed",
            EstimatedDelivery = row.EstimatedDelivery ?? "",
            PaymentMethod = row.PaymentMethod ?? "UPI",
            SubTotal = row.SubTotal,
            Discount = row.Discount,
            GrandTotal = row.GrandTotal,
            CancellationReason = row.CancellationReason,
            CancellationDate = row.CancellationDate
        };

        if (!string.IsNullOrEmpty(row.ItemsJson))
        {
            try { vm.Items = JsonSerializer.Deserialize<List<CartItemViewModel>>(row.ItemsJson) ?? new(); } catch { }
        }

        if (!string.IsNullOrEmpty(row.ShippingAddressJson))
        {
            try { vm.ShippingAddress = JsonSerializer.Deserialize<AddressViewModel>(row.ShippingAddressJson) ?? new(); } catch { }
        }

        if (!string.IsNullOrEmpty(row.TimelineJson))
        {
            try { vm.Timeline = JsonSerializer.Deserialize<List<OrderTimelineStep>>(row.TimelineJson) ?? new(); } catch { }
        }

        vm.Timeline = SynchronizeTimelineSteps(vm.Timeline, vm.Status, vm.OrderDate, vm.EstimatedDelivery, vm.CancellationReason);

        return vm;
    }

    public static List<OrderTimelineStep> SynchronizeTimelineSteps(List<OrderTimelineStep>? existing, string status, DateTime orderDate, string estimatedDelivery, string? cancellationReason = null)
    {
        var dateStr = orderDate.ToString("MMM dd, yyyy - hh:mm tt");
        var procTime = orderDate.AddHours(2).ToString("MMM dd, yyyy - hh:mm tt");
        var nowStr = DateTime.Now.ToString("MMM dd, yyyy - hh:mm tt");
        var estDelivery = string.IsNullOrWhiteSpace(estimatedDelivery) ? "Next 24-48 Hours" : estimatedDelivery;

        if (status.Equals("Cancelled", StringComparison.OrdinalIgnoreCase))
        {
            var steps = existing != null && existing.Any() ? existing : new List<OrderTimelineStep>
            {
                new() { Title = "Order Confirmed", Description = "Your order has been placed and payment confirmed.", Timestamp = dateStr, IsCompleted = true, IsCurrent = false }
            };
            foreach (var step in steps) { step.IsCurrent = false; }
            var cancelStep = steps.FirstOrDefault(s => s.Title.Contains("Cancelled", StringComparison.OrdinalIgnoreCase));
            if (cancelStep == null)
            {
                steps.Add(new OrderTimelineStep
                {
                    Title = "Order Cancelled",
                    Description = $"Order cancelled. Reason: {(string.IsNullOrWhiteSpace(cancellationReason) ? "Customer request" : cancellationReason)}",
                    Timestamp = nowStr,
                    IsCompleted = true,
                    IsCurrent = true
                });
            }
            else
            {
                cancelStep.IsCompleted = true;
                cancelStep.IsCurrent = true;
            }
            return steps;
        }

        if (status.Equals("Shipped", StringComparison.OrdinalIgnoreCase) || status.Equals("In Transit", StringComparison.OrdinalIgnoreCase))
        {
            return new List<OrderTimelineStep>
            {
                new() { Title = "Order Confirmed", Description = "Your order has been placed and payment confirmed.", Timestamp = dateStr, IsCompleted = true, IsCurrent = false },
                new() { Title = "Processing & Quality Check", Description = "Appliance undergoes pre-dispatch multi-point check.", Timestamp = procTime, IsCompleted = true, IsCurrent = false },
                new() { Title = "Out for White-Glove Express Delivery", Description = "Dispatched via specialized appliance logistics.", Timestamp = nowStr, IsCompleted = true, IsCurrent = true },
                new() { Title = "Delivered & Technician Installation", Description = "Unboxed, installed, and tested by certified team.", Timestamp = "Scheduled for " + estDelivery, IsCompleted = false, IsCurrent = false }
            };
        }

        if (status.Equals("Delivered", StringComparison.OrdinalIgnoreCase) || status.Equals("Completed", StringComparison.OrdinalIgnoreCase))
        {
            return new List<OrderTimelineStep>
            {
                new() { Title = "Order Confirmed", Description = "Your order has been placed and payment confirmed.", Timestamp = dateStr, IsCompleted = true, IsCurrent = false },
                new() { Title = "Processing & Quality Check", Description = "Appliance undergoes pre-dispatch multi-point check.", Timestamp = procTime, IsCompleted = true, IsCurrent = false },
                new() { Title = "Out for White-Glove Express Delivery", Description = "Dispatched via specialized appliance logistics.", Timestamp = orderDate.AddDays(1).ToString("MMM dd, yyyy - hh:mm tt"), IsCompleted = true, IsCurrent = false },
                new() { Title = "Delivered & Technician Installation", Description = "Unboxed, installed, and tested by certified team.", Timestamp = nowStr, IsCompleted = true, IsCurrent = true }
            };
        }

        // Default: Processing / Confirmed
        return new List<OrderTimelineStep>
        {
            new() { Title = "Order Confirmed", Description = "Your order has been placed and payment confirmed.", Timestamp = dateStr, IsCompleted = true, IsCurrent = false },
            new() { Title = "Processing & Quality Check", Description = "Appliance undergoes pre-dispatch multi-point check.", Timestamp = procTime, IsCompleted = true, IsCurrent = true },
            new() { Title = "Out for White-Glove Express Delivery", Description = "Dispatched via specialized appliance logistics.", Timestamp = "Scheduled for " + estDelivery, IsCompleted = false, IsCurrent = false },
            new() { Title = "Delivered & Technician Installation", Description = "Unboxed, installed, and tested by certified team.", Timestamp = "Pending", IsCompleted = false, IsCurrent = false }
        };
    }

    private class OrderEntityRow
    {
        public string Id { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public string EstimatedDelivery { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ShippingAddressJson { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal Discount { get; set; }
        public decimal GrandTotal { get; set; }
        public string ItemsJson { get; set; } = string.Empty;
        public string TimelineJson { get; set; } = string.Empty;
        public string? CancellationReason { get; set; }
        public DateTime? CancellationDate { get; set; }
    }
}
