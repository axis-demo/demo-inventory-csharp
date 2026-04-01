using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Services;

public class CategoryService
{
    private readonly InventoryDbContext _db;
    private readonly ILogger<CategoryService> _logger;

    public CategoryService(InventoryDbContext db, ILogger<CategoryService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<CategoryDto>> GetAllAsync()
    {
        var categories = await _db.Categories
            .Include(c => c.Products)
            .OrderBy(c => c.Name)
            .ToListAsync();

        return categories.Select(MapToDto);
    }

    public async Task<CategoryDto?> GetByIdAsync(Guid id)
    {
        var category = await _db.Categories
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id);

        return category is null ? null : MapToDto(category);
    }

    public async Task<CategoryDto> CreateAsync(CreateCategoryRequest request)
    {
        if (await _db.Categories.AnyAsync(c => c.Name == request.Name))
            throw new InvalidOperationException($"A category named '{request.Name}' already exists.");

        var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-").Replace("&", "and");

        var category = new Category
        {
            Name = request.Name,
            Description = request.Description,
            Slug = slug
        };

        _db.Categories.Add(category);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created category {Name}", category.Name);
        return MapToDto(category);
    }

    public async Task<CategoryDto?> UpdateAsync(Guid id, CreateCategoryRequest request)
    {
        var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return null;

        category.Name = request.Name;
        category.Description = request.Description;
        category.Slug = request.Slug ?? category.Slug;
        category.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("Updated category {Id}", id);
        return MapToDto(category);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id);
        if (category is null) return false;

        if (category.Products.Any())
            throw new InvalidOperationException("Cannot delete a category that contains products. Reassign or delete products first.");

        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Deleted category {Id}", id);
        return true;
    }

    private static CategoryDto MapToDto(Category c) => new(
        c.Id, c.Name, c.Description, c.Slug, c.IsActive,
        c.Products.Count, c.CreatedAt, c.UpdatedAt
    );
}
