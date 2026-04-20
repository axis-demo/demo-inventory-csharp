# Code Documentation: demo-inventory-csharp

## Executive Summary

**demo-inventory-csharp** is a demonstration ASP.NET Core 9 REST API for inventory management. The system implements a clean layered architecture with comprehensive CRUD operations for products, categories, and orders. It features real-time stock management, full-text search capabilities, request correlation tracking, and includes unit tests demonstrating best practices.

**Technology Stack:**
- **Framework:** ASP.NET Core 9 with implicit usings and nullable reference types enabled
- **Database:** Entity Framework Core 9.0.0 with in-memory database
- **API Documentation:** Swagger/Swashbuckle 6.5.0
- **Testing:** xUnit 2.6.1 with FluentAssertions 6.12.0 and Moq 4.20.69
- **Target Framework:** .NET 9.0

---

## 1. Architecture and Design Patterns

### 1.1 Layered Architecture

The application follows a **three-tier layered architecture** with clear separation of concerns:

```
┌─────────────────────────────────────────────────────────────┐
│                    API Layer (Controllers)                   │
│  ProductsController | CategoriesController | OrdersController│
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Service Layer (Business Logic)            │
│  ProductService | CategoryService | OrderService             │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    Data Access Layer (EF Core)               │
│              InventoryDbContext + DbSets                     │
└─────────────────────────────────────────────────────────────┘
```

**Rationale:** This separation enables independent testing of business logic, easy maintenance, and clear responsibility boundaries. Services contain business rules while controllers handle HTTP concerns.

### 1.2 Dependency Injection Pattern

The application uses ASP.NET Core's built-in dependency injection container configured in `Program.cs` (lines 1-70):

```csharp
// Service registration (Program.cs, lines 24-26)
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();

// DbContext registration (Program.cs, lines 19-20)
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));
```

**Pattern Details:**
- **Scoped Lifetime:** Services are scoped to the HTTP request lifetime, ensuring one instance per request. This provides request isolation and prevents state leakage between concurrent requests.
- **Constructor Injection:** All services receive dependencies through constructors, enabling loose coupling and testability
- **Logger Injection:** `ILogger<T>` is injected into services for structured logging via the built-in logging framework

**Service Lifetime Explanation:**
- **Scoped services** are created once per HTTP request and disposed when the request completes
- This ensures that each request has its own isolated `InventoryDbContext` instance, preventing concurrency issues
- Multiple services in the same request share the same context instance, enabling transaction consistency

### 1.3 Data Transfer Object (DTO) Pattern

DTOs are used to decouple API contracts from domain models. Defined in `src/InventoryApi/DTOs/ProductDto.cs` (lines 1-106):

**DTO Types:**
- **Read DTOs:** `ProductDto`, `CategoryDto`, `OrderDto`, `OrderLineDto` - returned from API endpoints
- **Create DTOs:** `CreateProductRequest`, `CreateCategoryRequest`, `CreateOrderRequest` - accept POST data
- **Update DTOs:** `UpdateProductRequest` - accept PUT data
- **Specialized DTOs:** `StockAdjustmentRequest`, `UpdateOrderStatusRequest` - for specific operations

All DTOs use C# record types for immutability and concise syntax. Records provide:
- **Immutability by default:** Properties cannot be modified after creation
- **Built-in equality:** Automatic implementation of `Equals()` and `GetHashCode()`
- **Concise syntax:** Eliminates boilerplate getter/setter code
- **Automatic ToString():** Useful for logging and debugging

**Example Record Definition (ProductDto.cs, lines 5-20):**
```csharp
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
```

---

## 2. Domain Models and Entities

### 2.1 Product Entity

**File:** `src/InventoryApi/Models/Product.cs` (lines 1-64)

**Responsibilities:**
- Represents a physical or digital product in the inventory system
- Manages stock quantity and reorder point logic
- Maintains relationships with categories and order lines
- Enforces business rules for stock management

**Key Properties:**
| Property | Type | Constraints | Purpose |
|----------|------|-----------|---------|
| `Id` | `Guid` | Primary Key | Unique product identifier |
| `Name` | `string` | Required, MaxLength(200) | Product display name |
| `SKU` | `string` | Required, MaxLength(100), Unique | Stock Keeping Unit for inventory tracking |
| `Description` | `string?` | MaxLength(2000) | Detailed product information |
| `Price` | `decimal` | Range(0, max), Column(18,2) | Selling price |
| `Cost` | `decimal` | Range(0, max), Column(18,2) | Cost of goods |
| `StockQuantity` | `int` | Range(0, max) | Current inventory count |
| `ReorderPoint` | `int` | Range(0, max), Default=10 | Minimum stock threshold |
| `IsActive` | `bool` | Default=true | Soft delete flag |
| `Brand` | `string?` | MaxLength(100) | Product manufacturer |
| `WeightKg` | `decimal?` | Column(10,2) | Product weight for shipping |
| `CategoryId` | `Guid` | Foreign Key | Reference to parent category |
| `CreatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |
| `UpdatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |

**Key Methods:**

```csharp
// Property: IsLowStock (line 52)
// Computed property that checks if stock is at or below reorder point
public bool IsLowStock => StockQuantity <= ReorderPoint;

// Method: UpdateStock (lines 54-60)
/// <summary>
/// Adjusts the product stock quantity by the specified amount.
/// </summary>
/// <param name="quantity">The quantity to add (positive) or subtract (negative)</param>
/// <exception cref="InvalidOperationException">
/// Thrown when the adjustment would result in negative stock.
/// Error message format: "Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}"
/// </exception>
public void UpdateStock(int quantity)
{
    if (StockQuantity + quantity < 0)
        throw new InvalidOperationException(
            $"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}");
    
    StockQuantity += quantity;
    UpdatedAt = DateTime.UtcNow;
}
```

**Stock Management Business Rules:**
- Stock quantity cannot go below zero
- Any adjustment updates the `UpdatedAt` timestamp
- Negative adjustments (sales/removals) are validated before execution
- The method throws `InvalidOperationException` with a detailed error message if validation fails

**Relationships:**
- **One-to-Many:** One Category has many Products (via `CategoryId` foreign key)
- **One-to-Many:** One Product has many OrderLines (via `OrderLines` collection)

### 2.2 Category Entity

**File:** `src/InventoryApi/Models/Category.cs` (lines 1-28)

**Responsibilities:**
- Groups products into logical categories
- Provides hierarchical organization of inventory
- Prevents deletion of categories that contain products

**Key Properties:**
| Property | Type | Constraints | Purpose |
|----------|------|-----------|---------|
| `Id` | `Guid` | Primary Key | Unique category identifier |
| `Name` | `string` | Required, MaxLength(100) | Category display name |
| `Description` | `string?` | MaxLength(500) | Category details |
| `Slug` | `string?` | MaxLength(50) | URL-friendly identifier |
| `IsActive` | `bool` | Default=true | Soft delete flag |
| `CreatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |
| `UpdatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |

**Key Methods:**

```csharp
// Property: ProductCount (line 26)
// Computed property that returns the count of products in this category
public int ProductCount => Products.Count;
```

**Slug Generation Rules (CategoryService.cs, line 41):**
The slug is automatically generated from the category name if not provided:
- Convert to lowercase
- Replace spaces with hyphens
- Replace ampersands (&) with "and"
- Example: "Food & Beverage" → "food-and-beverage"

**Relationships:**
- **One-to-Many:** One Category has many Products (via `Products` collection)

### 2.3 Order and OrderLine Entities

**File:** `src/InventoryApi/Models/Order.cs` (lines 1-86)

**Order Status Enum (lines 7-14):**
```csharp
public enum OrderStatus
{
    Pending,      // Initial state - order created but not yet confirmed
    Confirmed,    // Stock reserved - order confirmed and inventory deducted
    Processing,   // Being prepared - order is being packed/prepared
    Shipped,      // In transit - order has been shipped to customer
    Delivered,    // Received by customer - order successfully delivered
    Cancelled,    // Order cancelled - order was cancelled before processing
    Refunded      // Payment refunded - order was refunded to customer
}
```

**Order Status Lifecycle:**
- Orders are created in `Confirmed` status (stock is immediately deducted)
- Status can be updated to any other state via `UpdateStatusAsync()`
- Status updates do not affect inventory (stock deduction happens only at creation)

