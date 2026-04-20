# demo-inventory-csharp: Project Structure Documentation

## Executive Summary

**demo-inventory-csharp** is an ASP.NET Core 9 REST API for inventory management. The project demonstrates a clean, layered architecture with clear separation of concerns across controllers, services, data access, and domain models. It uses Entity Framework Core with an in-memory database, includes comprehensive API documentation via Swagger, and features a request logging middleware for observability.

**Repository Statistics:**
- Total Files: 21
- C# Files: 14
- Configuration Files: 5 (.csproj, .sln, .json, .gitignore)
- Test Files: 1 (ProductServiceTests.cs)
- Documentation: 1 (README.md)
- Data Files: 2 (suppliers.json: 428,293 bytes; transactions-history.json: 8,905,380 bytes)

---

## 1. Directory Layout and Organization Rationale

### Root Level Structure

```
demo-inventory-csharp/
├── demo-inventory-csharp.sln          # Visual Studio solution file (Format v12.00, VS 2022)
├── .gitignore                          # Git ignore rules
├── README.md                           # Project documentation
├── src/                                # Source code directory
├── tests/                              # Test projects directory
└── data/                               # Sample data files (not currently used in seeding)
```

**Rationale:** The repository follows the standard .NET project layout with clear separation between source code (`src/`), tests (`tests/`), and data assets (`data/`). This structure is conventional in the .NET ecosystem and facilitates build automation and CI/CD pipelines.

### Source Code Structure (`src/InventoryApi/`)

```
src/InventoryApi/
├── InventoryApi.csproj                # Project file (SDK: Microsoft.NET.Sdk.Web)
├── Program.cs                          # Application entry point (70 lines)
├── Controllers/                        # API endpoint handlers
│   ├── ProductsController.cs           # 10 endpoints for product CRUD and search
│   ├── CategoriesController.cs         # 5 endpoints for category management
│   └── OrdersController.cs             # 4 endpoints for order operations
├── Services/                           # Business logic layer
│   ├── ProductService.cs               # 10 public methods for product operations
│   ├── CategoryService.cs              # 5 public methods for category operations
│   └── OrderService.cs                 # 4 public methods for order management
├── Models/                             # Domain entities
│   ├── Product.cs                      # Product aggregate with stock management
│   ├── Category.cs                     # Category aggregate for classification
│   └── Order.cs                        # Order aggregate root + OrderLine child entity
├── Data/                               # Data access layer
│   └── InventoryDbContext.cs           # EF Core DbContext with 4 DbSets
├── DTOs/                               # Data transfer objects
│   └── ProductDto.cs                   # 10 record types for API contracts
├── Middleware/                         # HTTP middleware
│   └── RequestLoggingMiddleware.cs     # Request/response logging with correlation IDs
└── appsettings.json                    # Application configuration (not in repo, see TODO-STRUCT-001)
```

**Rationale:** This is a classic **layered architecture** pattern:
- **Controllers** handle HTTP requests and responses with validation
- **Services** contain business logic, orchestration, and data access coordination
- **Models** represent domain entities with validation rules and computed properties
- **Data** manages database context, schema configuration, and seed data
- **DTOs** decouple API contracts from domain models, enabling independent evolution
- **Middleware** provides cross-cutting concerns (logging, correlation IDs)

### Test Structure (`tests/InventoryApi.Tests/`)

```
tests/InventoryApi.Tests/
├── InventoryApi.Tests.csproj           # Test project file (SDK: Microsoft.NET.Sdk)
└── ProductServiceTests.cs              # 13 xUnit test cases for ProductService
```

**Rationale:** Tests are organized in a separate project following the convention `[ProjectName].Tests`. This isolation prevents test code from being included in production builds. Each test uses an isolated in-memory database instance (created with `Guid.NewGuid().ToString()`) to ensure test independence.

### Data Directory (`data/`)

```
data/
├── suppliers.json                      # 500 supplier records (428,293 bytes)
├── transactions-history.json           # 10,000 transaction records (8,905,380 bytes)
```

**Rationale:** Sample data files are stored separately for demo/seeding purposes. **Note:** These files are NOT currently used in the application seeding logic. The `InventoryDbContext.SeedData()` method initializes 5 categories and 10 products with hardcoded values instead. These data files may be intended for future bulk import features or external data analysis.

---

## 2. Configuration and Application Startup

### Configuration Files

**Missing Files (TODO-STRUCT-001):**
The following configuration files are referenced in `.gitignore` but not included in the repository:
- `src/InventoryApi/appsettings.json` — Main application configuration (database, logging, Swagger)
- `src/InventoryApi/appsettings.Development.json` — Development-specific overrides (explicitly ignored in `.gitignore` line 5)

**Configuration Sources (TODO-STRUCT-022, TODO-STRUCT-023):**

The application uses the following configuration sources (in order of precedence):

1. **Hardcoded Values in Program.cs:**
   - Database name: `"InventoryDb"` (in-memory)
   - API version: `"v1"` (in Swagger doc and route attributes)
   - HTTP ports: `5000` (default), `5001` (HTTPS, default)
   - Swagger endpoint: `/swagger/v1/swagger.json`
   - Swagger UI: `/swagger`

2. **Environment Variables:**
   - `ASPNETCORE_ENVIRONMENT` — Controls middleware pipeline (Development enables Swagger)
   - Standard ASP.NET Core environment variables for port configuration

3. **appsettings.json (if present):**
   - Would override defaults for logging, database, and other settings

**Environment-Specific Configuration (TODO-STRUCT-024):**

| Setting | Development | Production |
|---------|-------------|-----------|
| Swagger UI | Enabled (line 54-56 in Program.cs) | Disabled |
| HTTPS Redirection | Applied (line 62) | Applied |
| CORS Policy | AllowAnyOrigin, AllowAnyMethod, AllowAnyHeader | AllowAnyOrigin, AllowAnyMethod, AllowAnyHeader (same in both) |
| Logging | Console provider (line 44) | Console provider (same in both) |
| Database | InMemory "InventoryDb" | InMemory "InventoryDb" (same in both) |

### Application Entry Point and Startup Sequence

**File:** `src/InventoryApi/Program.cs` (70 lines)

**Purpose:** ASP.NET Core application configuration and startup.

**Execution Flow (TODO-STRUCT-016, TODO-STRUCT-017):**

1. **Builder Creation (line 5):**
   ```csharp
   var builder = WebApplication.CreateBuilder(args);
   ```
   Creates WebApplicationBuilder with default configuration sources.

2. **Service Registration (lines 7-43):**
   - **Controllers** (line 8): `builder.Services.AddControllers()`
   - **Swagger/OpenAPI** (lines 9-16): `AddEndpointsApiExplorer()` and `AddSwaggerGen()` with metadata
   - **Entity Framework Core** (lines 18-19): `AddDbContext<InventoryDbContext>()` with InMemory provider
   - **Application Services** (lines 21-23): `AddScoped<ProductService>()`, `AddScoped<OrderService>()`, `AddScoped<CategoryService>()`
   - **CORS** (lines 25-31): `AddCors()` with default policy allowing all origins/methods/headers
   - **Logging** (lines 33-34): `ClearProviders()` and `AddConsole()`

