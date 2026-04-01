using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

public enum OrderStatus
{
    Pending,
    Confirmed,
    Processing,
    Shipped,
    Delivered,
    Cancelled,
    Refunded
}

public class Order
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string OrderNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string CustomerName { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? CustomerEmail { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(500)]
    public string? ShippingAddress { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderLine> Lines { get; set; } = new List<OrderLine>();

    public void RecalculateTotal()
    {
        TotalAmount = Lines.Sum(l => l.LineTotal);
        UpdatedAt = DateTime.UtcNow;
    }
}

public class OrderLine
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OrderId { get; set; }

    [ForeignKey(nameof(OrderId))]
    public Order? Order { get; set; }

    public Guid ProductId { get; set; }

    [ForeignKey(nameof(ProductId))]
    public Product? Product { get; set; }

    [Required]
    [MaxLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ProductSKU { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal => Quantity * UnitPrice;
}