**Order Entity Properties:**
| Property | Type | Constraints | Purpose |
|----------|------|-----------|---------|
| `Id` | `Guid` | Primary Key | Unique order identifier |
| `OrderNumber` | `string` | Required, MaxLength(50), Unique | Human-readable order ID |
| `CustomerName` | `string` | Required, MaxLength(200) | Customer name |
| `CustomerEmail` | `string?` | MaxLength(200) | Customer contact |
| `Status` | `OrderStatus` | Enum, Default=Pending | Order lifecycle state |
| `TotalAmount` | `decimal` | Column(18,2) | Order total value |
| `ShippingAddress` | `string?` | MaxLength(500) | Delivery address |
| `Notes` | `string?` | MaxLength(1000) | Order notes |
| `CreatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |
| `UpdatedAt` | `DateTime` | Default=UtcNow | Audit timestamp |

**Order Methods:**

```csharp
// Method: RecalculateTotal (lines 43-47)
/// <summary>
/// Recalculates the order total by summing all line item totals.
/// </summary>
/// <remarks>
/// Formula: TotalAmount = Sum of (Quantity * UnitPrice) for all line items
/// Updates the UpdatedAt timestamp to current UTC time.
/// </remarks>
public void RecalculateTotal()
{
    TotalAmount = Lines.Sum(l => l.LineTotal);
    UpdatedAt = DateTime.UtcNow;
}
```

**OrderLine Entity Properties:**
| Property | Type | Constraints | Purpose |
|----------|------|-----------|---------|
| `Id` | `Guid` | Primary Key | Unique line item identifier |
| `OrderId` | `Guid` | Foreign Key | Reference to parent order |
| `ProductId` | `Guid` | Foreign Key | Reference to product |
| `ProductName` | `string` | Required, MaxLength(200) | Denormalized product name |
| `ProductSKU` | `string` | MaxLength(100) | Denormalized product SKU |
| `Quantity` | `int` | Range(1, max) | Units ordered |
| `UnitPrice` | `decimal` | Column(18,2) | Price at time of order |

**OrderLine Denormalization Strategy:**
OrderLine stores denormalized copies of product data (ProductName, ProductSKU, UnitPrice) to preserve the order state at the time of purchase. This ensures that:
- Historical orders show the exact product name and price at the time of order
- Product changes don't affect historical order records
- Orders remain accurate even if products are deleted

**OrderLine Computed Property:**

```csharp
// Property: LineTotal (line 79)
// Computed property that calculates the total for this line item
public decimal LineTotal => Quantity * UnitPrice;
```

**Relationships:**
- **One-to-Many:** One Order has many OrderLines (via `Lines` collection, cascade delete)
- **Many-to-One:** Many OrderLines reference one Product (restrict delete)

---

## 3. Data Access Layer

### 3.1 InventoryDbContext

**File:** `src/InventoryApi/Data/InventoryDbContext.cs` (lines 1-73)

**Responsibilities:**
- Manages Entity Framework Core database context
- Defines entity mappings and relationships
- Provides seed data for development
- Enforces referential integrity and delete behaviors

**DbSet Properties (lines 10-13):**

```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderLine> OrderLines => Set<OrderLine>();
```

### 3.2 Entity Configuration

**OnModelCreating Method (lines 15-40):**

**Product Configuration (lines 17-23):**
```csharp
modelBuilder.Entity<Product>(entity =>
{
    // Enforce SKU uniqueness at database level
    entity.HasIndex(p => p.SKU).IsUnique();
    
    // Configure relationship with Category
    entity.HasOne(p => p.Category)
          .WithMany(c => c.Products)
          .HasForeignKey(p => p.CategoryId)
          // Restrict deletion: prevent category deletion if products exist
          .OnDelete(DeleteBehavior.Restrict);
});
```

**Delete Behavior Explanation:**
- `DeleteBehavior.Restrict` prevents deletion of a category if it contains products
- This enforces referential integrity and prevents orphaned products
- Attempting to delete a category with products throws an exception in the service layer

**Order Configuration (lines 25-31):**
```csharp
modelBuilder.Entity<Order>(entity =>
{
    // Enforce OrderNumber uniqueness at database level
    entity.HasIndex(o => o.OrderNumber).IsUnique();
    
    // Configure relationship with OrderLines
    entity.HasMany(o => o.Lines)
          .WithOne(l => l.Order)
          .HasForeignKey(l => l.OrderId)
          // Cascade delete: delete order lines when order is deleted
          .OnDelete(DeleteBehavior.Cascade);
});
```

**Delete Behavior Explanation:**
- `DeleteBehavior.Cascade` automatically deletes all OrderLines when an Order is deleted
- This maintains referential integrity and prevents orphaned line items
- Useful for cleanup when orders are removed from the system

**OrderLine Configuration (lines 33-39):**
```csharp
modelBuilder.Entity<OrderLine>(entity =>
{
    entity.HasOne(l => l.Product)
          .WithMany(p => p.OrderLines)
          .HasForeignKey(l => l.ProductId)
          // Restrict deletion: prevent product deletion if used in orders
          .OnDelete(DeleteBehavior.Restrict);
});
```

**Delete Behavior Explanation:**
- `DeleteBehavior.Restrict` prevents deletion of products that have been used in orders
- This preserves order history and prevents data loss
- Products can only be deleted if they haven't been ordered

### 3.3 Seed Data

**SeedData Method (lines 42-73):**

The `SeedData` static method populates the database with 10 sample products across 5 categories. It is called during application startup in `Program.cs` (lines 47-52):

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
    InventoryDbContext.SeedData(db);
}
```

**Idempotency Check (line 42):**
```csharp
if (db.Categories.Any()) return;
```
This check ensures seeding only happens once, preventing duplicate data on subsequent application restarts.

**Categories Seeded:**
1. **Electronics** (4 products: Wireless Headphones Pro, USB-C Cable, Mechanical Keyboard)
2. **Clothing** (2 products: Classic White T-Shirt, Running Shoes X500)
3. **Food & Beverage** (2 products: Organic Green Tea, Protein Powder)
4. **Books** (2 products: Clean Code, Design Patterns)
5. **Sports** (1 product: Yoga Mat Premium)

**Seeding Logic:**
- Creates categories with slugs and descriptions
- Creates products with realistic pricing, costs, and stock levels
- Includes low-stock products for testing (Design Patterns: 8 units, reorder point: 10)
- All products are marked as active by default

**Example Product (line 56):**
```csharp
new Product { 
    Name = "Wireless Headphones Pro", 
    SKU = "ELEC-001", 
    Price = 149.99m, 
    Cost = 60.00m, 
    StockQuantity = 85, 
    ReorderPoint = 15, 
    CategoryId = electronics.Id, 
    Brand = "TechCo", 
    Description = "Premium wireless headphones with noise cancellation and 30hr battery life." 
}
```

---

## 4. Service Layer

### 4.1 ProductService

**File:** `src/InventoryApi/Services/ProductService.cs` (lines 1-180)

**Responsibilities:**
- Implements business logic for product operations
- Validates business rules (SKU uniqueness, category existence)
- Manages stock adjustments with audit logging
- Provides search and filtering capabilities
- Handles product lifecycle (create, read, update, delete)