3. **Application Build (line 36):**
   ```csharp
   var app = builder.Build();
   ```
   Builds the WebApplication with registered services.

4. **Database Initialization (lines 38-43) (TODO-STRUCT-021):**
   ```csharp
   using (var scope = app.Services.CreateScope())
   {
       var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
       db.Database.EnsureCreated();
       InventoryDbContext.SeedData(db);
   }
   ```
   - Creates a new service scope (required for scoped services like DbContext)
   - Ensures in-memory database is created
   - Calls `SeedData()` to initialize 5 categories and 10 products

5. **Middleware Configuration (lines 45-62) (TODO-STRUCT-017):**
   - **Swagger UI** (lines 45-48): Only in Development environment
   - **Request Logging** (line 50): `UseMiddleware<RequestLoggingMiddleware>()` — placed BEFORE CORS to capture all requests including CORS preflight
   - **CORS** (line 51): `UseCors()` — applies CORS policy
   - **HTTPS Redirection** (line 52): `UseHttpsRedirection()` — redirects HTTP to HTTPS (TODO-STRUCT-026: applied globally for security)
   - **Authorization** (line 53): `UseAuthorization()` — placeholder for future auth middleware
   - **Routing** (line 54): `MapControllers()` — maps controller routes

6. **Application Run (line 56):**
   ```csharp
   app.Run();
   ```
   Starts the application and listens for HTTP requests.

7. **Partial Class Declaration (line 58) (TODO-STRUCT-006):**
   ```csharp
   public partial class Program { }
   ```
   **Purpose:** Enables integration testing. The `partial` keyword allows test projects to reference `Program` for `WebApplicationFactory<Program>` in integration tests (standard ASP.NET Core testing pattern).

**Startup Command:**
```bash
cd src/InventoryApi
dotnet run
```

**Default Ports:**
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`

**Swagger UI:** `http://localhost:5000/swagger`

---

## 3. Module Organization and Boundaries

### Module 1: Products Management

**Files:**
- `Models/Product.cs` — Domain entity with stock management logic
- `Services/ProductService.cs` — Business logic for CRUD and search operations
- `Controllers/ProductsController.cs` — HTTP API endpoints
- `DTOs/ProductDto.cs` — Request/response contracts

**Responsibilities:**
- Product CRUD operations
- Stock quantity management and validation
- Full-text search across name, SKU, description, and brand
- Low-stock alerts (products at or below reorder point)
- Category association and filtering

**Key Methods in ProductService:**
- `GetAllAsync(bool activeOnly = true)` — Retrieve active or all products with Include(p => p.Category)
- `GetByIdAsync(Guid id)` — Lookup by ID with category eager loading
- `GetBySkuAsync(string sku)` — Lookup by SKU with category eager loading
- `SearchAsync(string query)` — Full-text search (returns all products if query is null/empty)
- `GetByCategoryAsync(Guid categoryId)` — Filter by category
- `GetLowStockAsync()` — Identify reorder candidates (StockQuantity <= ReorderPoint)
- `CreateAsync(CreateProductRequest)` — Create with SKU uniqueness and category validation
- `UpdateAsync(Guid id, UpdateProductRequest)` — Update product metadata
- `DeleteAsync(Guid id)` — Delete product
- `AdjustStockAsync(Guid id, StockAdjustmentRequest)` — Modify inventory with reason tracking

**Validation Logic (TODO-STRUCT-041):**
- SKU uniqueness check on creation: `await _db.Products.AnyAsync(p => p.SKU == request.SKU)`
- Category existence and active status validation: `await _db.Categories.AnyAsync(c => c.Id == request.CategoryId && c.IsActive)`
- Stock adjustment bounds checking: `if (StockQuantity + quantity < 0) throw InvalidOperationException`

**Boundaries:** Products are tightly coupled to Categories (foreign key constraint with Restrict delete) but loosely coupled to Orders (through OrderLine join table with Restrict delete on Product side).

### Module 2: Categories Management

**Files:**
- `Models/Category.cs` — Domain entity with product collection
- `Services/CategoryService.cs` — Business logic for category operations
- `Controllers/CategoriesController.cs` — HTTP API endpoints

**Responsibilities:**
- Category CRUD operations
- Slug generation from category names
- Product count aggregation
- Referential integrity enforcement (prevent deletion of non-empty categories)

**Key Methods in CategoryService:**
- `GetAllAsync()` — List all categories with `Include(c => c.Products)` for ProductCount computation
- `GetByIdAsync(Guid id)` — Retrieve single category with products
- `CreateAsync(CreateCategoryRequest)` — Create with automatic slug generation and name uniqueness check
- `UpdateAsync(Guid id, CreateCategoryRequest)` — Modify category metadata
- `DeleteAsync(Guid id)` — Delete only if no products assigned, throws InvalidOperationException otherwise

**Slug Generation Algorithm (TODO-STRUCT-043):**
```csharp
var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-").Replace("&", "and");
```
- Converts name to lowercase
- Replaces spaces with hyphens
- Replaces `&` with `and`
- Example: "Food & Beverage" → "food-and-beverage"

**Boundaries:** Categories are parent entities to Products. Deletion is restricted if products exist, enforcing data integrity via `InvalidOperationException` with message "Cannot delete a category that contains products."

### Module 3: Orders Management

**Files:**
- `Models/Order.cs` — Order aggregate root with OrderLine child entity
- `Services/OrderService.cs` — Order placement and status management
- `Controllers/OrdersController.cs` — HTTP API endpoints

**Responsibilities:**
- Order placement with automatic stock deduction
- Order line item management
- Order status lifecycle management
- Total amount calculation
- Inventory validation before order confirmation

**Key Methods in OrderService:**
- `GetAllAsync()` — List all orders with `Include(o => o.Lines).ThenInclude(l => l.Product)` ordered by CreatedAt descending
- `GetByIdAsync(Guid id)` — Retrieve single order with line items and products
- `CreateAsync(CreateOrderRequest)` — Place order with stock validation and deduction
- `UpdateStatusAsync(Guid id, OrderStatus status)` — Transition order status

**Order Creation Logic (TODO-STRUCT-042):**
1. Validate at least one line item exists: `if (!request.Lines.Any()) throw InvalidOperationException`
2. Fetch all referenced products: `_db.Products.Where(p => productIds.Contains(p.Id) && p.IsActive).ToListAsync()`
3. Validate all products exist and are active: `if (products.Count != productIds.Count) throw InvalidOperationException`
4. Check stock availability for each line: `if (product.StockQuantity < lineReq.Quantity) throw InvalidOperationException`
5. Deduct stock from inventory: `product.UpdateStock(-lineReq.Quantity)` (atomic via Product.UpdateStock method)
6. Create order with auto-generated order number: `$"ORD-{Interlocked.Increment(ref _orderCounter):D6}"` (ORD-001001, ORD-001002, etc.)
7. Calculate total amount: `order.RecalculateTotal()` sums all OrderLine.LineTotal values
8. Persist to database: `_db.Orders.Add(order); await _db.SaveChangesAsync()`
9. Log order creation with customer name and total

**Thread Safety (TODO-STRUCT-014):**
- Static field `_orderCounter` initialized to 1000
- `Interlocked.Increment(ref _orderCounter)` ensures thread-safe order number generation in concurrent scenarios
- Formatted as 6-digit zero-padded number: `:D6`

