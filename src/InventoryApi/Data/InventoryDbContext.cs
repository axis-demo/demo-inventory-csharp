using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace InventoryApi.Data;

public class InventoryDbContext : DbContext
{
    public InventoryDbContext(DbContextOptions<InventoryDbContext> options) : base(options) { }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderLine> OrderLines => Set<OrderLine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasIndex(p => p.SKU).IsUnique();
            entity.HasOne(p => p.Category)
                  .WithMany(c => c.Products)
                  .HasForeignKey(p => p.CategoryId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasIndex(o => o.OrderNumber).IsUnique();
            entity.HasMany(o => o.Lines)
                  .WithOne(l => l.Order)
                  .HasForeignKey(l => l.OrderId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OrderLine>(entity =>
        {
            entity.HasOne(l => l.Product)
                  .WithMany(p => p.OrderLines)
                  .HasForeignKey(l => l.ProductId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }

    public static void SeedData(InventoryDbContext db)
    {
        if (db.Categories.Any()) return;

        var electronics = new Category { Id = Guid.NewGuid(), Name = "Electronics", Slug = "electronics", Description = "Electronic devices and accessories" };
        var clothing = new Category { Id = Guid.NewGuid(), Name = "Clothing", Slug = "clothing", Description = "Apparel and fashion items" };
        var food = new Category { Id = Guid.NewGuid(), Name = "Food & Beverage", Slug = "food-beverage", Description = "Food and drink products" };
        var books = new Category { Id = Guid.NewGuid(), Name = "Books", Slug = "books", Description = "Books, eBooks, and publications" };
        var sports = new Category { Id = Guid.NewGuid(), Name = "Sports", Slug = "sports", Description = "Sports and outdoor equipment" };

        db.Categories.AddRange(electronics, clothing, food, books, sports);

        db.Products.AddRange(
            new Product { Name = "Wireless Headphones Pro", SKU = "ELEC-001", Price = 149.99m, Cost = 60.00m, StockQuantity = 85, ReorderPoint = 15, CategoryId = electronics.Id, Brand = "TechCo", Description = "Premium wireless headphones with noise cancellation and 30hr battery life." },
            new Product { Name = "USB-C Charging Cable 2m", SKU = "ELEC-002", Price = 19.99m, Cost = 4.50m, StockQuantity = 250, ReorderPoint = 50, CategoryId = electronics.Id, Brand = "TechCo", Description = "High-speed USB-C to USB-C charging and data cable, 2 meter length." },
            new Product { Name = "Mechanical Keyboard TKL", SKU = "ELEC-003", Price = 89.99m, Cost = 35.00m, StockQuantity = 42, ReorderPoint = 10, CategoryId = electronics.Id, Brand = "TechCo", Description = "Tenkeyless mechanical keyboard with RGB backlight and blue switches." },
            new Product { Name = "Classic White T-Shirt", SKU = "CLTH-001", Price = 24.99m, Cost = 8.00m, StockQuantity = 320, ReorderPoint = 50, CategoryId = clothing.Id, Brand = "FashionBrand", Description = "100% organic cotton classic fit t-shirt, available in all sizes." },
            new Product { Name = "Running Shoes X500", SKU = "CLTH-002", Price = 119.99m, Cost = 45.00m, StockQuantity = 65, ReorderPoint = 20, CategoryId = clothing.Id, Brand = "SportMax", Description = "Lightweight running shoes with memory foam insole and breathable mesh upper." },
            new Product { Name = "Organic Green Tea 100pk", SKU = "FOOD-001", Price = 12.99m, Cost = 4.00m, StockQuantity = 180, ReorderPoint = 30, CategoryId = food.Id, Brand = "HealthPlus", Description = "Premium organic green tea bags, 100 count. No artificial flavors." },
            new Product { Name = "Protein Powder Vanilla 2kg", SKU = "FOOD-002", Price = 54.99m, Cost = 22.00m, StockQuantity = 90, ReorderPoint = 25, CategoryId = food.Id, Brand = "HealthPlus", Description = "Whey protein isolate, 25g protein per serving, 80 servings per container." },
            new Product { Name = "Clean Code by Robert Martin", SKU = "BOOK-001", Price = 39.99m, Cost = 15.00m, StockQuantity = 55, ReorderPoint = 10, CategoryId = books.Id, Brand = "BookHouse", Description = "A Handbook of Agile Software Craftsmanship. Essential reading for developers." },
            new Product { Name = "Design Patterns: GoF", SKU = "BOOK-002", Price = 49.99m, Cost = 18.00m, StockQuantity = 8, ReorderPoint = 10, CategoryId = books.Id, Brand = "BookHouse", Description = "Gang of Four classic software design patterns book. Timeless reference." },
            new Product { Name = "Yoga Mat Premium 6mm", SKU = "SPRT-001", Price = 34.99m, Cost = 12.00m, StockQuantity = 110, ReorderPoint = 20, CategoryId = sports.Id, Brand = "SportMax", Description = "Non-slip premium yoga mat, 6mm thickness, eco-friendly TPE material." }
        );

        db.SaveChanges();
    }
}