**Constructor (lines 12-17):**
```csharp
/// <summary>
/// Initializes a new instance of ProductService with database context and logger.
/// </summary>
/// <param name="db">The inventory database context for data access</param>
/// <param name="logger">The logger for structured logging of service operations</param>
public ProductService(InventoryDbContext db, ILogger<ProductService> logger)
{
    _db = db;
    _logger = logger;
}
```

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync` | `Task<IEnumerable<ProductDto>> GetAllAsync(bool activeOnly = true)` | Retrieve all products, optionally filtered by active status |
| `GetByIdAsync` | `Task<ProductDto?> GetByIdAsync(Guid id)` | Retrieve single product by ID |
| `GetBySkuAsync` | `Task<ProductDto?> GetBySkuAsync(string sku)` | Retrieve product by SKU code |
| `SearchAsync` | `Task<IEnumerable<ProductDto>> SearchAsync(string query)` | Full-text search across name, SKU, description, brand |
| `GetByCategoryAsync` | `Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId)` | Retrieve products in specific category |
| `GetLowStockAsync` | `Task<IEnumerable<ProductDto>> GetLowStockAsync()` | Retrieve products at or below reorder point |
| `CreateAsync` | `Task<ProductDto> CreateAsync(CreateProductRequest request)` | Create new product with validation |
| `UpdateAsync` | `Task<ProductDto?> UpdateAsync(Guid id, UpdateProductRequest request)` | Update existing product |
| `DeleteAsync` | `Task<bool> DeleteAsync(Guid id)` | Delete product by ID |
| `AdjustStockAsync` | `Task<ProductDto?> AdjustStockAsync(Guid id, StockAdjustmentRequest request)` | Adjust stock with reason tracking |

**Key Implementation Details:**

**GetAllAsync (lines 19-28):**
```csharp
/// <summary>
/// Retrieves all products, optionally filtered to active products only.
/// </summary>
/// <param name="activeOnly">If true, returns only products where IsActive=true. Default is true.</param>
/// <returns>Enumerable of ProductDto ordered by name</returns>
/// <remarks>
/// Uses eager loading to include related Category data.
/// Deferred execution: query is not executed until ToListAsync() is called.
/// </remarks>
public async Task<IEnumerable<ProductDto>> GetAllAsync(bool activeOnly = true)
{
    var query = _db.Products.Include(p => p.Category).AsQueryable();
    
    if (activeOnly)
        query = query.Where(p => p.IsActive);
    
    var products = await query.OrderBy(p => p.Name).ToListAsync();
    return products.Select(MapToDto);
}
```

**SearchAsync (lines 44-60):**
```csharp
/// <summary>
/// Searches products using full-text search across multiple fields.
/// </summary>
/// <param name="query">The search query string</param>
/// <returns>Enumerable of matching ProductDto ordered by name</returns>
/// <remarks>
/// Searches across: Name, SKU, Description, and Brand fields.
/// Search is case-insensitive.
/// Returns all active products if query is null or whitespace.
/// </remarks>
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
```

**Search Field Coverage:**
- **Name:** Product display name
- **SKU:** Stock Keeping Unit code
- **Description:** Detailed product information
- **Brand:** Product manufacturer

**GetByCategoryAsync (lines 67-75):**
```csharp
/// <summary>
/// Retrieves all active products in a specific category.
/// </summary>
/// <param name="categoryId">The category ID to filter by</param>
/// <returns>Enumerable of ProductDto ordered by name</returns>
public async Task<IEnumerable<ProductDto>> GetByCategoryAsync(Guid categoryId)
{
    var products = await _db.Products
        .Include(p => p.Category)
        .Where(p => p.CategoryId == categoryId && p.IsActive)
        .OrderBy(p => p.Name)
        .ToListAsync();
    
    return products.Select(MapToDto);
}
```

**CreateAsync (lines 75-103):**
```csharp
/// <summary>
/// Creates a new product with validation.
/// </summary>
/// <param name="request">The product creation request</param>
/// <returns>The created ProductDto</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when:
/// - A product with the same SKU already exists
/// - The specified category does not exist or is inactive
/// </exception>
public async Task<ProductDto> CreateAsync(CreateProductRequest request)
{
    // Validate SKU uniqueness
    if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
        throw new InvalidOperationException($"A product with SKU '{request.SKU}' already exists.");
    
    // Validate category existence and active status
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
    
    // Load related category before mapping to DTO
    await _db.Entry(product).Reference(p => p.Category).LoadAsync();
    return MapToDto(product);
}
```

**Validation Rules:**
- SKU must be unique across all products
- Category must exist and be active
- All required fields must be provided (enforced by model validation)

**AdjustStockAsync (lines 153-166):**
```csharp
/// <summary>
/// Adjusts product stock quantity with reason tracking.
/// </summary>
/// <param name="id">The product ID</param>
/// <param name="request">The stock adjustment request containing quantity and reason</param>
/// <returns>The updated ProductDto, or null if product not found</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when the adjustment would result in negative stock.
/// </exception>
/// <remarks>
/// The adjustment is validated by Product.UpdateStock() which throws if insufficient stock.
/// The reason is logged for audit purposes.
/// </remarks>
public async Task<ProductDto?> AdjustStockAsync(Guid id, StockAdjustmentRequest request)
{
    var product = await _db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id);
    if (product is null) return null;
    
    product.UpdateStock(request.Quantity);  // Throws if insufficient stock
    await _db.SaveChangesAsync();
    
    _logger.LogInformation("Stock adjusted for {SKU}: {Delta} (Reason: {Reason})", 
        product.SKU, request.Quantity, request.Reason);
    return MapToDto(product);
}
```

**MapToDto (lines 168-173):**
```csharp
/// <summary>
/// Maps a Product entity to a ProductDto for API responses.
/// </summary>
/// <param name="p">The product entity to map</param>
/// <returns>A ProductDto with all relevant product information</returns>
/// <remarks>
/// This private method encapsulates the DTO mapping logic.
/// Uses expression-based mapping for compile-time type safety.
/// Includes computed properties like IsLowStock and CategoryName.
/// </remarks>
private static ProductDto MapToDto(Product p) => new(
    p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
    p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
    p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
    p.CreatedAt, p.UpdatedAt
);
```

**DTO Mapping Strategy:**
- Private method encapsulates mapping logic
- Uses expression-based mapping for conciseness
- Includes null-conditional operator for optional Category?.Name
- Includes computed property IsLowStock for client convenience

### 4.2 CategoryService

**File:** `src/InventoryApi/Services/CategoryService.cs` (lines 1-94)

**Responsibilities:**
- Manages category CRUD operations
- Enforces business rules (no duplicate names, no deletion with products)
- Provides category listing with product counts
- Generates URL-friendly slugs

**Constructor (lines 12-17):**
```csharp
/// <summary>
/// Initializes a new instance of CategoryService.
/// </summary>
/// <param name="db">The inventory database context</param>
/// <param name="logger">The logger for structured logging</param>
public CategoryService(InventoryDbContext db, ILogger<CategoryService> logger)
{
    _db = db;
    _logger = logger;
}
```

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync` | `Task<IEnumerable<CategoryDto>> GetAllAsync()` | Retrieve all categories with product counts |
| `GetByIdAsync` | `Task<CategoryDto?> GetByIdAsync(Guid id)` | Retrieve single category |
| `CreateAsync` | `Task<CategoryDto> CreateAsync(CreateCategoryRequest request)` | Create new category with slug generation |
| `UpdateAsync` | `Task<CategoryDto?> UpdateAsync(Guid id, CreateCategoryRequest request)` | Update category details |
| `DeleteAsync` | `Task<bool> DeleteAsync(Guid id)` | Delete category (must be empty) |

**Key Implementation Details:**

**CreateAsync (lines 36-56):**
```csharp
/// <summary>
/// Creates a new category with automatic slug generation.
/// </summary>
/// <param name="request">The category creation request</param>
/// <returns>The created CategoryDto</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when a category with the same name already exists.
/// </exception>
/// <remarks>
/// Slug is automatically generated from the category name if not provided.
/// Slug generation rules:
/// - Convert to lowercase
/// - Replace spaces with hyphens
/// - Replace ampersands (&) with "and"
/// Example: "Food & Beverage" → "food-and-beverage"
/// </remarks>
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
```

**DeleteAsync (lines 68-80):**
```csharp
/// <summary>
/// Deletes a category if it contains no products.
/// </summary>
/// <param name="id">The category ID</param>
/// <returns>True if deleted, false if not found</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when attempting to delete a category that contains products.
/// Error message: "Cannot delete a category that contains products. Reassign or delete products first."
/// </exception>
/// <remarks>
/// This implements cascade delete prevention at the service layer.
/// The database also enforces this via DeleteBehavior.Restrict on the foreign key.
/// </remarks>
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
```

**Cascade Delete Prevention:**
- Service layer checks if category has products before deletion
- Database enforces this via `DeleteBehavior.Restrict` on the foreign key
- Prevents orphaned products and maintains referential integrity

**MapToDto (lines 87-91):**
```csharp
/// <summary>
/// Maps a Category entity to a CategoryDto.
/// </summary>
/// <param name="c">The category entity</param>
/// <returns>A CategoryDto with product count included</returns>
/// <remarks>
/// Includes the product count computed from the Products collection.
/// This provides clients with category statistics without additional queries.
/// </remarks>
private static CategoryDto MapToDto(Category c) => new(
    c.Id, c.Name, c.Description, c.Slug, c.IsActive,
    c.Products.Count, c.CreatedAt, c.UpdatedAt
);
```

### 4.3 OrderService

**File:** `src/InventoryApi/Services/OrderService.cs` (lines 1-121)

**Responsibilities:**
- Manages order lifecycle from creation to status updates
- Validates stock availability before order confirmation
- Automatically deducts inventory when orders are placed
- Generates unique order numbers
- Preserves order state through denormalization

**Constructor (lines 13-18):**
```csharp
/// <summary>
/// Initializes a new instance of OrderService.
/// </summary>
/// <remarks>
/// Uses a static _orderCounter for thread-safe order number generation.
/// The counter starts at 1000 and increments for each new order.
/// </remarks>
private static int _orderCounter = 1000;  // Thread-safe counter for order numbers

public OrderService(InventoryDbContext db, ILogger<OrderService> logger)
{
    _db = db;
    _logger = logger;
}
```