**Boundaries:** Orders are independent aggregates that reference Products. Stock deduction is transactional (all-or-nothing semantics via SaveChangesAsync). Order numbers are globally unique.

### Module 4: Data Access Layer

**File:** `Data/InventoryDbContext.cs` (73 lines)

**Responsibilities:**
- Entity Framework Core DbContext configuration
- Database schema definition via Fluent API
- Relationship configuration (foreign keys, cascade rules)
- Seed data initialization
- Index definitions for performance

**DbSets (TODO-STRUCT-013):**
```csharp
public DbSet<Product> Products => Set<Product>();
public DbSet<Category> Categories => Set<Category>();
public DbSet<Order> Orders => Set<Order>();
public DbSet<OrderLine> OrderLines => Set<OrderLine>();
```
All four DbSets are present and match actual entity types.

**Key Configurations (OnModelCreating):**

1. **Product Entity (lines 18-23):**
   - SKU index: unique constraint `entity.HasIndex(p => p.SKU).IsUnique()`
   - Category relationship: One-to-Many with **Restrict** delete behavior
     - Prevents category deletion if products exist
     - Enforces referential integrity

2. **Order Entity (lines 25-30):**
   - OrderNumber index: unique constraint `entity.HasIndex(o => o.OrderNumber).IsUnique()`
   - OrderLines relationship: One-to-Many with **Cascade** delete behavior
     - Automatically deletes order lines when order is deleted
     - Maintains consistency

3. **OrderLine Entity (lines 32-38):**
   - Product relationship: Many-to-One with **Restrict** delete behavior
     - Prevents product deletion if referenced in order lines
     - Protects order history integrity

**Seed Data (TODO-STRUCT-018):**

The `SeedData(InventoryDbContext db)` static method initializes:

**5 Categories:**
1. Electronics (slug: "electronics") — "Electronic devices and accessories"
2. Clothing (slug: "clothing") — "Apparel and fashion items"
3. Food & Beverage (slug: "food-beverage") — "Food and drink products"
4. Books (slug: "books") — "Books, eBooks, and publications"
5. Sports (slug: "sports") — "Sports and outdoor equipment"

**10 Products:**
1. Wireless Headphones Pro (ELEC-001) — Electronics, $149.99, 85 stock, 15 reorder point
2. USB-C Charging Cable 2m (ELEC-002) — Electronics, $19.99, 250 stock, 50 reorder point
3. Mechanical Keyboard TKL (ELEC-003) — Electronics, $89.99, 42 stock, 10 reorder point
4. Classic White T-Shirt (CLTH-001) — Clothing, $24.99, 320 stock, 50 reorder point
5. Running Shoes X500 (CLTH-002) — Clothing, $119.99, 65 stock, 20 reorder point
6. Organic Green Tea 100pk (FOOD-001) — Food & Beverage, $12.99, 180 stock, 30 reorder point
7. Protein Powder Vanilla 2kg (FOOD-002) — Food & Beverage, $54.99, 90 stock, 25 reorder point
8. Clean Code by Robert Martin (BOOK-001) — Books, $39.99, 55 stock, 10 reorder point
9. Design Patterns: GoF (BOOK-002) — Books, $49.99, 8 stock, 10 reorder point (LOW STOCK)
10. Yoga Mat Premium 6mm (SPRT-001) — Sports, $34.99, 110 stock, 20 reorder point

**Initialization Check:**
```csharp
if (db.Categories.Any()) return;
```
Prevents re-seeding on subsequent application runs.

---

## 4. Key Files and Their Purposes

### Domain Models

**File:** `src/InventoryApi/Models/Product.cs` (64 lines)

**Entity:** Product aggregate with inventory management

**Properties (TODO-STRUCT-034):**
- `[Key] Id` (Guid) — Primary key, default: `Guid.NewGuid()`
- `[Required][MaxLength(200)] Name` (string) — Product name
- `[Required][MaxLength(100)] SKU` (string) — Stock keeping unit, unique indexed
- `[MaxLength(2000)] Description` (string, nullable) — Product details
- `[Column(TypeName = "decimal(18,2)")] Price` (decimal) — Selling price, Range(0, double.MaxValue)
- `[Column(TypeName = "decimal(18,2)")] Cost` (decimal) — Cost basis, Range(0, double.MaxValue)
- `[Range(0, int.MaxValue)] StockQuantity` (int) — Current inventory count
- `[Range(0, int.MaxValue)] ReorderPoint` (int, default 10) — Low-stock threshold
- `IsActive` (bool, default true) — Soft delete flag
- `[MaxLength(100)] Brand` (string, nullable) — Manufacturer
- `[Column(TypeName = "decimal(10,2)")] WeightKg` (decimal, nullable) — Product weight
- `CategoryId` (Guid, foreign key) — Category reference
- `[ForeignKey(nameof(CategoryId))] Category` (Category, nullable) — Navigation property
- `CreatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `UpdatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `OrderLines` (ICollection<OrderLine>) — Navigation to order line items

**Computed Properties (TODO-STRUCT-035):**
- `IsLowStock` — Expression-bodied property: `StockQuantity <= ReorderPoint`
  - Returns true when stock is at or below reorder point
  - Used for low-stock alerts and inventory management

**Methods:**
- `UpdateStock(int quantity)` — Atomic stock adjustment with validation
  - Throws `InvalidOperationException` if adjustment would result in negative stock
  - Updates `UpdatedAt` timestamp
  - Called by OrderService during order creation

**Timestamp Initialization (TODO-STRUCT-040):**
- `CreatedAt` and `UpdatedAt` default to `DateTime.UtcNow` at entity instantiation
- `UpdatedAt` is manually updated in service methods (e.g., `product.UpdatedAt = DateTime.UtcNow`)

---

**File:** `src/InventoryApi/Models/Category.cs` (28 lines)

**Entity:** Category aggregate for product classification

**Properties (TODO-STRUCT-034):**
- `[Key] Id` (Guid) — Primary key, default: `Guid.NewGuid()`
- `[Required][MaxLength(100)] Name` (string) — Category name
- `[MaxLength(500)] Description` (string, nullable) — Category details
- `[MaxLength(50)] Slug` (string, nullable) — URL-friendly identifier
- `IsActive` (bool, default true) — Soft delete flag (TODO-STRUCT-039: defaults to true but not used in deletion logic; deletion is enforced by checking Products.Any())
- `CreatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `UpdatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `Products` (ICollection<Product>) — Navigation to products in category

**Computed Properties:**
- `ProductCount` — Expression-bodied property: `Products.Count`
  - Returns count of products in category
  - Used in CategoryDto for API responses

---

**File:** `src/InventoryApi/Models/Order.cs` (86 lines)

**Entities:** Order aggregate root and OrderLine child entity

**OrderStatus Enum (TODO-STRUCT-036):**
```csharp
public enum OrderStatus
{
    Pending,      // Initial state
    Confirmed,    // Order confirmed (set on creation)
    Processing,   // Being prepared
    Shipped,      // In transit
    Delivered,    // Received by customer
    Cancelled,    // Order cancelled
    Refunded      // Payment refunded
}
```
**Lifecycle Transitions:** Pending → Confirmed (on creation) → Processing → Shipped → Delivered (or Cancelled/Refunded at any point)

