using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class ProductService
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<ProductService> _logger;

    public ProductService(InventoryDbContext db, ILogger<ProductService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllAsync(bool activeOnly = true)
    {
        var query = _db.Products.Include(p => p.Category).AsQueryable();

        if (activeOnly)
            query = query.Where(p => p.IsActive);

        var products = await query.OrderBy(p => p.Name).ToListAsync();
        return products.Select(MapToDto);
    }

    public async Task<ProductDto?> GetByIdAsync(Guid id)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == id);

        return product is null ? null : MapToDto(product);
    }

    public async Task<ProductDto?> GetBySkuAsync(string sku)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.SKU == sku);

        return product is null ? null : MapToDto(product);
    }

    public async Task<IEnumerable<ProductDto>> SearchAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return await GetAllAsync();

        var lower = query.ToLower();
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && (
                p.Name.ToLower().Contains(lower) ||
                p.SKU.ToLower().Contains(lower) ||
                (p.Description != null && p.Description.ToLower().Contains(lower)) ||
                (p.Brand != null && p.Brand.ToLower().Contains(lower))
            ))
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId)
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.CategoryId == categoryId && p.IsActive)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
    {
        var products = await _db.Products
            .Include(p => p.Category)
            .Where(p => p.IsActive && p.StockQuantity <= p.ReorderPoint)
            .OrderBy(p => p.StockQuantity)
            .ToListAsync();

        return products.Select(MapToDto);
    }

    public async Task<ProductDto> CreateAsync(CreateProductRequest request)
    {
        if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
            throw new InvalidOperationException($"A product with SKU '{request.SKU}' already exists.");

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive);
        if (!categoryExists)
            throw new InvalidOperationException($"Category with ID '{request.CategoryId}' does not exist or is inactive.");

        var product = new Product
        {
            Name = request.Name,
            SKU = request.SKU,
            Description = request.Description,
            Price = request.Price,
            Cost = request.Cost,
            StockQuantity = request.StockQuantity,
            ReorderPoint = request.ReorderPoint,
            Brand = request.Brand,
            WeightKg = request.WeightKg,
            CategoryId = request.CategoryId
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created product {SKU} - {Name}", product.SKU, product.Name);

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return MapToDto(product);
    }

    public async Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return null;

        var categoryExists = await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive);
        if (!categoryExists)
            throw new InvalidOperationException($"Category with ID '{request.CategoryId}' does not exist or is inactive.");

        product.Name = request.Name;
        product.Description = request.Description;
        product.Price = request.Price;
        product.Cost = request.Cost;
        product.ReorderPoint = request.ReorderPoint;
        product.IsActive = request.IsActive;
        product.Brand = request.Brand;
        product.WeightKg = request.WeightKg;
        product.CategoryId = request.CategoryId;
        product.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated product {SKU}", product.SKU);

        await _db.Entry(product).Reference(p => p.Category).LoadAsync();
        return MapToDto(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product is null) return false;

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted product {Id}", id);
        return true;
    }

    public async Task<ProductDto?> AdjustStockAsync(Guid id, StockAdjustmentRequest request)
    {
        var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
        if (product is null) return null;

        product.UpdateStock(request.Quantity);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Stock adjusted for {SKU}: {Delta} (Reason: {Reason})", product.SKU, request.Quantity, request.Reason);
        return MapToDto(product);
    }

    private static ProductDto MapToDto(Product p) => new(
        p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
        p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
        p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
        p.CreatedAt, p.UpdatedAt
    );
}
