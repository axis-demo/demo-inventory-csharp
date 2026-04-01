using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InventoryApi.Models;

public class Product
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string SKU { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    [Range(0, double.MaxValue)]
    public decimal Cost { get; set; }

    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int ReorderPoint { get; set; } = 10;

    public bool IsActive { get; set; } = true;

    [MaxLength(100)]
    public string? Brand { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal? WeightKg { get; set; }

    public Guid CategoryId { get; set; }

    [ForeignKey(nameof(CategoryId))]
    public Category? Category { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<OrderLine> OrderLines { get; set; } = new List<OrderLine>();

    public bool IsLowStock => StockQuantity <= ReorderPoint;

    public void UpdateStock(int quantity)
    {
        if (StockQuantity + quantity < 0)
            throw new InvalidOperationException($"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}");

        StockQuantity += quantity;
        UpdatedAt = DateTime.UtcNow;
    }
}