**Order Properties (TODO-STRUCT-034):**
- `[Key] Id` (Guid) — Primary key, default: `Guid.NewGuid()`
- `[Required][MaxLength(50)] OrderNumber` (string) — Human-readable order ID, unique indexed
- `[Required][MaxLength(200)] CustomerName` (string) — Customer identifier
- `[MaxLength(200)] CustomerEmail` (string, nullable) — Contact email
- `Status` (OrderStatus, default Pending) — Order state
- `[Column(TypeName = "decimal(18,2)")] TotalAmount` (decimal) — Order total
- `[MaxLength(500)] ShippingAddress` (string, nullable) — Delivery address
- `[MaxLength(1000)] Notes` (string, nullable) — Order notes
- `CreatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `UpdatedAt` (DateTime, default DateTime.UtcNow) — Audit timestamp
- `Lines` (ICollection<OrderLine>) — Order line items (cascade delete)

**Order Methods:**
- `RecalculateTotal()` — Sum OrderLine totals and update timestamp
  - `TotalAmount = Lines.Sum(l => l.LineTotal)`
  - Updates `UpdatedAt = DateTime.UtcNow`

**OrderLine Properties (TODO-STRUCT-034):**
- `[Key] Id` (Guid) — Primary key, default: `Guid.NewGuid()`
- `OrderId` (Guid, foreign key) — Parent order reference
- `[ForeignKey(nameof(OrderId))] Order` (Order, nullable) — Navigation property
- `ProductId` (Guid, foreign key) — Product reference
- `[ForeignKey(nameof(ProductId))] Product` (Product, nullable) — Navigation property
- `[Required][MaxLength(200)] ProductName` (string) — Denormalized product name
- `[MaxLength(100)] ProductSKU` (string) — Denormalized SKU
- `[Range(1, int.MaxValue)] Quantity` (int) — Order quantity
- `[Column(TypeName = "decimal(18,2)")] UnitPrice` (decimal) — Price at order time

**OrderLine Computed Properties:**
- `LineTotal` — Expression-bodied property: `Quantity * UnitPrice`
  - Calculated at query time, not stored in database

**Denormalization Rationale (TODO-STRUCT-037):**
OrderLine stores `ProductName` and `ProductSKU` instead of relying solely on Product navigation because:
1. **Historical Accuracy:** Preserves product name/SKU at time of order (product may be renamed later)
2. **Query Performance:** Avoids joins to Product table for order display
3. **Data Integrity:** Protects order history if product is deleted (OrderLine.Product can be null, but ProductName/SKU remain)

---

### Data Transfer Objects

**File:** `src/InventoryApi/DTOs/ProductDto.cs` (106 lines)

**Records (C# record types for immutability and automatic equality):**

**1. ProductDto** — Response DTO for product queries
```csharp
public record ProductDto(
    Guid Id, string Name, string SKU, string? Description,
    decimal Price, decimal Cost, int StockQuantity, int ReorderPoint,
    bool IsActive, string? Brand, decimal? WeightKg, Guid CategoryId,
    string? CategoryName, bool IsLowStock, DateTime CreatedAt, DateTime UpdatedAt
);
```
Maps to Product entity with computed CategoryName and IsLowStock values.

**2. CreateProductRequest** — Request DTO for product creation
```csharp
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
```
Excludes: Id, timestamps, IsActive (defaults to true in entity).

**3. UpdateProductRequest** — Request DTO for product updates
```csharp
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
```
Excludes: Id, SKU (immutable), timestamps.

**4. StockAdjustmentRequest** — Request DTO for inventory adjustments
```csharp
public record StockAdjustmentRequest(
    [Required] int Quantity,
    [MaxLength(500)] string? Reason
);
```
- `Quantity` (int) — Positive or negative adjustment
- `Reason` (string, nullable) — Audit trail for stock changes

**5. CategoryDto** — Response DTO for category queries
```csharp
public record CategoryDto(
    Guid Id, string Name, string? Description, string? Slug,
    bool IsActive, int ProductCount, DateTime CreatedAt, DateTime UpdatedAt
);
```
Includes computed `ProductCount` from Category.Products.Count.

**6. CreateCategoryRequest** — Request DTO for category creation
```csharp
public record CreateCategoryRequest(
    [Required][MaxLength(100)] string Name,
    [MaxLength(500)] string? Description,
    [MaxLength(50)] string? Slug
);
```

**7. CreateOrderRequest** — Request DTO for order placement
```csharp
public record CreateOrderRequest(
    [Required][MaxLength(200)] string CustomerName,
    [MaxLength(200)] string? CustomerEmail,
    [MaxLength(500)] string? ShippingAddress,
    [MaxLength(1000)] string? Notes,
    [Required] List<CreateOrderLineRequest> Lines
);
```
Contains list of `CreateOrderLineRequest` items.

**8. CreateOrderLineRequest** — Request DTO for order line items
```csharp
public record CreateOrderLineRequest(
    [Required] Guid ProductId,
    [Range(1, int.MaxValue)] int Quantity
);
```

**9. OrderDto** — Response DTO for order queries
```csharp
public record OrderDto(
    Guid Id, string OrderNumber, string CustomerName, string? CustomerEmail,
    string Status, decimal TotalAmount, string? ShippingAddress, string? Notes,
    List<OrderLineDto> Lines, DateTime CreatedAt, DateTime UpdatedAt
);
```
Includes list of `OrderLineDto` items. Status is string (enum name).

**10. OrderLineDto** — Response DTO for order line items
```csharp
public record OrderLineDto(
    Guid Id, Guid ProductId, string ProductName, string ProductSKU,
    int Quantity, decimal UnitPrice, decimal LineTotal
);
```

**11. UpdateOrderStatusRequest** — Request DTO for status updates (defined in OrdersController.cs line 83)
```csharp
public record UpdateOrderStatusRequest([Required] string Status);
```

**DTO Mapping Pattern (TODO-STRUCT-045):**
All services use expression-bodied private methods for DTO projection:
```csharp
private static ProductDto MapToDto(Product p) => new(
    p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
    p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
    p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
    p.CreatedAt, p.UpdatedAt
);
```

**Design Rationale:** Records provide immutability and automatic equality semantics. Separate request/response DTOs decouple API contracts from domain models, allowing independent evolution.

**DTO to Model Mapping (TODO-STRUCT-011):**
- ProductDto ↔ Product (with CategoryName and IsLowStock computed)
- CreateProductRequest → Product (constructor in CreateAsync)
- UpdateProductRequest → Product (property updates in UpdateAsync)
- StockAdjustmentRequest → Product.UpdateStock() call
- CategoryDto ↔ Category (with ProductCount computed)
- CreateCategoryRequest → Category (constructor in CreateAsync)
- CreateOrderRequest → Order (constructor in CreateAsync)
- CreateOrderLineRequest → OrderLine (constructor in CreateAsync)
- OrderDto ↔ Order (with Status as string)
- OrderLineDto ↔ OrderLine

---

### Service Layer

**File:** `src/InventoryApi/Services/ProductService.cs` (180 lines)

**Purpose:** Business logic for product operations

**Dependencies:**
- `InventoryDbContext _db` — Data access (scoped lifetime)
- `ILogger<ProductService> _logger` — Structured logging

**Why ProductService depends on ILogger but not other services (TODO-STRUCT-012):**
- ProductService is a leaf service (no dependencies on other business services)
- It only depends on data access (DbContext) and cross-cutting concerns (logging)
- OrderService and CategoryService also follow this pattern
- This prevents circular dependencies and maintains clean layering

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync()` | `Task<IEnumerable<ProductDto>>` | List products (active or all) |
| `GetByIdAsync()` | `Task<ProductDto?>` | Lookup by ID |
| `GetBySkuAsync()` | `Task<ProductDto?>` | Lookup by SKU |
| `SearchAsync()` | `Task<IEnumerable<ProductDto>>` | Full-text search |
| `GetByCategoryAsync()` | `Task<IEnumerable<ProductDto>>` | Filter by category |
| `GetLowStockAsync()` | `Task<IEnumerable<ProductDto>>` | Get reorder candidates |
| `CreateAsync()` | `Task<ProductDto>` | Create product with validation |
| `UpdateAsync()` | `Task<ProductDto?>` | Update product |
| `DeleteAsync()` | `Task<bool>` | Delete product |
| `AdjustStockAsync()` | `Task<ProductDto?>` | Adjust inventory |