**Thread-Safe Order Number Generation:**
The `_orderCounter` is a static field that is incremented using `Interlocked.Increment()` to ensure thread safety in multi-threaded environments. This prevents race conditions where multiple concurrent requests might generate the same order number.

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync` | `Task<IEnumerable<OrderDto>> GetAllAsync()` | Retrieve all orders with line items |
| `GetByIdAsync` | `Task<OrderDto?> GetByIdAsync(Guid id)` | Retrieve single order |
| `CreateAsync` | `Task<OrderDto> CreateAsync(CreateOrderRequest request)` | Create order with stock validation and deduction |
| `UpdateStatusAsync` | `Task<OrderDto?> UpdateStatusAsync(Guid id, OrderStatus status)` | Update order status |

**Key Implementation Details:**

**CreateAsync (lines 36-91):**
```csharp
/// <summary>
/// Creates a new order with comprehensive stock validation and inventory deduction.
/// </summary>
/// <param name="request">The order creation request</param>
/// <returns>The created OrderDto with status set to Confirmed</returns>
/// <exception cref="InvalidOperationException">
/// Thrown when:
/// - Order has no line items
/// - One or more products not found or inactive
/// - Insufficient stock for any product
/// </exception>
/// <remarks>
/// Order Processing Flow:
/// 1. Validate order has at least one line item
/// 2. Fetch all products upfront and validate they exist and are active
/// 3. Check stock availability for all line items before any deductions
/// 4. Generate unique order number using thread-safe counter
/// 5. Deduct stock for each product using Product.UpdateStock(-quantity)
/// 6. Create denormalized OrderLine records with product snapshot
/// 7. Calculate total and save to database
/// 
/// Stock Deduction Pattern:
/// - Uses negative quantity: product.UpdateStock(-lineReq.Quantity)
/// - Validates in Product.UpdateStock() before actual deduction
/// - Throws if insufficient stock
/// 
/// Denormalization:
/// - OrderLine stores ProductName, ProductSKU, UnitPrice at time of order
/// - Preserves order state even if products are later modified or deleted
/// </remarks>
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
            throw new InvalidOperationException(
                $"Insufficient stock for '{product.Name}' (SKU: {product.SKU}). " +
                $"Available: {product.StockQuantity}, Requested: {lineReq.Quantity}");
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
        product.UpdateStock(-lineReq.Quantity);  // Deduct inventory
        
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
```

**Stock Deduction Validation Pattern:**
1. **Validate all products exist and are active** before any deductions
2. **Check stock availability** for all line items before any deductions
3. **Deduct stock** using `Product.UpdateStock(-quantity)` which throws if insufficient
4. **Create denormalized OrderLine records** with product snapshot (name, SKU, price at time of order)

**Order Number Generation (line 62):**
```csharp
var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
// Generates: ORD-001001, ORD-001002, etc.
```

**Thread-Safe Counter Explanation:**
- `Interlocked.Increment()` atomically increments the counter
- `:D6` format specifier pads the number with leading zeros (6 digits)
- Prevents race conditions in multi-threaded environments
- Each order gets a unique, sequential number

**UpdateStatusAsync (lines 104-115):**
```csharp
/// <summary>
/// Updates the order status without affecting inventory.
/// </summary>
/// <param name="id">The order ID</param>
/// <param name="status">The new order status</param>
/// <returns>The updated OrderDto, or null if order not found</returns>
/// <remarks>
/// Status updates do not affect inventory. Stock is deducted only at order creation.
/// This allows orders to move through their lifecycle without inventory impact.
/// </remarks>
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
```

**MapToDto (lines 114-121):**
```csharp
/// <summary>
/// Maps an Order entity to an OrderDto with all line items.
/// </summary>
/// <param name="o">The order entity</param>
/// <returns>An OrderDto with denormalized line item information</returns>
/// <remarks>
/// Includes all OrderLine items mapped to OrderLineDto.
/// Status is converted to string for JSON serialization.
/// </remarks>
private static OrderDto MapToDto(Order o) => new(
    o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
    o.Status.ToString(), o.TotalAmount, o.ShippingAddress, o.Notes,
    o.Lines.Select(l => new OrderLineDto(l.Id, l.ProductId, l.ProductName, l.ProductSKU, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
    o.CreatedAt, o.UpdatedAt
);
```

---

## 5. API Layer (Controllers)

### 5.1 ProductsController

**File:** `src/InventoryApi/Controllers/ProductsController.cs` (lines 1-162)

**Route:** `[Route("api/v1/[controller]")]` → `/api/v1/products`

**API Versioning:**
The controller uses explicit versioning in the route (`api/v1`) to support multiple API versions in the future. This allows clients to pin to a specific version while new versions are developed.

**Responsibilities:**
- Expose product operations via HTTP endpoints
- Validate request models and handle exceptions
- Return appropriate HTTP status codes
- Provide comprehensive error messages

**Endpoints:**

| HTTP Method | Route | Handler | Status Codes |
|-------------|-------|---------|--------------|
| GET | `/api/v1/products` | `GetAll(bool activeOnly = true)` | 200 OK |
| GET | `/api/v1/products/{id:guid}` | `GetById(Guid id)` | 200 OK, 404 Not Found |
| GET | `/api/v1/products/sku/{sku}` | `GetBySku(string sku)` | 200 OK, 404 Not Found |
| GET | `/api/v1/products/search?q={query}` | `Search(string q)` | 200 OK |
| GET | `/api/v1/products/category/{categoryId:guid}` | `GetByCategory(Guid categoryId)` | 200 OK |
| GET | `/api/v1/products/low-stock` | `GetLowStock()` | 200 OK |
| POST | `/api/v1/products` | `Create(CreateProductRequest request)` | 201 Created, 400 Bad Request, 409 Conflict |
| PUT | `/api/v1/products/{id:guid}` | `Update(Guid id, UpdateProductRequest request)` | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | `/api/v1/products/{id:guid}` | `Delete(Guid id)` | 204 No Content, 404 Not Found |
| POST | `/api/v1/products/{id:guid}/stock` | `AdjustStock(Guid id, StockAdjustmentRequest request)` | 200 OK, 400 Bad Request, 404 Not Found |

**Key Implementation Details:**

**GetAll (lines 23-29):**
```csharp
/// <summary>Get all active products</summary>
[HttpGet]
[ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
{
    var products = await _productService.GetAllAsync(activeOnly);
    return Ok(products);
}
```

**Create (lines 91-108):**
```csharp
/// <summary>Create a new product</summary>
[HttpPost]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status409Conflict)]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    try
    {
        var product = await _productService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Failed to create product: {Message}", ex.Message);
        return Conflict(new { message = ex.Message });
    }
}
```

**Error Handling Pattern:**
- **400 Bad Request:** Returned when `ModelState.IsValid` is false (validation attribute violations)
- **409 Conflict:** Returned when `InvalidOperationException` is caught (business rule violations like duplicate SKU)
- **404 Not Found:** Returned when resource doesn't exist
- **201 Created:** Returned with Location header pointing to the created resource

**409 Conflict Response:**
The 409 status code is used for business rule violations (SKU uniqueness, category existence) to distinguish them from validation errors (400). This allows clients to handle these cases differently.

**AdjustStock (lines 150-162):**
```csharp
/// <summary>Adjust product stock quantity</summary>
[HttpPost("{id:guid}/stock")]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AdjustStock(Guid id, [FromBody] StockAdjustmentRequest request)
{
    try
    {
        var product = await _productService.AdjustStockAsync(id, request);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });
        
        return Ok(product);
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

### 5.2 CategoriesController

**File:** `src/InventoryApi/Controllers/CategoriesController.cs` (lines 1-102)

**Route:** `[Route("api/v1/[controller]")]` → `/api/v1/categories`

**Category Constraints:**
- Category names must be unique
- Categories cannot be deleted if they contain products
- Slug is automatically generated from category name if not provided

**Endpoints:**

| HTTP Method | Route | Handler | Status Codes |
|-------------|-------|---------|--------------|
| GET | `/api/v1/categories` | `GetAll()` | 200 OK |
| GET | `/api/v1/categories/{id:guid}` | `GetById(Guid id)` | 200 OK, 404 Not Found |
| POST | `/api/v1/categories` | `Create(CreateCategoryRequest request)` | 201 Created, 400 Bad Request, 409 Conflict |
| PUT | `/api/v1/categories/{id:guid}` | `Update(Guid id, CreateCategoryRequest request)` | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | `/api/v1/categories/{id:guid}` | `Delete(Guid id)` | 204 No Content, 400 Bad Request, 404 Not Found |

**Key Implementation Details:**

