using System.ComponentModel.DataAnnotations;

namespace InventoryApi.DTOs;

public record ProductDto(
    Guid Id,
    string Name,
    string SKU,
    string? Description,
    decimal Price,
    decimal Cost,
    int StockQuantity,
    int ReorderPoint,
    bool IsActive,
    string? Brand,
    decimal? WeightKg,
    Guid CategoryId,
    string? CategoryName,
    bool IsLowStock,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateProductRequest(
    [Required][MaxLength(200)] string Name,
    [Required][MaxLength(100)] string SKU,
    [MaxLength(2000)] string? Description,
    [Range(0, double.MaxValue)] decimal Price,
    [Range(0, double.MaxValue)] decimal Cost,
    [Range(0, int.MaxValue)] int StockQuantity,
    [Range(0, int.MaxValue)] int ReorderPoint,
    string? Brand,
    decimal? WeightKg,
    [Required] Guid CategoryId
);

public record UpdateProductRequest(
    [Required][MaxLength(200)] string Name,
    [MaxLength(2000)] string? Description,
    [Range(0, double.MaxValue)] decimal Price,
    [Range(0, double.MaxValue)] decimal Cost,
    [Range(0, int.MaxValue)] int ReorderPoint,
    bool IsActive,
    string? Brand,
    decimal? WeightKg,
    [Required] Guid CategoryId
);

public record StockAdjustmentRequest(
    [Required] int Quantity,
    [MaxLength(500)] string? Reason
);

public record CategoryDto(
    Guid Id,
    string Name,
    string? Description,
    string? Slug,
    bool IsActive,
    int ProductCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateCategoryRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    [MaxLength(50)] string? Slug
);

public record CreateOrderRequest(
    [Required][MaxLength(200)] string CustomerName,
    [MaxLength(200)] string? CustomerEmail,
    [MaxLength(500)] string? ShippingAddress,
    [MaxLength(1000)] string? Notes,
    [Required] List<CreateOrderLineRequest> Lines
);

public record CreateOrderLineRequest(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity
);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string CustomerName,
    string? CustomerEmail,
    string Status,
    decimal TotalAmount,
    string? ShippingAddress,
    string? Notes,
    List<OrderLineDto> Lines,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