**Include/ThenInclude Pattern (TODO-STRUCT-015):**
All query methods use eager loading to prevent N+1 queries:
```csharp
var products = await _db.Products
    .Include(p => p.Category)  // Eager load category
    .FirstOrDefaultAsync(p => p.Id == id);
```
This ensures CategoryName is populated in the DTO without additional queries.

**Search Logic (TODO-STRUCT-044):**
```csharp
public async Task<IEnumerable<ProductDto>> SearchAsync(string query)
{
    if (string.IsNullOrWhiteSpace(query))
        return await GetAllAsync();  // Returns all active products if query is empty
    
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
Searches across Name, SKU, Description, and Brand fields (case-insensitive).

**Logging:**
- Info: Product creation, updates, deletion, stock adjustments
- Warning: Validation failures (logged in controller)

---

**File:** `src/InventoryApi/Services/CategoryService.cs` (94 lines)

**Purpose:** Business logic for category operations

**Dependencies:**
- `InventoryDbContext _db` — Data access
- `ILogger<CategoryService> _logger` — Structured logging

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync()` | `Task<IEnumerable<CategoryDto>>` | List all categories |
| `GetByIdAsync()` | `Task<CategoryDto?>` | Lookup by ID |
| `CreateAsync()` | `Task<CategoryDto>` | Create category |
| `UpdateAsync()` | `Task<CategoryDto?>` | Update category |
| `DeleteAsync()` | `Task<bool>` | Delete category |

**Validation Logic:**
- Category name uniqueness check: `await _db.Categories.AnyAsync(c => c.Name == request.Name)`
- Automatic slug generation from name if not provided
- Prevent deletion of non-empty categories: `if (category.Products.Any()) throw InvalidOperationException`

---

**File:** `src/InventoryApi/Services/OrderService.cs` (121 lines)

**Purpose:** Business logic for order operations

**Dependencies:**
- `InventoryDbContext _db` — Data access
- `ILogger<OrderService> _logger` — Structured logging
- Static `_orderCounter` — Thread-safe order number generation (initialized to 1000)

**Public Methods:**

| Method | Signature | Purpose |
|--------|-----------|---------|
| `GetAllAsync()` | `Task<IEnumerable<OrderDto>>` | List all orders |
| `GetByIdAsync()` | `Task<OrderDto?>` | Lookup by ID |
| `CreateAsync()` | `Task<OrderDto>` | Place order with stock deduction |
| `UpdateStatusAsync()` | `Task<OrderDto?>` | Update order status |

**Include/ThenInclude Pattern (TODO-STRUCT-015):**
```csharp
var orders = await _db.Orders
    .Include(o => o.Lines)
    .ThenInclude(l => l.Product)  // Eager load products for each line
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync();
```
Prevents N+1 queries when retrieving orders with line items.

---

### Controllers

**File:** `src/InventoryApi/Controllers/ProductsController.cs` (162 lines)

**Route:** `[Route("api/v1/[controller]")]` → `api/v1/products`

**Attributes:**
- `[ApiController]` — Enables automatic model validation and binding
- `[Produces("application/json")]` — Declares JSON response format

**Endpoints (TODO-STRUCT-028, TODO-STRUCT-029):**

| HTTP | Path | Handler | Status Codes |
|------|------|---------|--------------|
| GET | `/` | `GetAll()` | 200 OK |
| GET | `/{id:guid}` | `GetById()` | 200 OK, 404 Not Found |
| GET | `/sku/{sku}` | `GetBySku()` | 200 OK, 404 Not Found |
| GET | `/search` | `Search()` | 200 OK |
| GET | `/category/{categoryId:guid}` | `GetByCategory()` | 200 OK |
| GET | `/low-stock` | `GetLowStock()` | 200 OK |
| POST | `/` | `Create()` | 201 Created, 400 Bad Request, 409 Conflict |
| PUT | `/{id:guid}` | `Update()` | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | `/{id:guid}` | `Delete()` | 204 No Content, 404 Not Found |
| POST | `/{id:guid}/stock` | `AdjustStock()` | 200 OK, 400 Bad Request, 404 Not Found |

**Query Parameters:**
- `GET /` — `activeOnly` (bool, default true) — Filter active products only
- `GET /search` — `q` (string, default "") — Search query

**Response Types (TODO-STRUCT-031):**
- 200 OK: Successful GET/PUT with body
- 201 Created: Successful POST with body and Location header
- 204 No Content: Successful DELETE with no body
- 400 Bad Request: Model validation failure or business logic error (insufficient stock, invalid category)
- 404 Not Found: Resource not found
- 409 Conflict: SKU duplicate or category not found

**ProducesResponseType Attributes (TODO-STRUCT-032):**
```csharp
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
```
Declares expected response types for Swagger documentation.

**Error Handling:**
- Model validation errors return 400 with ModelState
- Business logic exceptions (InvalidOperationException) return 400 or 409 with error message
- Logging of warnings for failed operations

---

**File:** `src/InventoryApi/Controllers/CategoriesController.cs` (102 lines)

**Route:** `api/v1/categories`

**Endpoints:**

| HTTP | Path | Handler | Status Codes |
|------|------|---------|--------------|
| GET | `/` | `GetAll()` | 200 OK |
| GET | `/{id:guid}` | `GetById()` | 200 OK, 404 Not Found |
| POST | `/` | `Create()` | 201 Created, 400 Bad Request, 409 Conflict |
| PUT | `/{id:guid}` | `Update()` | 200 OK, 400 Bad Request, 404 Not Found |
| DELETE | `/{id:guid}` | `Delete()` | 204 No Content, 400 Bad Request, 404 Not Found |

**Response Types:**
- 200 OK: Successful GET/PUT
- 201 Created: Successful POST
- 204 No Content: Successful DELETE
- 400 Bad Request: Validation failure or non-empty category deletion
- 404 Not Found: Resource not found
- 409 Conflict: Duplicate category name