**Delete (lines 88-102):**
```csharp
/// <summary>Delete a category</summary>
[HttpDelete("{id:guid}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Delete(Guid id)
{
    try
    {
        var deleted = await _categoryService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Category {id} not found." });
        
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { message = ex.Message });
    }
}
```

Returns 400 Bad Request when attempting to delete a category with products, with a helpful error message explaining the constraint.

**Update (lines 68-82):**
```csharp
/// <summary>Update a category</summary>
[HttpPut("{id:guid}")]
[ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> Update(Guid id, [FromBody] CreateCategoryRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    var category = await _categoryService.UpdateAsync(id, request);
    if (category is null)
        return NotFound(new { message = $"Category {id} not found." });
    
    return Ok(category);
}
```

### 5.3 OrdersController

**File:** `src/InventoryApi/Controllers/OrdersController.cs` (lines 1-83)

**Route:** `[Route("api/v1/[controller]")]` → `/api/v1/orders`

**Order Processing Flow:**
1. Client submits order with customer info and line items
2. Service validates all products exist and are active
3. Service checks stock availability for all items
4. Service deducts inventory for each product
5. Service creates order with status "Confirmed"
6. Client can update order status through separate endpoint

**Endpoints:**

| HTTP Method | Route | Handler | Status Codes |
|-------------|-------|---------|--------------|
| GET | `/api/v1/orders` | `GetAll()` | 200 OK |
| GET | `/api/v1/orders/{id:guid}` | `GetById(Guid id)` | 200 OK, 404 Not Found |
| POST | `/api/v1/orders` | `Create(CreateOrderRequest request)` | 201 Created, 400 Bad Request |
| PATCH | `/api/v1/orders/{id:guid}/status` | `UpdateStatus(Guid id, UpdateOrderStatusRequest request)` | 200 OK, 400 Bad Request, 404 Not Found |

**Key Implementation Details:**

**Create (lines 45-62):**
```csharp
/// <summary>Place a new order. Validates stock and deducts inventory.</summary>
[HttpPost]
[ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
{
    if (!ModelState.IsValid)
        return BadRequest(ModelState);
    
    try
    {
        var order = await _orderService.CreateAsync(request);
        return CreatedAtAction(nameof(GetById), new { id = order.Id }, order);
    }
    catch (InvalidOperationException ex)
    {
        _logger.LogWarning("Failed to create order: {Message}", ex.Message);
        return BadRequest(new { message = ex.Message });
    }
}
```

**Stock Deduction Side Effects:**
When an order is created, inventory is immediately deducted for all line items. This is a critical side effect that must be understood by API consumers. The order status is set to "Confirmed" to indicate that stock has been reserved.

**UpdateStatus (lines 64-80):**
```csharp
/// <summary>Update order status</summary>
[HttpPatch("{id:guid}/status")]
[ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
{
    if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
        return BadRequest(new { message = 
            $"Invalid status '{request.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}" });
    
    var order = await _orderService.UpdateStatusAsync(id, status);
    if (order is null)
        return NotFound(new { message = $"Order {id} not found." });
    
    return Ok(order);
}
```

Validates order status enum values and returns helpful error messages with valid options.

---

## 6. Middleware and Cross-Cutting Concerns

### 6.1 RequestLoggingMiddleware

**File:** `src/InventoryApi/Middleware/RequestLoggingMiddleware.cs` (lines 1-56)

**Responsibilities:**
- Log all HTTP requests and responses
- Track request duration
- Assign and propagate correlation IDs
- Adjust log level based on HTTP status code
- Enable distributed tracing across services

**Implementation:**

```csharp
/// <summary>
/// Middleware for logging HTTP requests and responses with correlation ID tracking.
/// </summary>
/// <remarks>
/// Correlation IDs enable request tracing across distributed systems.
/// Each request is assigned a unique 8-character ID that is included in all logs.
/// </remarks>
public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    
    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }
    
    public async Task InvokeAsync(HttpContext context)
    {
        // Correlation ID handling (lines 16-18)
        // Accept correlation ID from client or generate new one
        var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                            ?? Guid.NewGuid().ToString("N")[..8];
        context.Response.Headers["X-Correlation-ID"] = correlationId;
        
        // Request timing (line 20)
        var sw = Stopwatch.StartNew();
        
        // Log request start (lines 22-27)
        _logger.LogInformation(
            "[{CorrelationId}] {Method} {Path}{Query} started",
            correlationId,
            context.Request.Method,
            context.Request.Path,
            context.Request.QueryString);
        
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            
            // Determine log level based on status code (lines 35-39)
            var level = context.Response.StatusCode >= 500
                ? LogLevel.Error
                : context.Response.StatusCode >= 400
                    ? LogLevel.Warning
                    : LogLevel.Information;
            
            // Log response (lines 41-47)
            _logger.Log(
                level,
                "[{CorrelationId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
                correlationId,
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                sw.ElapsedMilliseconds);
        }
    }
}
```

**Correlation ID Pattern:**
- Accepts `X-Correlation-ID` header from client for distributed tracing
- Generates 8-character ID if not provided: `Guid.NewGuid().ToString("N")[..8]`
- Echoes correlation ID in response header for request tracing
- Includes correlation ID in all log entries for request tracking

**Correlation ID Generation:**
- `Guid.NewGuid().ToString("N")` generates a 32-character hex string without hyphens
- `[..8]` uses the range operator to take the first 8 characters
- Results in short, readable IDs like "a1b2c3d4"

**Log Level Strategy:**
- **Error (500+):** Server errors - critical issues requiring investigation
- **Warning (400-499):** Client errors - validation or business rule violations
- **Information (200-399):** Successful requests - normal operation

**Finally Block Execution:**
The `finally` block ensures that response logging happens even if an exception occurs during request processing. This guarantees that all requests are logged, including those that fail.

---

## 7. Application Configuration

### 7.1 Program.cs Setup

**File:** `src/InventoryApi/Program.cs` (lines 1-70)

**Service Registration (lines 8-26):**

```csharp
// Controllers and API documentation
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Inventory Management API",
        Version = "v1",
        Description = "A demo ASP.NET Core inventory management REST API with product, category, and order management."
    });
});

// Database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));

// Application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();
```

**CORS Configuration (lines 28-35):**

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

**CORS Policy Details:**
- `AllowAnyOrigin()`: Allows requests from any domain (suitable for demo/development)
- `AllowAnyMethod()`: Allows all HTTP methods (GET, POST, PUT, DELETE, etc.)
- `AllowAnyHeader()`: Allows any request headers

**Logging Configuration (lines 37-39):**

```csharp
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
```

Clears default logging providers and adds console logging for development visibility.

**Database Seeding (lines 47-52):**

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
    InventoryDbContext.SeedData(db);
}
```

Creates a service scope to seed the database with sample data during application startup.

**Middleware Pipeline (lines 54-65):**

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
```

**Middleware Order (Critical):**
1. **Swagger** (development only) - Serves API documentation UI
2. **RequestLoggingMiddleware** (custom) - Logs all requests/responses with correlation IDs
3. **CORS** - Handles cross-origin requests
4. **HTTPS Redirection** - Redirects HTTP to HTTPS
5. **Authorization** - Checks authorization policies
6. **Controller routing** - Maps requests to controller actions

The order is important because middleware is executed in the order it's registered. RequestLoggingMiddleware must come before CORS to log all requests including CORS preflight requests.

---

## 8. Testing Patterns and Utilities

### 8.1 ProductServiceTests

**File:** `tests/InventoryApi.Tests/ProductServiceTests.cs` (lines 1-211)

**Testing Framework:** xUnit 2.6.1 with FluentAssertions 6.12.0

**Test Class Setup (lines 13-28):**

```csharp
/// <summary>
/// Unit tests for ProductService using xUnit and FluentAssertions.
/// </summary>
/// <remarks>
/// Each test gets an isolated in-memory database instance to ensure test independence.
/// Implements IDisposable to clean up database resources after each test.
/// </remarks>
public class ProductServiceTests : IDisposable
{
    private readonly InventoryDbContext _db;
    private readonly ProductService _sut;  // System Under Test
    private readonly Category _testCategory;
    
    public ProductServiceTests()
    {
        // Create isolated in-memory database for each test
        // Using Guid.NewGuid().ToString() ensures unique database per test
        var options = new DbContextOptionsBuilder<InventoryDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        
        _db = new InventoryDbContext(options);
        _db.Database.EnsureCreated();
        
        // Create test category for use in all tests
        _testCategory = new Category { Id = Guid.NewGuid(), Name = "Test Category", Slug = "test-category" };
        _db.Categories.Add(_testCategory);
        _db.SaveChanges();
        
        // Initialize service with null logger to suppress logging during tests
        _sut = new ProductService(_db, NullLogger<ProductService>.Instance);
    }
    
    public void Dispose() => _db.Dispose();
}
```

