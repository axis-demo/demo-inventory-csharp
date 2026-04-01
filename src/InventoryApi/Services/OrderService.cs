using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class OrderService
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<OrderService> _logger;
    private static int _orderCounter = 1000;

    public OrderService(InventoryDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<OrderDto>> GetAllAsync()
    {
        var orders = await _db.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();

        return orders.Select(MapToDto);
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await _db.Orders
            .Include(o => o.Lines)
            .ThenInclude(l => l.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        if (!request.Lines.Any())
            throw new InvalidOperationException("Order must have at least one line item.");

        // Validate and fetch all products upfront
        var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Where(p => productIds.Contains(p.Id) && p.IsActive)
            .ToListAsync();

        if (products.Count != productIds.Count)
            throw new InvalidOperationException("One or more products not found or inactive.");

        // Check stock availability
        foreach (var lineReq in request.Lines)
        {
            var product = products.First(p => p.Id == lineReq.ProductId);
            if (product.StockQuantity < lineReq.Quantity)
                throw new InvalidOperationException($"Insufficient stock for '{product.Name}' (SKU: {product.SKU}). Available: {product.StockQuantity}, Requested: {lineReq.Quantity}");
        }

        // Deduct stock and build order
        var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
        var order = new Order
        {
            OrderNumber = orderNumber,
            CustomerName = request.CustomerName,
            CustomerEmail = request.CustomerEmail,
            ShippingAddress = request.ShippingAddress,
            Notes = request.Notes,
            Status = OrderStatus.Confirmed
        };

        foreach (var lineReq in request.Lines)
        {
            var product = products.First(p => p.Id == lineReq.ProductId);
            product.UpdateStock(-lineReq.Quantity);

            order.Lines.Add(new OrderLine
            {
                ProductId = product.Id,
                ProductName = product.Name,
                ProductSKU = product.SKU,
                Quantity = lineReq.Quantity,
                UnitPrice = product.Price
            });
        }

        order.RecalculateTotal();

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} created for customer {Customer}, total {Total:C}",
            order.OrderNumber, order.CustomerName, order.TotalAmount);

        return MapToDto(order);
    }

    public async Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status)
    {
        var order = await _db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return null;

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation("Order {OrderNumber} status updated to {Status}", order.OrderNumber, status);

        return MapToDto(order);
    }

    private static OrderDto MapToDto(Order o) => new(
        o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
        o.Status.ToString(), o.TotalAmount, o.ShippingAddress, o.Notes,
        o.Lines.Select(l => new OrderLineDto(l.Id, l.ProductId, l.ProductName, l.ProductSKU, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
        o.CreatedAt, o.UpdatedAt
    );
}