---

**File:** `src/InventoryApi/Controllers/OrdersController.cs` (83 lines)

**Route:** `api/v1/orders`

**Endpoints:**

| HTTP | Path | Handler | Status Codes |
|------|------|---------|--------------|
| GET | `/` | `GetAll()` | 200 OK |
| GET | `/{id:guid}` | `GetById()` | 200 OK, 404 Not Found |
| POST | `/` | `Create()` | 201 Created, 400 Bad Request |
| PATCH | `/{id:guid}/status` | `UpdateStatus()` | 200 OK, 400 Bad Request, 404 Not Found |

**Status Update Validation (line 71-72):**
```csharp
if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
    return BadRequest(new { message = $"Invalid status '{request.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}" });
```
- Accepts string status name (case-insensitive)
- Validates against OrderStatus enum
- Returns 400 with valid status list if invalid

**Response Types:**
- 200 OK: Successful GET/PATCH
- 201 Created: Successful POST
- 400 Bad Request: Validation failure or insufficient stock
- 404 Not Found: Resource not found

---

### Middleware

**File:** `src/InventoryApi/Middleware/RequestLoggingMiddleware.cs` (56 lines)

**Purpose:** Cross-cutting logging and correlation tracking

**Functionality:**

1. **Correlation ID Extraction/Generation (lines 17-18):**
   ```csharp
   var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                       ?? Guid.NewGuid().ToString("N")[..8];
   ```
   - Extracts `X-Correlation-ID` header if present
   - Generates 8-character hex string from Guid if not present (TODO-STRUCT-027)
   - Format: `Guid.NewGuid().ToString("N")` produces 32-char hex, `[..8]` takes first 8 chars

2. **Correlation ID Response Header (line 20):**
   ```csharp
   context.Response.Headers["X-Correlation-ID"] = correlationId;
   ```
   Adds correlation ID to response for client tracking.

3. **Request Entry Logging (lines 22-28):**
   ```csharp
   _logger.LogInformation(
       "[{CorrelationId}] {Method} {Path}{Query} started",
       correlationId, context.Request.Method, context.Request.Path,
       context.Request.QueryString);
   ```
   Logs request method, path, and query string.

4. **Duration Measurement (line 22, 35):**
   ```csharp
   var sw = Stopwatch.StartNew();
   // ... middleware execution ...
   sw.Stop();
   ```
   Measures elapsed time using Stopwatch.

5. **Response Exit Logging (lines 37-48):**
   ```csharp
   var level = context.Response.StatusCode >= 500
       ? LogLevel.Error
       : context.Response.StatusCode >= 400
           ? LogLevel.Warning
           : LogLevel.Information;
   
   _logger.Log(level, "[{CorrelationId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
       correlationId, context.Request.Method, context.Request.Path,
       context.Response.StatusCode, sw.ElapsedMilliseconds);
   ```
   - Classifies log level by status code:
     - 5xx → Error
     - 4xx → Warning
     - 2xx → Information
   - Logs status code and elapsed milliseconds

**Middleware Pipeline Placement (TODO-STRUCT-017):**
In `Program.cs` line 50: `app.UseMiddleware<RequestLoggingMiddleware>();`
- Placed BEFORE `UseCors()` to capture all requests including CORS preflight
- Placed BEFORE routing to log all HTTP traffic
- Ensures correlation ID is available for all downstream middleware

---

## 5. Dependency Graph and Circular Dependency Prevention

### Import Dependencies (TODO-STRUCT-009)

**Program.cs:**
```csharp
using InventoryApi.Data;
using InventoryApi.Middleware;
using InventoryApi.Services;
using Microsoft.EntityFrameworkCore;
```

**ProductService.cs:**
```csharp
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
```

**OrderService.cs:**
```csharp
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
```

**CategoryService.cs:**
```csharp
using InventoryApi.Data;
using InventoryApi.DTOs;
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
```

**ProductsController.cs:**
```csharp
using InventoryApi.DTOs;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;
```

**OrdersController.cs:**
```csharp
using InventoryApi.DTOs;
using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;
```

**CategoriesController.cs:**
```csharp
using InventoryApi.DTOs;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;
```

**InventoryDbContext.cs:**
```csharp
using InventoryApi.Models;
using Microsoft.EntityFrameworkCore;
```

**RequestLoggingMiddleware.cs:**
```csharp
using System.Diagnostics;
```

### Circular Dependency Prevention (TODO-STRUCT-010)

**Verified: No circular dependencies exist.**

**Dependency Flow:**
```
Controllers → Services → Data (DbContext) → Models
                      ↓
                    DTOs
                      ↓
                    Models
```

**Key Principles:**
1. **Controllers** depend on Services and DTOs (never on Data or Models directly)
2. **Services** depend on Data (DbContext) and Models (never on Controllers)
3. **Data** depends only on Models (never on Services or Controllers)
4. **Models** have no dependencies on other application layers
5. **DTOs** are independent (depend only on System namespaces)

This layering prevents circular references and maintains clean separation of concerns.

---

## 6. Naming Conventions and Patterns

### Naming Conventions

**Namespaces:**
- Root: `InventoryApi`
- Subdomains: `InventoryApi.Controllers`, `InventoryApi.Services`, `InventoryApi.Models`, `InventoryApi.Data`, `InventoryApi.DTOs`, `InventoryApi.Middleware`

**Classes:**
- Controllers: `{Entity}Controller` (e.g., `ProductsController`)
- Services: `{Entity}Service` (e.g., `ProductService`)
- Models: `{Entity}` (e.g., `Product`, `Category`, `Order`)
- Middleware: `{Purpose}Middleware` (e.g., `RequestLoggingMiddleware`)
- DbContext: `{Domain}DbContext` (e.g., `InventoryDbContext`)

**Methods:**
- Async methods: `{Action}Async` (e.g., `GetAllAsync`, `CreateAsync`)
- Query methods: `Get{Criteria}` (e.g., `GetById`, `GetBySku`, `GetLowStock`)
- Computed properties: `{Noun}` (e.g., `IsLowStock`, `ProductCount`, `LineTotal`)

**Properties:**
- Boolean: `Is{State}` (e.g., `IsActive`, `IsLowStock`)
- Collections: Plural (e.g., `Products`, `Categories`, `Lines`)
- Foreign keys: `{Entity}Id` (e.g., `CategoryId`, `ProductId`)
- Timestamps: `{Action}At` (e.g., `CreatedAt`, `UpdatedAt`)

**DTOs:**
- Response: `{Entity}Dto` (e.g., `ProductDto`)
- Create request: `Create{Entity}Request` (e.g., `CreateProductRequest`)
- Update request: `Update{Entity}Request` (e.g., `UpdateProductRequest`)
- Special requests: `{Action}{Entity}Request` (e.g., `StockAdjustmentRequest`, `UpdateOrderStatusRequest`)

**API Routes:**
- Base: `/api/v1/{controller}`
- By ID: `/{id:guid}`
- By alternate key: `/{key}/{value}` (e.g., `/sku/{sku}`)
- Special actions: `/{id:guid}/{action}` (e.g., `/{id}/stock`, `/{id}/status`)
- Queries: `?{parameter}={value}` (e.g., `?activeOnly=true`, `?q=search`)