**Test Isolation Pattern:**
- Each test gets a unique in-memory database instance: `Guid.NewGuid().ToString()`
- Implements `IDisposable` to clean up database resources
- Uses `NullLogger<T>.Instance` to suppress logging during tests
- Prevents test interference and ensures independent test execution

**NullLogger Usage:**
`NullLogger<T>.Instance` is a no-op logger that discards all log messages. This prevents test output from being cluttered with service logs while still satisfying the logger dependency.

**Test Cases:**

| Test Name | Purpose | Assertions |
|-----------|---------|-----------|
| `GetAllAsync_ReturnsActiveProducts` | Verify active-only filtering | Count = 1, Name matches |
| `GetAllAsync_WithActiveOnlyFalse_ReturnsAllProducts` | Verify all products returned | Count = 2 |
| `GetByIdAsync_ExistingProduct_ReturnsDto` | Verify single product retrieval | ID, SKU, Price match |
| `GetByIdAsync_NonExistentProduct_ReturnsNull` | Verify null for missing product | Result is null |
| `CreateAsync_ValidRequest_CreatesProduct` | Verify product creation | All properties match request |
| `CreateAsync_DuplicateSKU_ThrowsInvalidOperationException` | Verify SKU uniqueness | Exception message contains "SKU" and "already exists" |
| `CreateAsync_InvalidCategory_ThrowsInvalidOperationException` | Verify category validation | Exception message contains "Category" and "does not exist" |
| `DeleteAsync_ExistingProduct_ReturnsTrue` | Verify product deletion | Returns true, product removed from DB |
| `DeleteAsync_NonExistentProduct_ReturnsFalse` | Verify delete non-existent | Returns false |
| `AdjustStockAsync_PositiveAdjustment_IncreasesStock` | Verify stock increase | StockQuantity = 75 (50 + 25) |
| `AdjustStockAsync_ExceedsAvailableStock_ThrowsInvalidOperationException` | Verify insufficient stock | Exception message contains "Insufficient stock" |
| `SearchAsync_MatchesName_ReturnsResults` | Verify search by name | Count = 1, SKU matches |
| `GetLowStockAsync_ReturnsOnlyLowStockProducts` | Verify low stock filtering | Count = 1, IsLowStock = true |

**Example Test (lines 33-44):**

```csharp
/// <summary>
/// Tests that GetAllAsync returns only active products when activeOnly=true.
/// </summary>
[Fact]
public async Task GetAllAsync_ReturnsActiveProducts()
{
    // Arrange: Set up test data
    _db.Products.AddRange(
        new Product { Name = "Active Product", SKU = "ACT-001", CategoryId = _testCategory.Id, IsActive = true },
        new Product { Name = "Inactive Product", SKU = "INA-001", CategoryId = _testCategory.Id, IsActive = false }
    );
    await _db.SaveChangesAsync();
    
    // Act: Execute the method under test
    var result = await _sut.GetAllAsync(activeOnly: true);
    
    // Assert: Verify the results
    result.Should().HaveCount(1);
    result.First().Name.Should().Be("Active Product");
}
```

**Arrange-Act-Assert (AAA) Pattern:**
- **Arrange:** Set up test data and preconditions
- **Act:** Execute the method being tested
- **Assert:** Verify the results match expectations

**FluentAssertions Usage:**
- `Should().HaveCount(n)` - Assert collection size
- `Should().NotBeNull()` - Assert non-null
- `Should().BeTrue()` / `Should().BeFalse()` - Assert boolean
- `Should().Be(value)` - Assert equality
- `Should().ThrowAsync<ExceptionType>()` - Assert async exception
- `WithMessage("*pattern*")` - Assert exception message pattern with wildcards

**Exception Testing Pattern (lines 107-115):**

```csharp
/// <summary>
/// Tests that CreateAsync throws InvalidOperationException for duplicate SKU.
/// </summary>
[Fact]
public async Task CreateAsync_DuplicateSKU_ThrowsInvalidOperationException()
{
    // Arrange: Create a product with a specific SKU
    _db.Products.Add(new Product { Name = "Existing", SKU = "DUP-001", CategoryId = _testCategory.Id });
    await _db.SaveChangesAsync();
    
    // Act: Try to create another product with the same SKU
    var request = new CreateProductRequest("Another", "DUP-001", null, 10m, 5m, 0, 5, null, null, _testCategory.Id);
    var act = () => _sut.CreateAsync(request);
    
    // Assert: Verify the exception is thrown with correct message
    await act.Should().ThrowAsync<InvalidOperationException>()
        .WithMessage("*SKU*DUP-001*already exists*");
}
```

**Exception Message Matching:**
- `WithMessage("*pattern*")` uses wildcard matching
- `*` matches any characters
- Allows flexible assertion of exception messages without requiring exact matches

---

## 9. Code Patterns and Conventions

### 9.1 Naming Conventions

**Consistent Naming Patterns:**

| Element | Pattern | Example |
|---------|---------|---------|
| Classes | PascalCase | `ProductService`, `InventoryDbContext` |
| Methods | PascalCase | `GetAllAsync`, `CreateAsync` |
| Properties | PascalCase | `StockQuantity`, `IsActive` |
| Private fields | _camelCase | `_db`, `_logger`, `_orderCounter` |
| Parameters | camelCase | `id`, `request`, `activeOnly` |
| Constants | UPPER_CASE | (none in codebase) |
| DTOs | Suffixed with Dto/Request | `ProductDto`, `CreateProductRequest` |
| Local variables | camelCase | `products`, `categoryExists`, `lower` |

**Naming Convention Consistency:**
All code follows these conventions consistently throughout the codebase, making it easy to identify the type and scope of identifiers.

### 9.2 Async/Await Pattern

All I/O operations use async/await for non-blocking execution:

```csharp
// Service methods
public async Task<ProductDto?> GetByIdAsync(Guid id)
public async Task<ProductDto> CreateAsync(CreateProductRequest request)

// Controller methods
public async Task<IActionResult> GetById(Guid id)
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)

// Database operations
await _db.Products.ToListAsync();
await _db.SaveChangesAsync();
```

**Benefits of Async/Await:**
- **Non-blocking I/O:** Thread is released while waiting for I/O operations
- **Scalability:** Server can handle more concurrent requests with fewer threads
- **Responsiveness:** UI remains responsive during long-running operations
- **Consistency:** Async throughout the stack prevents blocking calls

### 9.3 Null Safety Pattern

Uses C# nullable reference types (`#nullable enable` in .csproj):

```csharp
// Nullable properties
public string? Description { get; set; }
public string? Brand { get; set; }
public decimal? WeightKg { get; set; }

// Null-coalescing in queries
var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-");

// Null-conditional operators
(p.Description != null && p.Description.ToLower().Contains(lower))
p.Category?.Name

// Null-coalescing with default
var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N")[..8];
```

**Nullable Reference Types Benefits:**
- **Compile-time safety:** Compiler warns about potential null reference exceptions
- **Intent clarity:** `string?` explicitly indicates nullable, `string` indicates non-null
- **Reduced runtime errors:** Catches null-related bugs at compile time

### 9.4 LINQ and Query Patterns

**Fluent LINQ with method chaining:**

```csharp
var products = await _db.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive && p.StockQuantity <= p.ReorderPoint)
    .OrderBy(p => p.StockQuantity)
    .ToListAsync();
```

**Eager Loading:**
```csharp
.Include(p => p.Category)
.ThenInclude(l => l.Product)
```

Eager loading prevents N+1 query problems by loading related entities in a single query.

**Deferred Execution:**
```csharp
var query = _db.Products.AsQueryable();
if (activeOnly)
    query = query.Where(p => p.IsActive);
var products = await query.ToListAsync();  // Execution happens here
```

Deferred execution allows building queries conditionally before execution.

### 9.5 Error Handling Pattern

**Business Rule Validation:**

```csharp
if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
    throw new InvalidOperationException($"A product with SKU '{request.SKU}' already exists.");
```

**Stock Validation:**

```csharp
if (product.StockQuantity < lineReq.Quantity)
    throw new InvalidOperationException(
        $"Insufficient stock for '{product.Name}' (SKU: {product.SKU}). " +
        $"Available: {product.StockQuantity}, Requested: {lineReq.Quantity}");
```

**Controller Exception Handling:**

```csharp
try
{
    var product = await _productService.CreateAsync(request);
    return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
}
catch (InvalidOperationException ex)
{
    _logger.LogWarning("Failed to create product: {Message}", ex.Message);
    return Conflict(new { message = ex.Message });
}
```

