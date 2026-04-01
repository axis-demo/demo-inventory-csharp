using FluentAssertions;
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace InventoryApi.Tests;

public class ProductServiceTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly ProductService _sut;
    private readonly Category _testCategory;

    public ProductServiceTests()
    {
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _db = new InventoryDbContext(options);
        _db.Database.EnsureCreated();

        _testCategory = new Category { Id = Guid.NewGuid(), Name = "Test Category", Slug = "test-category" };
        _db.Categories.Add(_testCategory);
        _db.SaveChanges();

        _sut = new ProductService(_db, NullLogger<ProductService>.Instance);
    }

    public void Dispose() => _db.Dispose();

    [Fact]
    public async Task GetAllAsync_ReturnsActiveProducts()
    {
        // Arrange
        _db.Products.AddRange(
            new Product { Name = "Active Product", SKU = "ACT-001", CategoryId = _testCategory.Id, IsActive = true },
            new Product { Name = "Inactive Product", SKU = "INA-001", CategoryId = _testCategory.Id, IsActive = false }
        );
        await _db.SaveChangesAsync();

        // Act
        var result = await _sut.GetAllAsync(activeOnly: true);

        // Assert
        result.Should().HaveCount(1);
        result.First().Name.Should().Be("Active Product");
    }

    [Fact]
    public async Task GetAllAsync_WithActiveOnlyFalse_ReturnsAllProducts()
    {
        _db.Products.AddRange(
            new Product { Name = "Active", SKU = "A-001", CategoryId = _testCategory.Id, IsActive = true },
            new Product { Name = "Inactive", SKU = "I-001", CategoryId = _testCategory.Id, IsActive = false }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetAllAsync(activeOnly: false);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetByIdAsync_ExistingProduct_ReturnsDto()
    {
        var product = new Product { Name = "Test", SKU = "TST-001", CategoryId = _testCategory.Id, Price = 9.99m };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(product.Id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(product.Id);
        result.SKU.Should().Be("TST-001");
        result.Price.Should().Be(9.99m);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistentProduct_ReturnsNull()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_CreatesProduct()
    {
        var request = new CreateProductRequest(
            "New Widget", "WDGT-001", "A fine widget", 29.99m, 12.00m,
            100, 20, "WidgetCo", 0.5m, _testCategory.Id
        );

        var result = await _sut.CreateAsync(request);

        result.Should().NotBeNull();
        result.Name.Should().Be("New Widget");
        result.SKU.Should().Be("WDGT-001");
        result.Price.Should().Be(29.99m);
        result.StockQuantity.Should().Be(100);
        result.CategoryId.Should().Be(_testCategory.Id);
    }

    [Fact]
    public async Task CreateAsync_DuplicateSKU_ThrowsInvalidOperationException()
    {
        _db.Products.Add(new Product { Name = "Existing", SKU = "DUP-001", CategoryId = _testCategory.Id });
        await _db.SaveChangesAsync();

        var request = new CreateProductRequest("Another", "DUP-001", null, 10m, 5m, 0, 5, null, null, _testCategory.Id);

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*SKU*DUP-001*already exists*");
    }

    [Fact]
    public async Task CreateAsync_InvalidCategory_ThrowsInvalidOperationException()
    {
        var request = new CreateProductRequest("X", "X-001", null, 10m, 5m, 0, 5, null, null, Guid.NewGuid());

        var act = () => _sut.CreateAsync(request);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Category*does not exist*");
    }

    [Fact]
    public async Task DeleteAsync_ExistingProduct_ReturnsTrue()
    {
        var product = new Product { Name = "Delete Me", SKU = "DEL-001", CategoryId = _testCategory.Id };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var result = await _sut.DeleteAsync(product.Id);

        result.Should().BeTrue();
        (await _db.Products.FindAsync(product.Id)).Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_NonExistentProduct_ReturnsFalse()
    {
        var result = await _sut.DeleteAsync(Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task AdjustStockAsync_PositiveAdjustment_IncreasesStock()
    {
        var product = new Product { Name = "Stocked", SKU = "STK-001", CategoryId = _testCategory.Id, StockQuantity = 50 };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var result = await _sut.AdjustStockAsync(product.Id, new StockAdjustmentRequest(25, "Restock"));

        result.Should().NotBeNull();
        result!.StockQuantity.Should().Be(75);
    }

    [Fact]
    public async Task AdjustStockAsync_ExceedsAvailableStock_ThrowsInvalidOperationException()
    {
        var product = new Product { Name = "Low", SKU = "LOW-001", CategoryId = _testCategory.Id, StockQuantity = 5 };
        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        var act = () => _sut.AdjustStockAsync(product.Id, new StockAdjustmentRequest(-10, "Sale"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Insufficient stock*");
    }

    [Fact]
    public async Task SearchAsync_MatchesName_ReturnsResults()
    {
        _db.Products.AddRange(
            new Product { Name = "Bluetooth Speaker", SKU = "BT-001", CategoryId = _testCategory.Id },
            new Product { Name = "Wired Headphones", SKU = "WH-001", CategoryId = _testCategory.Id }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.SearchAsync("bluetooth");

        result.Should().HaveCount(1);
        result.First().SKU.Should().Be("BT-001");
    }

    [Fact]
    public async Task GetLowStockAsync_ReturnsOnlyLowStockProducts()
    {
        _db.Products.AddRange(
            new Product { Name = "Low Stock", SKU = "LS-001", CategoryId = _testCategory.Id, StockQuantity = 3, ReorderPoint = 10 },
            new Product { Name = "High Stock", SKU = "HS-001", CategoryId = _testCategory.Id, StockQuantity = 100, ReorderPoint = 10 }
        );
        await _db.SaveChangesAsync();

        var result = await _sut.GetLowStockAsync();

        result.Should().HaveCount(1);
        result.First().SKU.Should().Be("LS-001");
        result.First().IsLowStock.Should().BeTrue();
    }
}