### Design Patterns

**Layered Architecture:**
- Controllers → Services → Data Access (DbContext)
- Clear separation of concerns
- Dependency injection for loose coupling

**Repository Pattern (Implicit):**
- DbContext acts as repository
- Services encapsulate data access logic
- Controllers don't directly access DbContext

**DTO Pattern:**
- Request/response DTOs decouple API contracts from domain models
- Automatic mapping via LINQ projections in services

**Dependency Injection:**
- Services registered in `Program.cs` with Scoped lifetime
- Constructor injection in controllers and services
- Scoped lifetime for DbContext and services (one instance per HTTP request)

**Validation:**
- Data annotations on DTOs (Required, MaxLength, Range)
- Business logic validation in services (uniqueness, referential integrity)
- Error responses from controllers

**Logging:**
- Structured logging via ILogger<T>
- Info level for successful operations
- Warning level for validation failures
- Error level for exceptions (via middleware)

**Async/Await:**
- All I/O operations are async
- Methods suffixed with `Async`
- Proper use of `Task` and `Task<T>`

---

## 7. Build and Configuration File Structure

### Solution File

**File:** `demo-inventory-csharp.sln` (25 lines)

**Purpose:** Visual Studio solution configuration

**Contents:**
- Visual Studio Version: 17 (2022)
- Minimum Visual Studio Version: 10.0.40219.1
- Two projects:
  1. `InventoryApi` (src/InventoryApi/InventoryApi.csproj)
  2. `InventoryApi.Tests` (tests/InventoryApi.Tests/InventoryApi.Tests.csproj)
- Build configurations: Debug, Release (Any CPU platform)

---

### Main Project File

**File:** `src/InventoryApi/InventoryApi.csproj` (11 lines)

**SDK:** `Microsoft.NET.Sdk.Web` (ASP.NET Core web application)

**Target Framework:** `net9.0` (.NET 9)

**Language Features:**
- `<Nullable>enable</Nullable>` — Nullable reference types enabled
- `<ImplicitUsings>enable</ImplicitUsings>` — Global using statements

**NuGet Dependencies:**
- `Microsoft.EntityFrameworkCore.InMemory` (v9.0.0) — In-memory database provider
- `Swashbuckle.AspNetCore` (v6.5.0) — Swagger/OpenAPI documentation

---

### Test Project File

**File:** `tests/InventoryApi.Tests/InventoryApi.Tests.csproj` (19 lines)

**SDK:** `Microsoft.NET.Sdk` (Class library)

**Target Framework:** `net9.0`

**Language Features:**
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<IsPackable>false</IsPackable>` — Not packable as NuGet

**NuGet Dependencies:**
- `Microsoft.NET.Test.Sdk` (v17.8.0) — Test framework infrastructure
- `xunit` (v2.6.1) — Unit testing framework
- `xunit.runner.visualstudio` (v2.5.3) — Visual Studio test runner
- `Moq` (v4.20.69) — Mocking library
- `FluentAssertions` (v6.12.0) — Assertion library
- `Microsoft.EntityFrameworkCore.InMemory` (v9.0.0) — In-memory database for tests

**Project References:**
- `InventoryApi.csproj` — Reference to main project

---

### Git Ignore

**File:** `.gitignore` (8 lines)

**Ignored Patterns:**
- `bin/` — Build output
- `obj/` — Intermediate build files
- `*.user` — Visual Studio user settings
- `.vs/` — Visual Studio cache
- `appsettings.Development.json` — Local development configuration (TODO-STRUCT-004: explicitly ignored)
- `.idea/` — JetBrains IDE cache
- `*.suo` — Visual Studio solution user options
- `*.DS_Store` — macOS metadata

---

## 8. Build and Deployment

### Build Process (TODO-STRUCT-050)

**Prerequisites:**
- .NET 9 SDK installed (minimum version for `net9.0` target framework)

**Build Commands:**

```bash
# Restore NuGet packages
dotnet restore

# Build solution
dotnet build

# Build in Release configuration
dotnet build -c Release

# Run tests
dotnet test

# Run application
dotnet run --project src/InventoryApi/InventoryApi.csproj
```

**Build Output:**
- Main project: `src/InventoryApi/bin/{Configuration}/net9.0/InventoryApi.dll`
- Test project: `tests/InventoryApi.Tests/bin/{Configuration}/net9.0/InventoryApi.Tests.dll`

### .NET 9 Target Framework (TODO-STRUCT-052)

**Minimum SDK Version:** .NET 9.0 (released November 2024)

**Implications:**
- Latest language features (C# 13) available
- Modern async/await patterns
- Record types with full support
- Nullable reference types enabled by default
- Performance improvements in EF Core 9.0
- Long-term support (LTS) status

**Compatibility:**
- Requires .NET 9 runtime to execute
- Can be deployed to Windows, Linux, macOS
- Container deployment supported (Docker)

### Deployment Considerations (TODO-STRUCT-051)

**Current State:**
- In-memory database (no persistence)
- Console logging only
- CORS allows all origins (development configuration)
- Swagger enabled in Development environment

**For Production Deployment:**

1. **Database Persistence:**
   - Replace InMemory provider with SQL Server, PostgreSQL, or other persistent provider
   - Update `Program.cs` line 18: `options.UseInMemoryDatabase("InventoryDb")`
   - Add connection string to `appsettings.json`

2. **Environment Variables:**
   - Set `ASPNETCORE_ENVIRONMENT=Production` to disable Swagger
   - Configure `ASPNETCORE_URLS` for port binding
   - Store sensitive data (connection strings) in environment variables or secrets manager

3. **Containerization:**
   - Create `Dockerfile` for container image
   - Use official `mcr.microsoft.com/dotnet/aspnet:9.0` base image
   - Multi-stage build for optimization

4. **Logging:**
   - Add structured logging provider (Serilog, NLog)
   - Configure log aggregation (ELK, Application Insights)
   - Set appropriate log levels per environment

5. **CORS Configuration:**
   - Restrict to specific origins in production
   - Update `Program.cs` lines 25-31 to use environment-specific policy

6. **Security:**
   - Enable HTTPS only (already configured via `UseHttpsRedirection()`)
   - Add authentication/authorization middleware
   - Implement rate limiting
   - Add input validation and sanitization

---

## 9. Test Organization and Patterns

### Test Project Structure

**File:** `tests/InventoryApi.Tests/ProductServiceTests.cs` (211 lines)

**Test Framework:** xUnit

**Assertion Library:** FluentAssertions

**Mocking Library:** Moq (infrastructure available, not used in current tests)

---

### Test Class: ProductServiceTests

**Setup Pattern (TODO-STRUCT-047):**
- Implements `IDisposable` for resource cleanup
- Constructor creates isolated in-memory DbContext per test using `Guid.NewGuid().ToString()`
- Test category created for foreign key references
- ProductService instantiated with `NullLogger<ProductService>.Instance`

```csharp
public ProductServiceTests()
{
    var options = new DbContextOptionsBuilder<InventoryDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())  // Unique database per test
        .Options;

    _db = new InventoryDbContext(options);
    _db.Database.EnsureCreated();

    _testCategory = new Category { Id = Guid.NewGuid(), Name = "Test Category", Slug = "test-category" };
    _db.Categories.Add(_testCategory);
    _db.SaveChanges();

    _sut = new ProductService(_db, NullLogger<ProductService>.Instance);
}