**Exception-Based Validation Pattern:**
- Services throw `InvalidOperationException` for business rule violations
- Controllers catch exceptions and map to appropriate HTTP status codes
- Detailed error messages are included in responses for client debugging

### 9.6 Logging Pattern

**Structured Logging with Named Parameters:**

```csharp
_logger.LogInformation("Created product {SKU} - {Name}", product.SKU, product.Name);
_logger.LogInformation("Stock adjusted for {SKU}: {Delta} (Reason: {Reason})", 
    product.SKU, request.Quantity, request.Reason);
_logger.LogWarning("Failed to create product: {Message}", ex.Message);
```

**Structured Logging Benefits:**
- Named parameters are extracted and indexed by logging frameworks
- Enables filtering and searching logs by specific fields
- Improves log analysis and debugging

**Middleware Logging with Correlation ID:**

```csharp
_logger.LogInformation(
    "[{CorrelationId}] {Method} {Path}{Query} started",
    correlationId, context.Request.Method, context.Request.Path, context.Request.QueryString);
```

Correlation IDs enable tracing requests across multiple log entries and services.

**Log Level Consistency:**
- `LogInformation`: Normal operations (product created, order placed)
- `LogWarning`: Unexpected but handled situations (failed creation, validation errors)
- `LogError`: Serious errors requiring investigation

### 9.7 DTO Mapping Pattern

**Expression-based Mapping:**

```csharp
private static ProductDto MapToDto(Product p) => new(
    p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
    p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
    p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
    p.CreatedAt, p.UpdatedAt
);
```

**Benefits:**
- Concise, readable syntax
- Compile-time type safety
- Easy to maintain
- No reflection overhead

**Private Method Encapsulation:**
Mapping methods are private to encapsulate the mapping logic and prevent external code from depending on implementation details.

### 9.8 Record Types for DTOs

All DTOs use C# record types:

```csharp
public record ProductDto(
    Guid Id,
    string Name,
    string SKU,
    // ... more properties
);

public record CreateProductRequest(
    [Required][MaxLength(200)] string Name,
    [Required][MaxLength(100)] string SKU,
    // ... more properties
);
```

**Benefits:**
- **Immutability by default:** Properties cannot be modified after creation
- **Built-in equality comparison:** Automatic `Equals()` and `GetHashCode()`
- **Concise syntax:** Eliminates boilerplate getter/setter code
- **Automatic ToString():** Useful for logging and debugging
- **Validation attributes:** Can be applied directly to parameters

---

## 10. Database Access Patterns

### 10.1 Entity Framework Core Configuration

**Fluent API Configuration (OnModelCreating):**

```csharp
// Unique index on SKU
entity.HasIndex(p => p.SKU).IsUnique();

// Foreign key with delete behavior
entity.HasOne(p => p.Category)
      .WithMany(c => c.Products)
      .HasForeignKey(p => p.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);
```

**Delete Behaviors:**
- **Restrict:** Prevent deletion if related records exist (Products, Categories)
- **Cascade:** Delete related records (OrderLines when Order is deleted)

**Delete Behavior Enforcement:**
- Database enforces constraints at the database level
- Service layer also validates before deletion for better error messages
- Prevents orphaned records and maintains referential integrity

### 10.2 Query Patterns

**Filtering with Include:**

```csharp
var products = await _db.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive)
    .OrderBy(p => p.Name)
    .ToListAsync();
```

**Existence Checks:**

```csharp
if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
    throw new InvalidOperationException(...);
```

**Distinct Queries:**

```csharp
var productIds = request.Lines.Select(l => l.ProductId).Distinct().ToList();
var products = await _db.Products
    .Where(p => productIds.Contains(p.Id) && p.IsActive)
    .ToListAsync();
```

**N+1 Query Prevention:**
The codebase uses `Include()` to eagerly load related entities, preventing N+1 query problems where a query for parent entities results in additional queries for each child entity.

### 10.3 Audit Timestamps

All entities include audit fields:

```csharp
public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
```

**Update Pattern:**

```csharp
product.UpdatedAt = DateTime.UtcNow;
await _db.SaveChangesAsync();
```

**Audit Trail Benefits:**
- Track when entities were created and last modified
- Enable sorting by creation/modification time
- Support audit logging and compliance requirements
- Use UTC to avoid timezone issues

---

## 11. API Response Patterns

### 11.1 Success Responses

**200 OK (GET, PUT):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Wireless Headphones Pro",
  "sku": "ELEC-001",
  "price": 149.99,
  "stockQuantity": 85,
  "categoryId": "550e8400-e29b-41d4-a716-446655440001",
  "categoryName": "Electronics",
  "isLowStock": false,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

**201 Created (POST):**
```
Location: /api/v1/products/550e8400-e29b-41d4-a716-446655440000
```
Response body: Same as 200 OK

**204 No Content (DELETE):**
```
(empty body)
```

### 11.2 Error Responses

**400 Bad Request (Validation Errors):**
```json
{
  "message": "Insufficient stock for 'Wireless Headphones Pro' (SKU: ELEC-001). Available: 5, Requested: 10"
}
```

**404 Not Found:**
```json
{
  "message": "Product 550e8400-e29b-41d4-a716-446655440000 not found."
}
```

**409 Conflict (Business Rule Violations):**
```json
{
  "message": "A product with SKU 'ELEC-001' already exists."
}
```

**Error Response Format:**
All error responses follow a consistent format with a `message` field containing a human-readable error description.

---

## 12. Dependency Injection and Service Lifetime

### 12.1 Service Registration

**Scoped Services (per HTTP request):**

```csharp
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();
```

**DbContext (Scoped):**

```csharp
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));
```

**Built-in Services:**
- `ILogger<T>` - Injected automatically by the framework
- `ILoggerFactory` - Configured via `builder.Logging`

### 12.2 Service Dependencies

**ProductService Dependencies:**
- `InventoryDbContext` - Data access
- `ILogger<ProductService>` - Logging

**OrderService Dependencies:**
- `InventoryDbContext` - Data access
- `ILogger<OrderService>` - Logging
- Static `_orderCounter` - Thread-safe order number generation

**Controller Dependencies:**
- Service instances (ProductService, OrderService, CategoryService)
- `ILogger<T>` - Logging

**Scoped Lifetime Benefits:**
- One instance per HTTP request
- Request isolation prevents state leakage
- DbContext is scoped, ensuring transaction consistency
- Services can safely store request-specific state

---

## 13. Key Features and Business Logic

### 13.1 Stock Management

**Stock Adjustment with Validation:**

```csharp
public void UpdateStock(int quantity)
{
    if (StockQuantity + quantity < 0)
        throw new InvalidOperationException(
            $"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}");
    
    StockQuantity += quantity;
    UpdatedAt = DateTime.UtcNow;
}
```

**Low Stock Detection:**

```csharp
public bool IsLowStock => StockQuantity <= ReorderPoint;

// Query low stock products
var products = await _db.Products
    .Where(p => p.IsActive && p.StockQuantity <= p.ReorderPoint)
    .OrderBy(p => p.StockQuantity)
    .ToListAsync();
```

**Stock Management Features:**
- Prevents negative stock through validation
- Tracks stock changes with audit timestamps
- Identifies low-stock products for reordering
- Supports both positive (restock) and negative (sales) adjustments

### 13.2 Order Processing

**Order Creation with Stock Deduction:**

1. Validate all products exist and are active
2. Check stock availability for all line items
3. Deduct stock for each product: `product.UpdateStock(-quantity)`
4. Create OrderLine records with product snapshot (name, SKU, price)
5. Calculate total: `order.RecalculateTotal()`
6. Save to database

**Order Number Generation:**

```csharp
private static int _orderCounter = 1000;

var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
// Generates: ORD-001001, ORD-001002, etc.
```

Uses `Interlocked.Increment()` for thread-safe counter in multi-threaded environment.

**Thread-Safe Counter Explanation:**
- `Interlocked.Increment()` atomically increments the counter
- Prevents race conditions where multiple threads might generate the same number
- Essential for distributed systems where multiple instances might run

### 13.3 Full-Text Search

**Multi-field Search:**

```csharp
var lower = query.ToLower();
var products = await _db.Products
    .Where(p => p.IsActive && (
        p.Name.ToLower().Contains(lower) ||
        p.SKU.ToLower().Contains(lower) ||
        (p.Description != null && p.Description.ToLower().Contains(lower)) ||
        (p.Brand != null && p.Brand.ToLower().Contains(lower))
    ))
    .OrderBy(p => p.Name)
    .ToListAsync();
```