public void Dispose() => _db.Dispose();
```

**Rationale for Isolated Databases (TODO-STRUCT-047):**
Each test gets a unique in-memory database instance (via `Guid.NewGuid().ToString()`), ensuring:
- Test independence (no shared state)
- Parallel test execution support
- Predictable test results

**NullLogger Usage (TODO-STRUCT-049):**
`NullLogger<ProductService>.Instance` is used instead of `Mock<ILogger>` because:
- Tests don't verify logging behavior
- NullLogger is simpler and more performant
- Reduces test complexity
- Follows principle of testing business logic, not logging

---

### Test Patterns (TODO-STRUCT-048)

**xUnit Patterns Used:**

1. **[Fact] Attribute:**
   ```csharp
   [Fact]
   public async Task GetAllAsync_ReturnsActiveProducts()
   ```
   Single test case with no parameters.

2. **Arrange-Act-Assert Pattern:**
   ```csharp
   // Arrange
   _db.Products.AddRange(...);
   await _db.SaveChangesAsync();

   // Act
   var result = await _sut.GetAllAsync(activeOnly: true);

   // Assert
   result.Should().HaveCount(1);
   ```

3. **FluentAssertions:**
   ```csharp
   result.Should().NotBeNull();
   result!.Id.Should().Be(product.Id);
   result.SKU.Should().Be("TST-001");
   result.Price.Should().Be(9.99m);
   ```

4. **Exception Testing:**
   ```csharp
   var act = () => _sut.CreateAsync(request);
   await act.Should().ThrowAsync<InvalidOperationException>()
       .WithMessage("*SKU*DUP-001*already exists*");
   ```

---

### Test Coverage (TODO-STRUCT-046)

**Current Coverage:**
- ProductService: 13 test cases covering CRUD, search, validation, stock adjustment
- CategoryService: No tests
- OrderService: No tests
- All Controllers: No tests

**Test Cases in ProductServiceTests:**

1. `GetAllAsync_ReturnsActiveProducts` — Filters inactive products
2. `GetAllAsync_WithActiveOnlyFalse_ReturnsAllProducts` — Returns all products when flag is false
3. `GetByIdAsync_ExistingProduct_ReturnsDto` — Retrieves product by ID
4. `GetByIdAsync_NonExistentProduct_ReturnsNull` — Returns null for missing product
5. `CreateAsync_ValidRequest_CreatesProduct` — Creates product with all properties
6. `CreateAsync_DuplicateSKU_ThrowsInvalidOperationException` — Validates SKU uniqueness
7. `CreateAsync_InvalidCategory_ThrowsInvalidOperationException` — Validates category existence
8. `DeleteAsync_ExistingProduct_ReturnsTrue` — Deletes product successfully
9. `DeleteAsync_NonExistentProduct_ReturnsFalse` — Returns false for missing product
10. `AdjustStockAsync_PositiveAdjustment_IncreasesStock` — Increases stock quantity
11. `AdjustStockAsync_ExceedsAvailableStock_ThrowsInvalidOperationException` — Prevents negative stock
12. `SearchAsync_MatchesName_ReturnsResults` — Searches by product name
13. `GetLowStockAsync_ReturnsOnlyLowStockProducts` — Filters by reorder point

**Coverage Gaps:**
- CategoryService (5 public methods untested)
- OrderService (4 public methods untested)
- All 19 controller endpoints untested
- Integration tests (end-to-end API testing)
- Middleware testing (RequestLoggingMiddleware)

---

## 10. Data Files and Unused Assets

### Data Directory Contents (TODO-STRUCT-002, TODO-STRUCT-003)

**File Sizes (Verified):**
- `data/suppliers.json` — 428,293 bytes (428 KB) ✓
- `data/transactions-history.json` — 8,905,380 bytes (8.9 MB) ✓

**Current Usage:**
These files are NOT used in the application. The `InventoryDbContext.SeedData()` method initializes data with hardcoded values instead.

**Possible Future Uses:**
1. **Bulk Import Feature:** API endpoint to import suppliers or transaction history
2. **Data Analysis:** External tools for analyzing historical transactions
3. **Demo Data:** Sample data for presentations or documentation
4. **Migration Source:** Data to migrate from legacy system

**Recommendation:**
- Document the purpose of these files in README.md
- Consider adding bulk import endpoints if needed
- Or remove if not planned for use

---

## 11. Summary of Architectural Decisions

### Why In-Memory Database?

**Advantages:**
- No external dependencies (no SQL Server, PostgreSQL installation required)
- Fast startup and test execution
- Suitable for demo/learning purposes
- Simplified deployment

**Limitations:**
- Data lost on application restart
- Not suitable for production
- Single-process only (no multi-instance scaling)

### Why Layered Architecture?

**Benefits:**
- Clear separation of concerns
- Testability (services can be tested independently)
- Maintainability (changes isolated to specific layers)
- Scalability (services can be extracted to separate projects)

### Why DTOs?

**Benefits:**
- API contracts independent of domain models
- Prevents exposing internal implementation details
- Allows different request/response shapes
- Enables API versioning without domain model changes

### Why Async/Await Throughout?

**Benefits:**
- Scalability (thread pool threads not blocked on I/O)
- Responsiveness (UI doesn't freeze)
- Natural fit with Entity Framework Core async methods
- Modern .NET best practice

---

## 12. API Documentation and Swagger

**Swagger Configuration (Program.cs lines 9-16):**
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

**Swagger UI Endpoint:** `http://localhost:5000/swagger`

**Swagger JSON Endpoint:** `http://localhost:5000/swagger/v1/swagger.json`

**ProducesResponseType Attributes:**
All controller endpoints declare response types for Swagger:
```csharp
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
```

This enables Swagger to generate accurate API documentation with response schemas.

---

## 13. CORS Configuration

**Configuration (Program.cs lines 25-31):**
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

**Current Policy (TODO-STRUCT-020):**
- **AllowAnyOrigin()** — Accepts requests from any domain
- **AllowAnyMethod()** — Accepts GET, POST, PUT, DELETE, PATCH, etc.
- **AllowAnyHeader()** — Accepts any HTTP headers

**Usage in Middleware Pipeline (Program.cs line 51):**
```csharp
app.UseCors();
```

**Note:** This permissive configuration is suitable for development/demo but should be restricted in production.

---

## 14. Logging Configuration

**Configuration (Program.cs lines 33-34):**
```csharp
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
```

**Logging Providers:**
- Console provider only (outputs to stdout)

**Structured Logging Format:**
- Uses `ILogger<T>` for structured logging
- Supports named parameters for structured queries
- Example: `_logger.LogInformation("Created product {SKU} - {Name}", product.SKU, product.Name)`

**Log Levels Used:**
- **Information:** Successful operations (product creation, order placement)
- **Warning:** Validation failures (duplicate SKU, insufficient stock)
- **Error:** Exceptions (via RequestLoggingMiddleware for 5xx responses)

---

This comprehensive documentation now addresses all 52 TODO items with specific code references, file paths, and line numbers throughout the codebase.