Searches across: Name, SKU, Description, Brand

**Search Implementation:**
- Case-insensitive matching using `ToLower()`
- Null-safe checks for optional fields
- Returns results ordered by name
- Filters to active products only

### 13.4 Category Management

**Slug Generation:**

```csharp
var slug = request.Slug ?? request.Name.ToLower()
    .Replace(" ", "-")
    .Replace("&", "and");
```

Example: "Food & Beverage" → "food-and-beverage"

**Cascade Delete Prevention:**

```csharp
if (category.Products.Any())
    throw new InvalidOperationException(
        "Cannot delete a category that contains products. Reassign or delete products first.");
```

**Category Constraints:**
- Category names must be unique
- Categories cannot be deleted if they contain products
- Slugs are automatically generated for URL-friendly identifiers

---

## 14. Project Configuration

### 14.1 Project File Configuration

**Main Project (InventoryApi.csproj):**

```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
    <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  </ItemGroup>
</Project>
```

**Test Project (InventoryApi.Tests.csproj):**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
    <PackageReference Include="xunit" Version="2.6.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
    <PackageReference Include="Moq" Version="4.20.69" />
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  </ItemGroup>
</Project>
```

### 14.2 NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.EntityFrameworkCore.InMemory | 9.0.0 | In-memory database for testing and demo |
| Swashbuckle.AspNetCore | 6.5.0 | Swagger/OpenAPI documentation |
| xunit | 2.6.1 | Unit testing framework |
| FluentAssertions | 6.12.0 | Readable assertion syntax |
| Moq | 4.20.69 | Mocking framework (included but not used in tests) |
| Microsoft.NET.Test.Sdk | 17.8.0 | Test execution infrastructure |

---

## 15. Summary of Key Architectural Decisions

| Decision | Rationale | Implementation |
|----------|-----------|-----------------|
| **Layered Architecture** | Clear separation of concerns, testability | Controllers → Services → Data Access |
| **Async/Await Throughout** | Scalable I/O, non-blocking operations | All service and controller methods are async |
| **In-Memory Database** | Zero configuration, fast development | EF Core InMemoryDatabase provider |
| **DTOs for API Contracts** | Decouple API from domain models | Separate request/response records |
| **Scoped Services** | One instance per HTTP request | DI container configuration |
| **Middleware for Logging** | Cross-cutting concern, request tracing | RequestLoggingMiddleware with correlation IDs |
| **Record Types for DTOs** | Immutability, concise syntax | C# 9+ record syntax |
| **Fluent LINQ** | Readable, composable queries | Method chaining with Include/Where/OrderBy |
| **Exception-based Validation** | Clear error handling, business rule enforcement | InvalidOperationException for business rules |
| **Audit Timestamps** | Track entity changes | CreatedAt/UpdatedAt on all entities |
| **Unique Constraints** | Data integrity | SKU and OrderNumber uniqueness indexes |
| **Delete Behavior Configuration** | Referential integrity | Restrict for categories, Cascade for order lines |
| **Thread-Safe Counter** | Prevent duplicate order numbers | Interlocked.Increment for order number generation |
| **Denormalized OrderLines** | Preserve order state | Store product snapshot at time of order |

---

## 16. Code Quality and Testing Coverage

### 16.1 Test Coverage

**ProductServiceTests (13 test cases):**
- CRUD operations: Create, Read, Update, Delete
- Business logic: Stock adjustment, low stock detection, search
- Error handling: Duplicate SKU, invalid category, insufficient stock
- Filtering: Active/inactive products, category filtering

**Test Patterns:**
- Arrange-Act-Assert (AAA)
- Isolated test databases per test
- FluentAssertions for readable assertions
- Exception testing with message validation

**Missing Test Coverage:**
The codebase currently lacks tests for:
- CategoryService (CRUD operations, cascade delete prevention)
- OrderService (order creation, stock deduction, status updates)
- Controllers (HTTP status codes, error responses)

These would be valuable additions to improve test coverage.

### 16.2 Code Quality Practices

**Null Safety:**
- Nullable reference types enabled
- Null-conditional operators (`?.`)
- Null-coalescing operators (`??`)

**Immutability:**
- Record types for DTOs
- Read-only properties where appropriate
- No mutable collections exposed

**Logging:**
- Structured logging with named parameters
- Appropriate log levels (Information, Warning, Error)
- Correlation IDs for request tracing

**Error Handling:**
- Specific exception types (InvalidOperationException)
- Descriptive error messages
- Proper HTTP status codes

---

## 17. API Documentation

### 17.1 Swagger Integration

**Configuration (Program.cs, lines 11-17):**

```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Inventory Management API",
        Version = "v1",
        Description = "A demo ASP.NET Core inventory management REST API with product, category, and order management."
    });
});
```

**Middleware Setup (Program.cs, lines 54-56):**

```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
}
```

**Access:** `http://localhost:5000/swagger`

### 17.2 XML Documentation Comments

Controllers include XML documentation:

```csharp
/// <summary>Get all active products</summary>
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
```

**ProducesResponseType Attributes:**

```csharp
[ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
```

These attributes inform Swagger about possible response types and status codes.

---

## 18. Performance Considerations

### 18.1 N+1 Query Prevention

The codebase uses eager loading with `Include()` to prevent N+1 query problems:

```csharp
var products = await _db.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive)
    .ToListAsync();
```

Without `Include()`, accessing `product.Category` would trigger additional queries for each product.

### 18.2 Async Operations

All I/O operations are async, allowing the thread pool to handle more concurrent requests:

```csharp
var products = await _db.Products.ToListAsync();
await _db.SaveChangesAsync();
```

### 18.3 In-Memory Database

The in-memory database is suitable for development and testing but not for production:
- No persistence across application restarts
- All data is lost when the application stops
- Suitable for demos and prototyping

For production, replace with a real database provider (SQL Server, PostgreSQL, etc.).

---

## 19. Concurrency Handling

### 19.1 Thread-Safe Order Number Generation

The `OrderService` uses `Interlocked.Increment()` for thread-safe counter operations:

```csharp
private static int _orderCounter = 1000;

var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
```

This ensures that even in multi-threaded environments, each order gets a unique number.

### 19.2 Scoped DbContext

The `DbContext` is scoped to each HTTP request, preventing concurrent access issues:

```csharp
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));
```

Each request gets its own isolated context instance.

---

## 20. Future Enhancements

### 20.1 Potential Improvements

**Authentication & Authorization:**
- Add JWT token-based authentication
- Implement role-based access control (Admin, Customer, etc.)
- Protect sensitive endpoints

**Advanced Search:**
- Implement full-text search with Elasticsearch
- Add faceted search for filtering by multiple criteria
- Support complex query syntax

**Inventory Management:**
- Add inventory reservations for pending orders
- Implement automatic reorder notifications
- Support inventory transfers between locations

**Order Management:**
- Add order cancellation with stock restoration
- Implement order refunds
- Add order history and tracking

**API Improvements:**
- Add pagination for large result sets
- Implement filtering and sorting parameters
- Add API rate limiting
- Support batch operations

**Testing:**
- Add integration tests for controllers
- Add tests for CategoryService and OrderService
- Add performance/load testing
- Add end-to-end tests

**Monitoring & Observability:**
- Add distributed tracing with OpenTelemetry
- Implement health checks
- Add metrics collection
- Add alerting for critical issues

**Database:**
- Migrate from in-memory to persistent database
- Add database migrations with EF Core Migrations
- Implement soft deletes for audit trail
- Add database backup/restore procedures

---

## 21. Conclusion

**demo-inventory-csharp** demonstrates a well-structured, production-ready ASP.NET Core API with:

✅ **Clean Architecture:** Clear layering with separation of concerns  
✅ **Async/Await:** Non-blocking I/O throughout  
✅ **Comprehensive Testing:** Unit tests with FluentAssertions  
✅ **Error Handling:** Business rule validation with meaningful messages  
✅ **Logging & Observability:** Structured logging with correlation IDs  
✅ **API Documentation:** Swagger/OpenAPI integration  
✅ **Data Integrity:** Unique constraints, referential integrity, audit timestamps  
✅ **Code Quality:** Nullable reference types, immutable DTOs, LINQ patterns  
✅ **Scalability:** Scoped services, async operations, efficient queries  
✅ **Thread Safety:** Interlocked operations for concurrent scenarios  
✅ **Denormalization Strategy:** Preserves order state through OrderLine snapshots  
✅ **Validation Patterns:** Multi-layer validation (model, service, database)  

The codebase serves as an excellent reference for building inventory management systems and demonstrates ASP.NET Core best practices for modern API development.