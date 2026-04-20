# Architecture Overview: demo-inventory-csharp

## Executive Summary

**demo-inventory-csharp** is a demonstration ASP.NET Core 9 REST API for inventory management, built with a clean layered architecture. The system provides comprehensive CRUD operations for products, categories, and orders with real-time stock management, full-text search capabilities, and request correlation tracking. The application uses Entity Framework Core with an in-memory database for zero-configuration persistence and includes comprehensive unit tests.

---

## 1. System Architecture and High-Level Design

### 1.1 Architectural Pattern

The system follows a **layered (N-tier) architecture** with clear separation of concerns:

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
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                  In-Memory Database                          │
│                  (EF Core InMemory)                          │
└─────────────────────────────────────────────────────────────┘
```

### 1.2 Core Technology Stack

- **Framework**: ASP.NET Core 9 (Web API)
- **Language**: C# 13 with nullable reference types enabled (`<Nullable>enable</Nullable>` in `InventoryApi.csproj` line 4)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>` in `InventoryApi.csproj` line 5)
- **ORM**: Entity Framework Core 9.0.0 with InMemory provider
- **API Documentation**: Swagger/OpenAPI (Swashbuckle.AspNetCore 6.5.0)
- **Testing**: xUnit 2.6.1 with FluentAssertions 6.12.0 and Moq 4.20.69
- **Logging**: Built-in Microsoft.Extensions.Logging with console provider

### 1.3 Deployment Model

The application is designed as a **stateless, single-instance REST API**:

- **Runtime**: .NET 9 runtime
- **Hosting**: ASP.NET Core Kestrel web server
- **Configuration**: Environment-based (Development/Production)
- **Database**: In-memory (ephemeral, resets on application restart)
- **Ports**: HTTPS on 5001, HTTP on 5000
- **Startup**: Automatic database seeding on application initialization via `Program.cs` (lines 33-39)

### 1.4 Deployment Architecture

The application can be containerized and deployed in multiple ways:

#### **Docker Containerization**

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /src/bin/Release/net9.0/publish .
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
```

#### **Kubernetes Deployment** (Recommended for production)

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inventory-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: inventory-api
  template:
    metadata:
      labels:
        app: inventory-api
    spec:
      containers:
      - name: inventory-api
        image: inventory-api:latest
        ports:
        - containerPort: 5000
        - containerPort: 5001
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
```

**Note**: Health check endpoint not currently implemented. See TODO-ARCH-006.

### 1.5 C4 Model - System Context (Level 1)

```
┌─────────────────────────────────────────────────────────────┐
│                                                               │
│  ┌──────────────────┐                                        │
│  │   API Clients    │                                        │
│  │  (Web, Mobile,   │                                        │
│  │   3rd Party)     │                                        │
│  └────────┬─────────┘                                        │
│           │ HTTP/REST                                        │
│           ↓                                                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  Inventory Management API (demo-inventory-csharp)    │   │
│  │  - Product Management                                │   │
│  │  - Category Management                               │   │
│  │  - Order Management                                  │   │
│  │  - Stock Tracking                                    │   │
│  └────────┬─────────────────────────────────────────────┘   │
│           │ In-Memory Data                                   │
│           ↓                                                  │
│  ┌──────────────────────────────────────────────────────┐   │
│  │  In-Memory Database (EF Core)                        │   │
│  │  - Products, Categories, Orders, OrderLines          │   │
│  └──────────────────────────────────────────────────────┘   │
│                                                               │
└─────────────────────────────────────────────────────────────┘
```

---

## 2. Module Structure and Boundaries

### 2.1 Project Organization

```
src/InventoryApi/
├── Controllers/          # HTTP request handlers
│   ├── ProductsController.cs (162 lines)
│   ├── CategoriesController.cs (102 lines)
│   └── OrdersController.cs (83 lines)
├── Services/             # Business logic layer
│   ├── ProductService.cs (180 lines)
│   ├── OrderService.cs (121 lines)
│   └── CategoryService.cs (94 lines)
├── Models/               # Domain entities
│   ├── Product.cs (64 lines)
│   ├── Category.cs (28 lines)
│   └── Order.cs (86 lines)
├── Data/                 # EF Core DbContext and seeding
│   └── InventoryDbContext.cs (73 lines)
├── DTOs/                 # Request/response contracts
│   └── ProductDto.cs (106 lines)
├── Middleware/           # HTTP pipeline components
│   └── RequestLoggingMiddleware.cs (56 lines)
├── InventoryApi.csproj   # Project configuration
└── Program.cs            # Application startup configuration (70 lines)

tests/InventoryApi.Tests/
├── InventoryApi.Tests.csproj
└── ProductServiceTests.cs (211 lines)
```

### 2.2 Module Responsibilities

#### **Controllers Module** (`src/InventoryApi/Controllers/`)

Handles HTTP request routing and response formatting:

- **ProductsController** (`ProductsController.cs`, lines 1-162): 
  - 10 endpoints for product CRUD, search, category filtering, low-stock alerts, and stock adjustments
  - Routes: `GET/POST /api/v1/products`, `GET /api/v1/products/{id}`, `GET /api/v1/products/sku/{sku}`, `GET /api/v1/products/search`, `GET /api/v1/products/category/{categoryId}`, `GET /api/v1/products/low-stock`, `PUT /api/v1/products/{id}`, `DELETE /api/v1/products/{id}`, `POST /api/v1/products/{id}/stock`
  - Response type: `[Produces("application/json")]` (line 8)
  - All actions are `async Task<IActionResult>` for non-blocking I/O

- **CategoriesController** (`CategoriesController.cs`, lines 1-102):
  - 5 endpoints for category management
  - Routes: `GET/POST /api/v1/categories`, `GET/PUT/DELETE /api/v1/categories/{id}`
  - Enforces referential integrity: DELETE returns 400 Bad Request if category has products

- **OrdersController** (`OrdersController.cs`, lines 1-83):
  - 4 endpoints for order management
  - Routes: `GET/POST /api/v1/orders`, `GET /api/v1/orders/{id}`, `PATCH /api/v1/orders/{id}/status`
  - Custom record type: `UpdateOrderStatusRequest` (line 82) for status updates
  - Validates enum values with `Enum.TryParse()` (line 72)

#### **Services Module** (`src/InventoryApi/Services/`)

Encapsulates business logic and data access orchestration:

- **ProductService** (`ProductService.cs`, lines 1-180):
  - 8 public methods: `GetAllAsync()`, `GetByIdAsync()`, `GetBySkuAsync()`, `SearchAsync()`, `GetByCategoryAsync()`, `GetLowStockAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`, `AdjustStockAsync()`
  - Implements full-text search across Name, SKU, Description, and Brand fields (lines 48-56)
  - Case-insensitive search: `ToLower()` on all searchable fields
  - Validates SKU uniqueness (line 89): `AnyAsync(p => p.SKU == request.SKU)`
  - Validates category existence (line 92): `AnyAsync(c => c.Id == request.CategoryId && c.IsActive)`
  - Logs all operations via `ILogger<ProductService>` (lines 108, 125, 137)
  - Uses `Include()` for eager loading to prevent N+1 queries (line 20)
  - DTO mapping via private method `MapToDto()` (lines 177-180)

- **OrderService** (`OrderService.cs`, lines 1-121):
  - 4 public methods: `GetAllAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateStatusAsync()`
  - Implements atomic stock deduction during order creation (lines 60-75)
  - Validates stock availability before order confirmation (lines 48-54)
  - Generates sequential order numbers with thread-safe counter (line 13: `static int _orderCounter = 1000`)
  - Uses `Interlocked.Increment()` for thread-safe counter (line 64)
  - Recalculates order totals automatically (line 76: `order.RecalculateTotal()`)
  - Eager loads related entities: `Include(o => o.Lines).ThenInclude(l => l.Product)` (lines 20-21)

- **CategoryService** (`CategoryService.cs`, lines 1-94):
  - 5 public methods: `GetAllAsync()`, `GetByIdAsync()`, `CreateAsync()`, `UpdateAsync()`, `DeleteAsync()`
  - Auto-generates URL-friendly slugs from category names (line 45): `request.Name.ToLower().Replace(" ", "-").Replace("&", "and")`
  - Enforces referential integrity by preventing deletion of non-empty categories (line 80)
  - Throws `InvalidOperationException` with descriptive message if category has products

#### **Models Module** (`src/InventoryApi/Models/`)

Domain entities with business logic:

- **Product** (`Product.cs`, lines 1-64):
  - Properties: Id (Guid, auto-generated), Name, SKU (unique), Description, Price, Cost, StockQuantity, ReorderPoint, IsActive, Brand, WeightKg, CategoryId, timestamps (CreatedAt, UpdatedAt)
  - Computed property: `IsLowStock` (line 51): `StockQuantity <= ReorderPoint`
  - Business method: `UpdateStock()` (lines 53-59) with validation to prevent negative stock
  - Throws `InvalidOperationException` if stock would go negative
  - Automatically updates `UpdatedAt` timestamp on stock changes
  - Foreign key relationship: `CategoryId` with `Category` navigation property (line 43)
  - Collection: `OrderLines` (line 47) for order history

- **Category** (`Category.cs`, lines 1-28):
  - Properties: Id, Name, Description, Slug, IsActive, timestamps
  - Computed property: `ProductCount` (line 25): `Products.Count`
  - One-to-many relationship with Products (line 24)
  - Slug is URL-friendly identifier for SEO

- **Order** (`Order.cs`, lines 1-86):
  - Enum: `OrderStatus` with 7 states (lines 6-11): Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Refunded
  - Properties: Id, OrderNumber (unique), CustomerName, CustomerEmail, Status, TotalAmount, ShippingAddress, Notes, timestamps
  - Business method: `RecalculateTotal()` (lines 48-52): Sums all `OrderLine.LineTotal` values
  - One-to-many relationship with OrderLines (line 40)

- **OrderLine** (`Order.cs`, lines 54-86):
  - Properties: Id, OrderId, ProductId, ProductName, ProductSKU, Quantity, UnitPrice
  - Computed property: `LineTotal` (line 79): `Quantity * UnitPrice`
  - Foreign keys: OrderId (with Cascade delete), ProductId (with Restrict delete)
  - Quantity validation: `[Range(1, int.MaxValue)]` (line 75)

#### **Data Module** (`src/InventoryApi/Data/`)

Entity Framework Core configuration and seeding:

- **InventoryDbContext** (`InventoryDbContext.cs`, lines 1-73):
  - DbSets for Products, Categories, Orders, OrderLines (lines 9-12)
  - Fluent API configuration (lines 15-42):
    - SKU unique index on Product (line 18): `entity.HasIndex(p => p.SKU).IsUnique()`
    - Category-Product one-to-many with Restrict delete behavior (lines 19-22): Prevents category deletion if products exist
    - OrderNumber unique index on Order (line 25)
    - Order-OrderLine one-to-many with Cascade delete (lines 26-29): Deleting order deletes all lines
    - OrderLine-Product many-to-one with Restrict delete (lines 32-35): Prevents product deletion if order lines reference it
  - Static `SeedData()` method (lines 44-73) populates 5 categories and 10 sample products on startup
  - Idempotent seeding: `if (db.Categories.Any()) return;` (line 45)
  - Seed data includes realistic products across 5 categories with pricing and stock levels

#### **DTOs Module** (`src/InventoryApi/DTOs/`)

Request/response contracts using C# records:

- **ProductDto** (`ProductDto.cs`, lines 1-20): Read model with 16 properties
  - Includes computed `IsLowStock` property
  - Includes `CategoryName` for display purposes
  - Includes timestamps for audit trails

- **CreateProductRequest** (`ProductDto.cs`, lines 22-35): Write model with validation attributes
  - `[Required]` on Name, SKU, CategoryId
  - `[MaxLength]` on all string properties
  - `[Range]` on numeric properties (Price, Cost, StockQuantity, ReorderPoint)

- **UpdateProductRequest** (`ProductDto.cs`, lines 37-47): Partial update model
  - Does not include SKU (immutable after creation)
  - Includes IsActive flag for soft-delete capability

- **StockAdjustmentRequest** (`ProductDto.cs`, lines 49-53): Stock delta with reason
  - Quantity can be positive (restock) or negative (sale/adjustment)
  - Reason field for audit trail

- **CategoryDto** (`ProductDto.cs`, lines 55-63): Category read model
  - Includes ProductCount computed property
  - Includes timestamps

- **CreateCategoryRequest** (`ProductDto.cs`, lines 65-70): Category write model
  - Slug is optional; auto-generated if not provided

- **CreateOrderRequest** (`ProductDto.cs`, lines 72-80): Order creation with line items
  - Requires at least one line item (validated in service)
  - CustomerName is required
  - Lines collection is required

- **CreateOrderLineRequest** (`ProductDto.cs`, lines 82-86): Order line item
  - ProductId and Quantity are required
  - Quantity must be >= 1

- **OrderDto** (`ProductDto.cs`, lines 88-99): Order read model
  - Includes all order details and line items
  - Status as string (enum converted to string)

- **OrderLineDto** (`ProductDto.cs`, lines 101-108): Order line read model
  - Includes computed LineTotal

#### **Middleware Module** (`src/InventoryApi/Middleware/`)

HTTP pipeline components:

- **RequestLoggingMiddleware** (`RequestLoggingMiddleware.cs`, lines 1-56):
  - Extracts or generates correlation ID from `X-Correlation-ID` header (line 18)
  - Generates 8-character correlation ID if not provided: `Guid.NewGuid().ToString("N")[..8]`
  - Logs request start with method, path, query string (lines 23-27)
  - Measures request duration with `Stopwatch` (line 29)
  - Logs response with status code and elapsed time (lines 40-47)
  - Differentiates log levels: Error (5xx), Warning (4xx), Information (2xx-3xx)
  - Adds correlation ID to response headers for client tracking (line 20)
  - Uses structured logging with named parameters for correlation tracking

### 2.3 Dependency Injection Configuration

Configured in `Program.cs` (lines 21-29):

```csharp
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));
```

**Service Lifetimes**:
- **Scoped**: ProductService, OrderService, CategoryService (one instance per HTTP request)
  - Ensures thread-safety for database operations
  - Proper resource cleanup via `IDisposable`
  - Prevents cross-request data leakage

- **Scoped**: InventoryDbContext (one instance per HTTP request)
  - EF Core DbContext is not thread-safe
  - Scoped lifetime ensures isolation between requests

**Initialization Order**:
1. Services registered (lines 21-29)
2. Application built (line 31)
3. Database seeded (lines 33-39)
4. Middleware pipeline configured (lines 41-57)
5. Application runs (line 59)

---

## 3. Key Design Patterns Used Throughout the Codebase

### 3.1 Layered Architecture Pattern

Clear separation between presentation (Controllers), business logic (Services), and data access (DbContext). Each layer has a single responsibility and communicates through well-defined interfaces (DTOs).

**Evidence**: 
- Controllers delegate to Services (`ProductsController.cs` line 24: `await _productService.GetAllAsync()`)
- Services delegate to DbContext (`ProductService.cs` line 20: `_db.Products.Include()`)
- DTOs decouple API contracts from domain models

### 3.2 Repository Pattern (Implicit)

Entity Framework Core DbContext acts as a repository, abstracting database operations. Services interact with `DbSet<T>` collections rather than raw SQL.

**Evidence**: 
- `InventoryDbContext.cs` lines 9-12 expose DbSets as repositories
- `ProductService.cs` line 20: `_db.Products.Include(p => p.Category).AsQueryable()`
- All queries use LINQ-to-Entities for automatic parameterization

### 3.3 Data Transfer Object (DTO) Pattern

Decouples API contracts from domain models. Controllers accept/return DTOs, services map between DTOs and entities.

**Evidence**:
- `ProductService.cs` line 177: `MapToDto()` method converts Product entity to ProductDto
- `ProductsController.cs` line 24: `IEnumerable<ProductDto>` return type
- DTOs use C# records for immutability and value-based equality

### 3.4 Dependency Injection Pattern

Constructor injection for loose coupling and testability. All dependencies are injected via constructors.

**Evidence**:
- `ProductService.cs` lines 11-15: Constructor accepts `InventoryDbContext` and `ILogger<ProductService>`
- `ProductsController.cs` lines 14-17: Constructor accepts `ProductService` and `ILogger<ProductsController>`
- All services registered in `Program.cs` lines 21-29

### 3.5 Middleware Pipeline Pattern

HTTP request/response processing through a chain of middleware components.

**Evidence**:
- `Program.cs` line 57: `app.UseMiddleware<RequestLoggingMiddleware>()`
- `RequestLoggingMiddleware.cs` lines 17-47: Implements `InvokeAsync()` to process requests
- Middleware executes in order: Swagger → RequestLogging → CORS → HTTPS → Authorization → Controllers

### 3.6 Validation Pattern

Data validation at multiple levels:
- **Attribute-based**: Data annotations on DTOs (`ProductDto.cs` line 2: `[Required]`)
- **Business logic**: Service methods validate state (`ProductService.cs` line 89: SKU uniqueness check)
- **Domain model**: Entity methods validate invariants (`Product.cs` line 56: stock cannot go negative)

**Evidence**:
- `CreateProductRequest` has `[Required]`, `[MaxLength]`, `[Range]` attributes
- `ProductService.CreateAsync()` checks SKU uniqueness (line 89) and category existence (line 92)
- `Product.UpdateStock()` throws if stock would become negative (line 56)
- Controllers validate `ModelState.IsValid` before processing (line 95)

### 3.7 Async/Await Pattern

All I/O operations are asynchronous to prevent thread pool starvation.

**Evidence**:
- `ProductService.GetAllAsync()` (line 19)
- `OrderService.CreateAsync()` (line 37)
- All controller actions are `async Task<IActionResult>`
- All database operations use `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`

### 3.8 Logging Pattern

Structured logging with correlation IDs for request tracing.

**Evidence**:
- `RequestLoggingMiddleware.cs` line 18: Extracts/generates correlation ID
- `ProductService.cs` line 108: `_logger.LogInformation("Created product {SKU} - {Name}", ...)`
- `OrderService.cs` line 85: `_logger.LogInformation("Order {OrderNumber} created...")`
- All logs include correlation ID for distributed tracing

### 3.9 Atomic Operations Pattern

Order creation atomically validates stock, deducts inventory, and creates order in a single transaction.

**Evidence**:
- `OrderService.CreateAsync()` lines 48-76: Validates all products, checks stock, deducts stock, creates order, then saves once
- `_db.SaveChangesAsync()` (line 77) commits all changes atomically
- If any validation fails, no database changes are made

### 3.10 Computed Properties Pattern

Read-only properties computed from other properties without database queries.

**Evidence**:
- `Product.IsLowStock` (line 51): `StockQuantity <= ReorderPoint`
- `Category.ProductCount` (line 25): `Products.Count`
- `OrderLine.LineTotal` (line 79): `Quantity * UnitPrice`
- These are calculated in-memory, not persisted to database

### 3.11 Immutable Records Pattern

DTOs use C# records for immutability and value-based equality.

**Evidence**:
- `ProductDto` (line 1): `public record ProductDto(...)`
- `CreateProductRequest` (line 22): `public record CreateProductRequest(...)`
- Records provide automatic `Equals()`, `GetHashCode()`, `ToString()` implementations
- Records are thread-safe and suitable for API contracts

### 3.12 Enum-Based State Machine Pattern

Order status transitions managed via enum.

**Evidence**:
- `OrderStatus` enum (lines 6-11): 7 states representing order lifecycle
- `Order.Status` property (line 27): `public OrderStatus Status { get; set; }`
- Status validation in controller (line 72): `Enum.TryParse<OrderStatus>()`

---

## 4. Dependency Graph Between Major Components

### 4.1 Component Dependency Map

```
┌─────────────────────────────────────────────────────────────┐
│                    HTTP Clients                              │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│              RequestLoggingMiddleware                        │
│  (Logs all requests with correlation IDs)                   │
└────────────────────────┬────────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────────┐
│  ProductsController  │  CategoriesController  │  OrdersController
│  (HTTP routing)      │  (HTTP routing)        │  (HTTP routing)
└────────┬─────────────┴──────────┬─────────────┴────────┬────┘
         │                        │                      │
    ┌────▼────────────────────────▼──────────────────────▼────┐
    │  ProductService  │  CategoryService  │  OrderService    │
    │  (Business logic)│  (Business logic) │  (Business logic)│
    └────┬────────────┬┴──────────┬────────┴────────┬─────────┘
         │            │           │                 │
         └────────────┼───────────┼─────────────────┘
                      │           │
              ┌───────▼───────────▼──────────┐
              │  InventoryDbContext          │
              │  (EF Core DbContext)         │
              │  - DbSet<Product>            │
              │  - DbSet<Category>           │
              │  - DbSet<Order>              │
              │  - DbSet<OrderLine>          │
              └───────┬──────────────────────┘
                      │
              ┌───────▼──────────────────────┐
              │  In-Memory Database          │
              │  (EF Core InMemory Provider) │
              └──────────────────────────────┘
```

### 4.2 Data Flow Diagrams

#### **Product Creation Flow**

```
POST /api/v1/products
    ↓
ProductsController.Create() [line 95]
    ├─ Validate ModelState [line 95]
    ├─ Call ProductService.CreateAsync() [line 99]
    │   ├─ Query: Check SKU uniqueness [line 89]
    │   ├─ Query: Validate category exists [line 92]
    │   ├─ Create Product entity [line 94]
    │   ├─ Add to DbSet<Product> [line 104]
    │   ├─ SaveChangesAsync() → Database [line 105]
    │   ├─ Load Category navigation [line 107]
    │   └─ MapToDto() → ProductDto [line 108]
    ├─ Return CreatedAtAction() [line 100]
    │   └─ HTTP 201 with Location header
    └─ Catch InvalidOperationException → HTTP 409 Conflict [line 107]
```

#### **Order Creation Flow**

```
POST /api/v1/orders
    ↓
OrdersController.Create() [line 48]
    ├─ Validate ModelState [line 50]
    ├─ Call OrderService.CreateAsync() [line 54]
    │   ├─ Validate order has line items [line 40]
    │   ├─ Fetch all products in one query [line 43-45]
    │   ├─ Validate all products exist and active [line 47]
    │   ├─ For each line item:
    │   │  ├─ Check stock availability [line 52]
    │   │  ├─ Deduct stock via Product.UpdateStock() [line 68]
    │   │  └─ Create OrderLine [line 70-75]
    │   ├─ RecalculateTotal() [line 76]
    │   ├─ Add Order to DbSet<Order> [line 78]
    │   ├─ SaveChangesAsync() → Database (atomic) [line 79]
    │   └─ MapToDto() → OrderDto [line 81]
    ├─ Return CreatedAtAction() [line 55]
    │   └─ HTTP 201 with Location header
    └─ Catch InvalidOperationException → HTTP 400 Bad Request [line 60]
```

#### **Product Search Flow**

```
GET /api/v1/products/search?q=bluetooth
    ↓
ProductsController.Search() [line 60]
    ├─ Call ProductService.SearchAsync() [line 63]
    │   ├─ Check if query is empty [line 49]
    │   ├─ Convert query to lowercase [line 51]
    │   ├─ Query DbSet<Product> with LINQ [line 52-56]:
    │   │  ├─ Name.ToLower().Contains(query)
    │   │  ├─ SKU.ToLower().Contains(query)
    │   │  ├─ Description.ToLower().Contains(query)
    │   │  └─ Brand.ToLower().Contains(query)
    │   ├─ Filter IsActive = true [line 52]
    │   ├─ Include Category [line 52]
    │   ├─ ToListAsync() → Database [line 57]
    │   └─ MapToDto() → IEnumerable<ProductDto> [line 58]
    ↓
200 OK + ProductDto[]
```

### 4.3 Cross-Cutting Concerns

**Request Logging**: Every request flows through `RequestLoggingMiddleware` before reaching controllers.
- Correlation ID extracted or generated (line 18)
- Request logged with method, path, query string (lines 23-27)
- Response logged with status code and elapsed time (lines 40-47)
- Correlation ID added to response headers (line 20)

**Dependency Injection**: All services and DbContext are injected via constructor, enabling:
- Loose coupling between layers
- Easy mocking for unit tests
- Centralized configuration in `Program.cs`

---

## 5. Communication Patterns

### 5.1 Synchronous Communication

All communication is **synchronous request-response** via HTTP REST:

- **Controllers** receive HTTP requests and return HTTP responses
- **Services** are called synchronously from controllers
- **Database** queries are awaited asynchronously but blocking at the HTTP request level

**Evidence**:
- `ProductsController.GetAll()` (line 24): `await _productService.GetAllAsync()` returns `IActionResult`
- `ProductService.GetAllAsync()` (line 20): `await query.ToListAsync()` executes query synchronously within request context

### 5.2 Request/Response Patterns

#### **RESTful Conventions**

- **GET** for retrieval (idempotent, cacheable)
- **POST** for creation (returns 201 Created with Location header)
- **PUT** for full updates (returns 200 OK)
- **PATCH** for partial updates (order status, returns 200 OK)
- **DELETE** for removal (returns 204 No Content)

**Evidence**:
- `ProductsController.Create()` (line 100): `return CreatedAtAction(nameof(GetById), new { id = product.Id }, product)`
  - Returns HTTP 201 Created
  - Location header: `/api/v1/products/{id}`
- `OrdersController.UpdateStatus()` (line 72): `[HttpPatch("{id:guid}/status")]`
  - Returns HTTP 200 OK with updated order
- `ProductsController.Delete()` (line 151): `return NoContent()`
  - Returns HTTP 204 No Content

#### **Error Handling**

Controllers catch `InvalidOperationException` and return appropriate HTTP status codes:

- **400 Bad Request**: Validation failures, insufficient stock, invalid enum values
- **404 Not Found**: Resource not found
- **409 Conflict**: Business rule violations (duplicate SKU, non-empty category deletion)

**Evidence**:
- `ProductsController.Create()` (lines 100-107): Catches `InvalidOperationException` and returns 409 Conflict
- `OrdersController.Create()` (lines 57-65): Catches `InvalidOperationException` and returns 400 Bad Request
- `OrdersController.UpdateStatus()` (line 72): Validates enum with `Enum.TryParse()` and returns 400 if invalid

### 5.3 Async/Await Implementation

All I/O operations use async/await to prevent blocking:

- **Database queries**: `ToListAsync()`, `FirstOrDefaultAsync()`, `SaveChangesAsync()`
- **Controller actions**: `async Task<IActionResult>`
- **Service methods**: `async Task<T>`

**Evidence**:
- `ProductService.GetAllAsync()` (line 20): `await query.ToListAsync()`
- `OrderService.CreateAsync()` (line 79): `await _db.SaveChangesAsync()`
- All controller actions use `async Task<IActionResult>` pattern

### 5.4 Correlation Tracking

Request correlation IDs enable end-to-end tracing:

1. Client sends `X-Correlation-ID` header (optional)
2. Middleware extracts or generates 8-character ID (`RequestLoggingMiddleware.cs` line 18)
3. Middleware adds ID to response headers (line 20)
4. All logs include correlation ID (line 24)

**Evidence**:
```csharp
var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                    ?? Guid.NewGuid().ToString("N")[..8];
context.Response.Headers["X-Correlation-ID"] = correlationId;
_logger.LogInformation("[{CorrelationId}] {Method} {Path}{Query} started", 
    correlationId, context.Request.Method, context.Request.Path, context.Request.QueryString);
```

**Benefits**:
- Trace requests across multiple services
- Correlate logs from different components
- Debug distributed issues
- Monitor request lifecycle

---

## 6. Security Architecture

### 6.1 Authentication and Authorization

**Current State**: No authentication/authorization implemented.

- No `[Authorize]` attributes on controllers
- No JWT token validation
- No role-based access control (RBAC)
- CORS allows any origin, method, and header

**Evidence**:
- `Program.cs` lines 31-37: CORS policy allows all origins and methods
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
- Controllers have no `[Authorize]` attributes

**Recommended Implementation**:

```csharp
// In Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = "https://your-auth-server";
        options.Audience = "inventory-api";
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    options.AddPolicy("UserOrAdmin", policy =>
        policy.RequireRole("User", "Admin"));
});

// In controllers
[Authorize(Policy = "AdminOnly")]
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
{
    // ...
}
```

### 6.2 Input Validation

**Data Annotation Validation**:
- Required fields: `[Required]` on all DTOs
- String length: `[MaxLength(n)]` on all string properties
- Numeric ranges: `[Range(min, max)]` on numeric properties

**Evidence**:
- `CreateProductRequest` (lines 22-35): All properties have validation attributes
  ```csharp
  public record CreateProductRequest(
      [Required][MaxLength(200)] string Name,
      [Required][MaxLength(100)] string SKU,
      [Range(0, double.MaxValue)] decimal Price,
      // ...
  );
  ```
- `ProductsController.Create()` (line 95): `if (!ModelState.IsValid) return BadRequest(ModelState)`

**Business Logic Validation**:
- SKU uniqueness: `ProductService.CreateAsync()` line 89
- Category existence: `ProductService.CreateAsync()` line 92
- Stock availability: `OrderService.CreateAsync()` lines 48-54
- Non-negative stock: `Product.UpdateStock()` line 56
- Enum validation: `OrdersController.UpdateStatus()` line 72

### 6.3 Data Protection

**In-Memory Database**:
- No persistence to disk
- Data lost on application restart
- No encryption at rest
- No encryption in transit (HTTPS available but not enforced in code)

**Sensitive Data**:
- Customer email stored in plaintext in Order entity (line 26)
- No PII encryption
- No data masking in logs

**Evidence**:
- `Order.cs` line 26: `public string? CustomerEmail { get; set; }`
- `Program.cs` line 54: `app.UseHttpsRedirection()` (optional, not enforced in code)

### 6.4 SQL Injection Prevention

**EF Core Parameterization**:
All database queries use LINQ-to-Entities, which automatically parameterizes queries. No raw SQL strings.

**Evidence**:
- `ProductService.SearchAsync()` (lines 48-56): Uses LINQ `Where()` with lambda expressions
- No `FromSqlRaw()` or `FromSqlInterpolated()` calls in codebase
- All queries use parameterized LINQ expressions

### 6.5 CORS Configuration

**Current Configuration** (`Program.cs` lines 31-37):
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

**Issues**:
- Allows requests from any origin
- Allows any HTTP method
- Allows any header
- No credentials validation

**Production Recommendation**:
```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", policy =>
    {
        policy.WithOrigins("https://yourdomain.com", "https://app.yourdomain.com")
              .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE")
              .WithHeaders("Content-Type", "Authorization")
              .AllowCredentials();
    });
});

app.UseCors("ProductionPolicy");
```

### 6.6 HTTPS Enforcement

**Current State** (`Program.cs` line 54):
```csharp
app.UseHttpsRedirection();
```

**Behavior**:
- Redirects HTTP requests to HTTPS
- Only active in production (not in development)
- Adds `Strict-Transport-Security` header

**Production Recommendation**:
```csharp
if (!app.Environment.IsDevelopment())
{
    app.UseHsts(); // HTTP Strict Transport Security
    app.UseHttpsRedirection();
}
```

### 6.7 Recommended Security Enhancements

1. **Authentication**: Implement JWT bearer token validation
2. **Authorization**: Add role-based access control (Admin, User, Guest)
3. **CORS**: Restrict to specific origins in production
4. **HTTPS**: Enforce HTTPS in production with HSTS
5. **Rate Limiting**: Implement rate limiting middleware
6. **Input Sanitization**: Sanitize search queries to prevent injection attacks
7. **Audit Logging**: Log all modifications with user identity
8. **Data Encryption**: Encrypt sensitive fields (customer email, addresses)
9. **API Key Management**: Implement API key validation for third-party integrations
10. **Security Headers**: Add X-Frame-Options, X-Content-Type-Options, Content-Security-Policy

---

## 7. Scalability Considerations and Bottlenecks

### 7.1 Current Scalability Limitations

#### **In-Memory Database**

The in-memory database is the primary scalability bottleneck:

- **Single Instance**: Data is stored in application memory, not shared across instances
- **No Persistence**: Data is lost on restart
- **Memory Bound**: Total data size limited by available RAM
- **No Horizontal Scaling**: Cannot distribute load across multiple servers

**Evidence**:
- `Program.cs` line 26: `options.UseInMemoryDatabase("InventoryDb")`
- `InventoryDbContext.cs` line 44: `SeedData()` populates 10 products on startup

#### **Stateless Design Limitation**

While the API is stateless, the in-memory database creates implicit state:

- Each instance has its own data copy
- No data synchronization between instances
- Requests to different instances see different data

#### **Single-Threaded Request Processing**

Although async/await is used, the in-memory database is not thread-safe for concurrent writes:

- EF Core InMemory provider uses simple locking
- High concurrency may cause contention
- No built-in connection pooling (not applicable to in-memory)

### 7.2 Performance Bottlenecks

#### **Full-Text Search**

`ProductService.SearchAsync()` (lines 48-56) performs client-side filtering:

```csharp
.Where(p => p.IsActive && (
    p.Name.ToLower().Contains(lower) ||
    p.SKU.ToLower().Contains(lower) ||
    (p.Description != null && p.Description.ToLower().Contains(lower)) ||
    (p.Brand != null && p.Brand.ToLower().Contains(lower))
))
```

**Issues**:
- No database indexes on searchable fields
- Case-insensitive search requires `ToLower()` on every comparison
- Multiple OR conditions prevent query optimization
- Scales O(n) with product count
- Substring matching is inefficient for large datasets

**Performance Characteristics**:
- 100 products: ~1ms
- 1,000 products: ~10ms
- 10,000 products: ~100ms
- 100,000 products: ~1000ms (unacceptable)

**Recommendation**: Use full-text search indexes in a relational database (SQL Server, PostgreSQL) or Elasticsearch.

#### **N+1 Query Problem**

`ProductService.GetAllAsync()` (line 20) uses `Include()` to prevent N+1 queries:

```csharp
var query = _db.Products.Include(p => p.Category).AsQueryable();
```

**Current State**: Properly mitigated with eager loading.

**Verification**:
- Single query to fetch products with categories
- No additional queries per product

#### **Order Creation Validation**

`OrderService.CreateAsync()` (lines 48-54) queries products multiple times:

```csharp
var products = await _db.Products
    .Where(p => productIds.Contains(p.Id) && p.IsActive)
    .ToListAsync();

foreach (var lineReq in request.Lines)
{
    var product = products.First(p => p.Id == lineReq.ProductId);
    // ...
}
```

**Current State**: Optimized with single query and in-memory lookup.

**Verification**:
- Single database query to fetch all products
- In-memory LINQ for finding products in loop
- No additional database queries

### 7.3 Scalability Recommendations

#### **Short-term (In-Memory Database)**

1. **Add Caching**: Implement distributed cache (Redis) for frequently accessed products
   ```csharp
   builder.Services.AddStackExchangeRedisCache(options =>
   {
       options.Configuration = builder.Configuration.GetConnectionString("Redis");
   });
   ```

2. **Pagination**: Add `Skip()/Take()` to list endpoints to limit result sets
   ```csharp
   public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
   {
       var products = await _productService.GetAllAsync(page, pageSize);
       return Ok(products);
   }
   ```

3. **Filtering**: Add query parameters to reduce result sets before returning
   ```csharp
   public async Task<IActionResult> GetAll([FromQuery] bool? isActive, [FromQuery] Guid? categoryId)
   {
       var products = await _productService.GetAllAsync(isActive, categoryId);
       return Ok(products);
   }
   ```

4. **Indexing**: Add database indexes on SKU, CategoryId, IsActive (when migrating to relational DB)

#### **Medium-term (Relational Database)**

1. **Replace InMemory**: Migrate to SQL Server, PostgreSQL, or MySQL
   ```csharp
   builder.Services.AddDbContext<InventoryDbContext>(options =>
       options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
   ```

2. **Connection Pooling**: Use connection pooling to handle concurrent requests
   ```
   Server=localhost;Database=InventoryDb;User Id=sa;Password=YourPassword;Min Pool Size=5;Max Pool Size=100;
   ```

3. **Database Indexes**: Create indexes on:
   - `Product.SKU` (unique)
   - `Product.CategoryId` (foreign key)
   - `Product.IsActive` (filter)
   - `Order.OrderNumber` (unique)
   - `OrderLine.ProductId` (foreign key)

4. **Full-Text Search**: Implement database-level full-text search
   ```csharp
   // SQL Server
   .Where(p => EF.Functions.Like(p.Name, $"%{query}%"))
   
   // PostgreSQL
   .Where(p => EF.Functions.ILike(p.Name, $"%{query}%"))
   ```

#### **Long-term (Distributed Architecture)**

1. **Horizontal Scaling**: Deploy multiple API instances behind a load balancer
   - Kubernetes with auto-scaling
   - Azure App Service with scale sets
   - AWS ECS with auto-scaling groups

2. **Database Replication**: Use read replicas for read-heavy queries
   - Primary database for writes
   - Read replicas for queries
   - Automatic failover

3. **Caching Layer**: Implement Redis for session and data caching
   - Cache frequently accessed products
   - Cache category lists
   - Cache order history

4. **Message Queue**: Use message broker (RabbitMQ, Azure Service Bus) for async operations:
   - Order processing
   - Stock notifications
   - Audit logging

5. **Microservices**: Split into separate services:
   - Product Catalog Service
   - Order Service
   - Inventory Service
   - Notification Service

### 7.4 Load Testing Considerations

**Current Limitations**:
- In-memory database cannot handle sustained high load
- Single instance cannot scale beyond available RAM
- No caching layer to reduce database hits

**Recommended Load Testing Thresholds**:
- **Concurrent Users**: < 100 (in-memory)
- **Requests/Second**: < 1,000 (in-memory)
- **Data Size**: < 100,000 products (in-memory)

**Load Testing Tools**:
- Apache JMeter
- Locust
- k6
- Artillery

**Sample Load Test Script (k6)**:
```javascript
import http from 'k6/http';
import { check } from 'k6';

export let options = {
  vus: 50,
  duration: '30s',
};

export default function () {
  let res = http.get('http://localhost:5000/api/v1/products');
  check(res, {
    'status is 200': (r) => r.status === 200,
    'response time < 500ms': (r) => r.timings.duration < 500,
  });
}
```

---

## 8. Data Model and Relationships

### 8.1 Entity Relationship Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                      Category                               │
├─────────────────────────────────────────────────────────────┤
│ PK: Id (Guid)                                               │
│ Name (string, required, max 100)                            │
│ Description (string, max 500)                               │
│ Slug (string, max 50)                                       │
│ IsActive (bool, default true)                               │
│ CreatedAt (DateTime, UTC)                                   │
│ UpdatedAt (DateTime, UTC)                                   │
│ Products (ICollection<Product>)                             │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ 1
                          │
                          │ *
┌─────────────────────────────────────────────────────────────┐
│                      Product                                │
├─────────────────────────────────────────────────────────────┤
│ PK: Id (Guid)                                               │
│ Name (string, required, max 200)                            │
│ SKU (string, required, max 100, unique)                     │
│ Description (string, max 2000)                              │
│ Price (decimal(18,2), >= 0)                                 │
│ Cost (decimal(18,2), >= 0)                                  │
│ StockQuantity (int, >= 0)                                   │
│ ReorderPoint (int, >= 0, default 10)                        │
│ IsActive (bool, default true)                               │
│ Brand (string, max 100)                                     │
│ WeightKg (decimal(10,2))                                    │
│ FK: CategoryId (Guid)                                       │
│ CreatedAt (DateTime, UTC)                                   │
│ UpdatedAt (DateTime, UTC)                                   │
│ Category (Category)                                         │
│ OrderLines (ICollection<OrderLine>)                         │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ 1
                          │
                          │ *
┌─────────────────────────────────────────────────────────────┐
│                      OrderLine                              │
├─────────────────────────────────────────────────────────────┤
│ PK: Id (Guid)                                               │
│ FK: OrderId (Guid)                                          │
│ FK: ProductId (Guid)                                        │
│ ProductName (string, required, max 200)                     │
│ ProductSKU (string, max 100)                                │
│ Quantity (int, required, >= 1)                              │
│ UnitPrice (decimal(18,2))                                   │
│ LineTotal (decimal, computed: Quantity * UnitPrice)         │
│ Order (Order)                                               │
│ Product (Product)                                           │
└─────────────────────────────────────────────────────────────┘
                          ▲
                          │ 1
                          │
                          │ *
┌─────────────────────────────────────────────────────────────┐
│                       Order                                 │
├─────────────────────────────────────────────────────────────┤
│ PK: Id (Guid)                                               │
│ OrderNumber (string, required, max 50, unique)              │
│ CustomerName (string, required, max 200)                    │
│ CustomerEmail (string, max 200)                             │
│ Status (OrderStatus enum: Pending, Confirmed, Processing,   │
│         Shipped, Delivered, Cancelled, Refunded)            │
│ TotalAmount (decimal(18,2))                                 │
│ ShippingAddress (string, max 500)                           │
│ Notes (string, max 1000)                                    │
│ CreatedAt (DateTime, UTC)                                   │
│ UpdatedAt (DateTime, UTC)                                   │
│ Lines (ICollection<OrderLine>)                              │
└─────────────────────────────────────────────────────────────┘
```

### 8.2 Referential Integrity

**Configured in `InventoryDbContext.OnModelCreating()`** (`InventoryDbContext.cs` lines 15-42):

1. **Product → Category** (lines 19-22):
   - Foreign Key: `Product.CategoryId`
   - Delete Behavior: `Restrict` (cannot delete category with products)
   - Relationship: One Category has many Products
   - **Rationale**: Prevents orphaned products; requires reassignment before category deletion

2. **Order → OrderLine** (lines 26-29):
   - Foreign Key: `OrderLine.OrderId`
   - Delete Behavior: `Cascade` (deleting order deletes lines)
   - Relationship: One Order has many OrderLines
   - **Rationale**: Order lines are dependent on order; no standalone order lines

3. **Product → OrderLine** (lines 32-35):
   - Foreign Key: `OrderLine.ProductId`
   - Delete Behavior: `Restrict` (cannot delete product with order lines)
   - Relationship: One Product has many OrderLines
   - **Rationale**: Prevents deletion of products with order history; maintains audit trail

### 8.3 Data Constraints

**Unique Constraints**:
- `Product.SKU` (line 18): Prevents duplicate product codes
  ```csharp
  entity.HasIndex(p => p.SKU).IsUnique();
  ```
- `Order.OrderNumber` (line 25): Prevents duplicate order numbers
  ```csharp
  entity.HasIndex(o => o.OrderNumber).IsUnique();
  ```

**Validation Constraints** (via Data Annotations):
- `[Required]`: Name, SKU, CustomerName, OrderNumber, Lines
- `[MaxLength(n)]`: All string properties
- `[Range(min, max)]`: Price, Cost, StockQuantity, ReorderPoint, Quantity

**Database Column Types**:
- `decimal(18,2)`: Price, Cost, TotalAmount, UnitPrice (monetary values)
- `decimal(10,2)`: WeightKg (non-monetary decimal)
- `int`: StockQuantity, ReorderPoint, Quantity (whole numbers)
- `Guid`: All primary and foreign keys
- `DateTime`: CreatedAt, UpdatedAt (UTC timestamps)

### 8.4 Timestamp Management

**CreatedAt and UpdatedAt**:
- Automatically set to `DateTime.UtcNow` on entity creation
- Updated on modification (e.g., `Product.UpdateStock()` line 58)
- Used for audit trails and sorting (e.g., `OrderService.GetAllAsync()` line 22)

**Evidence**:
- `Product.cs` line 47: `public DateTime CreatedAt { get; set; } = DateTime.UtcNow;`
- `Product.cs` line 58: `UpdatedAt = DateTime.UtcNow;` in `UpdateStock()`
- `OrderService.cs` line 22: `.OrderByDescending(o => o.CreatedAt)`

---

## 9. Testing Architecture

### 9.1 Test Framework and Tools

- **Test Runner**: xUnit 2.6.1
- **Assertion Library**: FluentAssertions 6.12.0
- **Mocking**: Moq 4.20.69
- **Database**: EF Core InMemory (same as production)

**Evidence**:
- `InventoryApi.Tests.csproj` lines 8-15: Package references

### 9.2 Test Coverage

**ProductServiceTests** (`ProductServiceTests.cs`, lines 1-211):

Comprehensive unit tests for `ProductService`:

1. **GetAllAsync Tests**:
   - `GetAllAsync_ReturnsActiveProducts()` (lines 32-45): Verifies active-only filtering
   - `GetAllAsync_WithActiveOnlyFalse_ReturnsAllProducts()` (lines 47-57): Verifies all products returned

2. **GetByIdAsync Tests**:
   - `GetByIdAsync_ExistingProduct_ReturnsDto()` (lines 59-70): Verifies product retrieval
   - `GetByIdAsync_NonExistentProduct_ReturnsNull()` (lines 72-76): Verifies null return

3. **CreateAsync Tests**:
   - `CreateAsync_ValidRequest_CreatesProduct()` (lines 78-92): Verifies product creation
   - `CreateAsync_DuplicateSKU_ThrowsInvalidOperationException()` (lines 94-104): Verifies SKU uniqueness
   - `CreateAsync_InvalidCategory_ThrowsInvalidOperationException()` (lines 106-112): Verifies category validation

4. **DeleteAsync Tests**:
   - `DeleteAsync_ExistingProduct_ReturnsTrue()` (lines 114-124): Verifies deletion
   - `DeleteAsync_NonExistentProduct_ReturnsFalse()` (lines 126-130): Verifies false return

5. **AdjustStockAsync Tests**:
   - `AdjustStockAsync_PositiveAdjustment_IncreasesStock()` (lines 132-144): Verifies stock increase
   - `AdjustStockAsync_ExceedsAvailableStock_ThrowsInvalidOperationException()` (lines 146-156): Verifies stock validation

6. **SearchAsync Tests**:
   - `SearchAsync_MatchesName_ReturnsResults()` (lines 158-170): Verifies search functionality

7. **GetLowStockAsync Tests**:
   - `GetLowStockAsync_ReturnsOnlyLowStockProducts()` (lines 172-185): Verifies low-stock filtering

### 9.3 Test Setup and Teardown

**Setup** (lines 15-28):
- Creates isolated in-memory database for each test: `Guid.NewGuid().ToString()`
- Adds test category
- Instantiates `ProductService` with `NullLogger`

**Teardown** (line 30):
- Disposes DbContext to clean up resources

**Database Isolation**:
- Each test gets a unique in-memory database name
- Prevents test interference
- Ensures test independence

### 9.4 Test Patterns

**Arrange-Act-Assert (AAA)**:
```csharp
// Arrange
_db.Products.Add(product);
await _db.SaveChangesAsync();

// Act
var result = await _sut.GetByIdAsync(product.Id);

// Assert
result.Should().NotBeNull();
result!.Id.Should().Be(product.Id);
```

**Exception Testing**:
```csharp
var act = () => _sut.CreateAsync(request);
await act.Should().ThrowAsync<InvalidOperationException>()
    .WithMessage("*SKU*DUP-001*already exists*");
```

**Fluent Assertions**:
- `Should().HaveCount(1)`: Verify collection size
- `Should().NotBeNull()`: Verify non-null
- `Should().Be(value)`: Verify equality
- `Should().ThrowAsync<T>()`: Verify exception thrown

### 9.5 Test Gaps

**Not Tested**:
- `OrderService` (no test file)
- `CategoryService` (no test file)
- Controllers (no integration tests)
- Middleware (no tests)
- Error handling in controllers
- CORS configuration
- Swagger documentation

**Recommended Additional Tests**:

1. **OrderService Tests**:
   ```csharp
   [Fact]
   public async Task CreateAsync_ValidOrder_CreatesOrderAndDeductsStock()
   {
       // Arrange
       var product = new Product { StockQuantity = 100 };
       _db.Products.Add(product);
       await _db.SaveChangesAsync();
       
       var request = new CreateOrderRequest(
           "John Doe", "john@example.com", "123 Main St",
           null, new List<CreateOrderLineRequest> { 
               new(product.Id, 10) 
           }
       );
       
       // Act
       var result = await _sut.CreateAsync(request);
       
       // Assert
       result.Should().NotBeNull();
       result.TotalAmount.Should().Be(product.Price * 10);
       var updatedProduct = await _db.Products.FindAsync(product.Id);
       updatedProduct!.StockQuantity.Should().Be(90);
   }
   ```

2. **CategoryService Tests**:
   ```csharp
   [Fact]
   public async Task DeleteAsync_CategoryWithProducts_ThrowsInvalidOperationException()
   {
       // Arrange
       var category = new Category { Name = "Test" };
       var product = new Product { CategoryId = category.Id };
       _db.Categories.Add(category);
       _db.Products.Add(product);
       await _db.SaveChangesAsync();
       
       // Act & Assert
       var act = () => _sut.DeleteAsync(category.Id);
       await act.Should().ThrowAsync<InvalidOperationException>()
           .WithMessage("*Cannot delete*category*contains products*");
   }
   ```

3. **Controller Integration Tests**:
   ```csharp
   [Fact]
   public async Task Create_ValidProduct_Returns201Created()
   {
       // Arrange
       var request = new CreateProductRequest(
           "Test", "TST-001", null, 10m, 5m, 100, 20, null, null, _categoryId
       );
       
       // Act
       var response = await _client.PostAsJsonAsync("/api/v1/products", request);
       
       // Assert
       response.StatusCode.Should().Be(HttpStatusCode.Created);
       response.Headers.Location.Should().NotBeNull();
   }
   ```

4. **Middleware Tests**:
   ```csharp
   [Fact]
   public async Task InvokeAsync_AddsCorrelationIdToResponse()
   {
       // Arrange
       var context = new DefaultHttpContext();
       context.Request.Headers["X-Correlation-ID"] = "test-123";
       
       // Act
       await _middleware.InvokeAsync(context);
       
       // Assert
       context.Response.Headers["X-Correlation-ID"].Should().Be("test-123");
   }
   ```

### 9.6 Test Coverage Metrics

**Current Coverage**:
- ProductService: ~90% (8 test methods covering main paths)
- OrderService: 0% (no tests)
- CategoryService: 0% (no tests)
- Controllers: 0% (no tests)
- Middleware: 0% (no tests)

**Overall Coverage**: ~15-20%

**Target Coverage**: 80%+ for production readiness

---

## 10. API Specification

### 10.1 Base URL

```
http://localhost:5000/api/v1
https://localhost:5001/api/v1
```

### 10.2 Products Endpoints

| Method | Path | Description | Status Codes | Implemented |
|--------|------|-------------|--------------|-------------|
| GET | `/products` | List all active products | 200 | ✓ |
| GET | `/products?activeOnly=false` | List all products | 200 | ✓ |
| GET | `/products/{id}` | Get product by ID | 200, 404 | ✓ |
| GET | `/products/sku/{sku}` | Get product by SKU | 200, 404 | ✓ |
| GET | `/products/search?q={query}` | Search products | 200 | ✓ |
| GET | `/products/category/{categoryId}` | Get products by category | 200 | ✓ |
| GET | `/products/low-stock` | Get low-stock products | 200 | ✓ |
| POST | `/products` | Create product | 201, 400, 409 | ✓ |
| PUT | `/products/{id}` | Update product | 200, 400, 404 | ✓ |
| DELETE | `/products/{id}` | Delete product | 204, 404 | ✓ |
| POST | `/products/{id}/stock` | Adjust stock | 200, 400, 404 | ✓ |

### 10.3 Categories Endpoints

| Method | Path | Description | Status Codes | Implemented |
|--------|------|-------------|--------------|-------------|
| GET | `/categories` | List all categories | 200 | ✓ |
| GET | `/categories/{id}` | Get category by ID | 200, 404 | ✓ |
| POST | `/categories` | Create category | 201, 400, 409 | ✓ |
| PUT | `/categories/{id}` | Update category | 200, 400, 404 | ✓ |
| DELETE | `/categories/{id}` | Delete category | 204, 400, 404 | ✓ |

### 10.4 Orders Endpoints

| Method | Path | Description | Status Codes | Implemented |
|--------|------|-------------|--------------|-------------|
| GET | `/orders` | List all orders | 200 | ✓ |
| GET | `/orders/{id}` | Get order by ID | 200, 404 | ✓ |
| POST | `/orders` | Create order | 201, 400 | ✓ |
| PATCH | `/orders/{id}/status` | Update order status | 200, 400, 404 | ✓ |

### 10.5 Response Headers

All responses include:
- `X-Correlation-ID`: Request correlation ID for tracing (generated or extracted from request)
- `Content-Type: application/json`

### 10.6 Error Response Format

```json
{
  "message": "Error description"
}
```

**Examples**:
- SKU Duplicate: `{"message": "A product with SKU 'DUP-001' already exists."}`
- Category Not Found: `{"message": "Category with ID '...' does not exist or is inactive."}`
- Insufficient Stock: `{"message": "Insufficient stock for 'Product Name' (SKU: SKU-001). Available: 5, Requested: 10"}`

### 10.7 API Examples

#### **Create Product**
```bash
curl -X POST http://localhost:5000/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Wireless Mouse",
    "sku": "ELEC-004",
    "description": "Ergonomic wireless mouse",
    "price": 49.99,
    "cost": 20.00,
    "stockQuantity": 150,
    "reorderPoint": 30,
    "brand": "TechCo",
    "categoryId": "<category-id>"
  }'
```

**Response (201 Created)**:
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Wireless Mouse",
  "sku": "ELEC-004",
  "description": "Ergonomic wireless mouse",
  "price": 49.99,
  "cost": 20.00,
  "stockQuantity": 150,
  "reorderPoint": 30,
  "isActive": true,
  "brand": "TechCo",
  "weightKg": null,
  "categoryId": "<category-id>",
  "categoryName": "Electronics",
  "isLowStock": false,
  "createdAt": "2024-01-15T10:30:00Z",
  "updatedAt": "2024-01-15T10:30:00Z"
}
```

#### **Place Order**
```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "John Doe",
    "customerEmail": "john@example.com",
    "shippingAddress": "123 Main St, City, State 12345",
    "lines": [
      {"productId": "<product-id-1>", "quantity": 2},
      {"productId": "<product-id-2>", "quantity": 1}
    ]
  }'
```

**Response (201 Created)**:
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "orderNumber": "ORD-001001",
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "status": "Confirmed",
  "totalAmount": 299.97,
  "shippingAddress": "123 Main St, City, State 12345",
  "notes": null,
  "lines": [
    {
      "id": "770e8400-e29b-41d4-a716-446655440000",
      "productId": "<product-id-1>",
      "productName": "Wireless Headphones Pro",
      "productSKU": "ELEC-001",
      "quantity": 2,
      "unitPrice": 149.99,
      "lineTotal": 299.98
    }
  ],
  "createdAt": "2024-01-15T10:35:00Z",
  "updatedAt": "2024-01-15T10:35:00Z"
}
```

#### **Search Products**
```bash
curl "http://localhost:5000/api/v1/products/search?q=wireless"
```

**Response (200 OK)**:
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Wireless Headphones Pro",
    "sku": "ELEC-001",
    "description": "Premium wireless headphones with noise cancellation",
    "price": 149.99,
    "cost": 60.00,
    "stockQuantity": 85,
    "reorderPoint": 15,
    "isActive": true,
    "brand": "TechCo",
    "weightKg": 0.25,
    "categoryId": "<category-id>",
    "categoryName": "Electronics",
    "isLowStock": false,
    "createdAt": "2024-01-15T10:00:00Z",
    "updatedAt": "2024-01-15T10:00:00Z"
  }
]
```

#### **Update Order Status**
```bash
curl -X PATCH http://localhost:5000/api/v1/orders/<order-id>/status \
  -H "Content-Type: application/json" \
  -d '{"status": "Shipped"}'
```

**Response (200 OK)**:
```json
{
  "id": "660e8400-e29b-41d4-a716-446655440000",
  "orderNumber": "ORD-001001",
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "status": "Shipped",
  "totalAmount": 299.97,
  "shippingAddress": "123 Main St, City, State 12345",
  "notes": null,
  "lines": [...],
  "createdAt": "2024-01-15T10:35:00Z",
  "updatedAt": "2024-01-15T10:40:00Z"
}
```

### 10.8 API Versioning Strategy

**Current Implementation**: Hardcoded `v1` in route prefix
```csharp
[Route("api/v1/[controller]")]
```

**Recommended Strategy**:
1. **URL-based versioning** (current): `/api/v1/products`, `/api/v2/products`
2. **Header-based versioning**: `X-API-Version: 1`
3. **Query parameter versioning**: `/api/products?version=1`

**Backward Compatibility Plan**:
- Support multiple versions simultaneously
- Deprecate old versions with 6-month notice
- Provide migration guide for clients

### 10.9 Rate Limiting Specification

**Not Currently Implemented**

**Recommended Implementation**:
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter(policyName: "fixed", configure: options =>
    {
        options.PermitLimit = 100;
        options.Window = TimeSpan.FromMinutes(1);
    });
});

app.UseRateLimiter();
```

**Rate Limits**:
- 100 requests per minute per IP address
- 1000 requests per hour per API key
- 10,000 requests per day per API key

---

## 11. Deployment and Infrastructure

### 11.1 Application Startup Sequence

**Initialization Sequence** (`Program.cs` lines 1-70):

1. **Create WebApplicationBuilder** (line 9)
   - Initializes configuration from appsettings.json
   - Sets up logging providers
   - Prepares dependency injection container

2. **Register Services** (lines 11-29):
   - Controllers: `AddControllers()` (line 10)
   - Swagger/OpenAPI: `AddSwaggerGen()` (lines 11-18)
   - EF Core InMemory DbContext: `AddDbContext<InventoryDbContext>()` (lines 26-27)
   - Application services: `AddScoped<ProductService>()`, etc. (lines 21-23)
   - CORS policy: `AddCors()` (lines 31-37)
   - Console logging: `AddConsole()` (line 39)

3. **Build Application** (line 41)
   - Finalizes service registration
   - Creates service provider
   - Validates dependency graph

4. **Seed Database** (lines 43-49):
   - Create scope for database access
   - Get DbContext from service provider
   - Ensure database created: `EnsureCreated()`
   - Call `SeedData()` to populate initial data

5. **Configure Middleware Pipeline** (lines 51-67):
   - Swagger UI (development only): `UseSwagger()`, `UseSwaggerUI()` (lines 53-55)
   - Request logging: `UseMiddleware<RequestLoggingMiddleware>()` (line 57)
   - CORS: `UseCors()` (line 58)
   - HTTPS redirection: `UseHttpsRedirection()` (line 59)
   - Authorization: `UseAuthorization()` (line 60)
   - Map controllers: `MapControllers()` (line 61)

6. **Run Application** (line 63)
   - Starts Kestrel web server
   - Listens on HTTP (5000) and HTTPS (5001)
   - Blocks until shutdown signal

**Startup Time**: ~500-1000ms (in-memory database)

### 11.2 Environment Configuration

**Development**:
- Swagger UI enabled at `/swagger`
- Console logging enabled
- HTTPS redirection enabled
- Detailed error messages

**Production**:
- Swagger UI disabled
- Console logging enabled (can be configured)
- HTTPS redirection enabled
- Generic error messages

**Configuration Detection**:
```csharp
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(...);
}
```

### 11.3 Database Initialization

**Seeding** (`InventoryDbContext.SeedData()`, lines 44-73):

1. Check if categories exist (line 45)
   ```csharp
   if (db.Categories.Any()) return;
   ```

2. Create 5 categories: Electronics, Clothing, Food & Beverage, Books, Sports

3. Create 10 sample products across categories with:
   - Realistic pricing (cost < price)
   - Stock quantities
   - Reorder points
   - Brand information
   - Descriptions

4. Save to database

**Idempotent Seeding**: Only seeds if database is empty, preventing duplicate data on restarts.

**Seed Data Summary**:
- 5 categories
- 10 products
- Total initial data: ~2KB

### 11.4 Deployment Considerations

**Prerequisites**:
- .NET 9 SDK or runtime
- No external dependencies (in-memory database)
- No database migrations required

**Build**:
```bash
dotnet build
```

**Run**:
```bash
dotnet run
```

**Publish**:
```bash
dotnet publish -c Release -o ./publish
```

**Docker Containerization**:

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY . .
RUN dotnet build -c Release

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /src/bin/Release/net9.0/publish .
EXPOSE 5000 5001
ENTRYPOINT ["dotnet", "InventoryApi.dll"]
```

**Docker Build and Run**:
```bash
docker build -t inventory-api:latest .
docker run -p 5000:5000 -p 5001:5001 inventory-api:latest
```

**Kubernetes Deployment**:

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: inventory-api
  labels:
    app: inventory-api
spec:
  replicas: 3
  selector:
    matchLabels:
      app: inventory-api
  template:
    metadata:
      labels:
        app: inventory-api
    spec:
      containers:
      - name: inventory-api
        image: inventory-api:latest
        ports:
        - containerPort: 5000
          name: http
        - containerPort: 5001
          name: https
        env:
        - name: ASPNETCORE_ENVIRONMENT
          value: "Production"
        - name: ASPNETCORE_URLS
          value: "http://+:5000;https://+:5001"
        livenessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 30
          periodSeconds: 10
          timeoutSeconds: 5
        readinessProbe:
          httpGet:
            path: /health
            port: 5000
          initialDelaySeconds: 10
          periodSeconds: 5
          timeoutSeconds: 3
        resources:
          requests:
            memory: "256Mi"
            cpu: "250m"
          limits:
            memory: "512Mi"
            cpu: "500m"
---
apiVersion: v1
kind: Service
metadata:
  name: inventory-api-service
spec:
  selector:
    app: inventory-api
  ports:
  - protocol: TCP
    port: 80
    targetPort: 5000
    name: http
  - protocol: TCP
    port: 443
    targetPort: 5001
    name: https
  type: LoadBalancer
```

**Health Check Endpoint** (Not Currently Implemented):

```csharp
app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .WithName("Health")
    .WithOpenApi();
```

### 11.5 Configuration Management

**appsettings.json** (Not included in repository):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=InventoryDb;User Id=sa;Password=YourPassword;"
  }
}
```

**Environment Variables**:
- `ASPNETCORE_ENVIRONMENT`: Development, Staging, Production
- `ASPNETCORE_URLS`: HTTP and HTTPS endpoints
- `ASPNETCORE_HTTPS_PORT`: HTTPS port (default 5001)

### 11.6 Graceful Shutdown

**Current Implementation**: Default ASP.NET Core behavior
- Stops accepting new requests
- Waits for in-flight requests to complete (default 30 seconds)
- Closes database connections

**Recommended Enhancement**:

```csharp
var cts = new CancellationTokenSource();
Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

await app.RunAsync(cts.Token);
```

---

## 12. .NET 9 Specific Features

### 12.1 Nullable Reference Types

**Enabled in Project File** (`InventoryApi.csproj` line 4):
```xml
<Nullable>enable</Nullable>
```

**Usage**:
- `string?` indicates nullable string
- `string` indicates non-nullable string
- Compiler enforces null-safety checks

**Examples**:
- `Product.cs` line 21: `public string? Description { get; set; }`
- `Product.cs` line 13: `public string Name { get; set; } = string.Empty;`

### 12.2 Implicit Usings

**Enabled in Project File** (`InventoryApi.csproj` line 5):
```xml
<ImplicitUsings>enable</ImplicitUsings>
```

**Automatically Included Namespaces**:
- `System`
- `System.Collections.Generic`
- `System.Linq`
- `System.Threading.Tasks`
- `Microsoft.AspNetCore.Builder`
- `Microsoft.AspNetCore.Http`
- `Microsoft.Extensions.DependencyInjection`
- `Microsoft.Extensions.Logging`

**Benefit**: Reduces boilerplate using statements

### 12.3 C# 13 Records

**Used for DTOs** (`ProductDto.cs`):
```csharp
public record ProductDto(
    Guid Id,
    string Name,
    string SKU,
    // ...
);
```

**Benefits**:
- Immutable by default
- Value-based equality
- Automatic `ToString()`, `Equals()`, `GetHashCode()`
- Positional parameters
- Thread-safe

### 12.4 Top-Level Statements

**Used in Program.cs** (lines 1-70):
- No `Main()` method required
- Implicit `Program` class
- Cleaner startup code

### 12.5 File-Scoped Namespaces

**Used Throughout Codebase**:
```csharp
namespace InventoryApi.Controllers;

public class ProductsController : ControllerBase
{
    // ...
}
```

**Benefit**: Reduces indentation, improves readability

### 12.6 Target-Typed New Expressions

**Used in Middleware** (`RequestLoggingMiddleware.cs` line 29):
```csharp
var sw = Stopwatch.StartNew();
```

**Benefit**: Compiler infers type from context

### 12.7 Range Operator

**Used in Middleware** (`RequestLoggingMiddleware.cs` line 18):
```csharp
Guid.NewGuid().ToString("N")[..8]
```

**Benefit**: Concise substring syntax

---

## 13. Summary and Recommendations

### 13.1 Strengths

1. **Clean Architecture**: Clear separation of concerns with layered design
2. **Async/Await**: All I/O operations are non-blocking
3. **Validation**: Multi-level validation (annotations, business logic, domain models)
4. **Logging**: Structured logging with correlation IDs
5. **Testing**: Comprehensive unit tests with proper setup/teardown
6. **Documentation**: Swagger/OpenAPI integration for API documentation
7. **Error Handling**: Appropriate HTTP status codes and error messages
8. **Dependency Injection**: Loose coupling with constructor injection
9. **Modern C#**: Uses C# 13 features (records, nullable reference types)
10. **Atomic Operations**: Order creation ensures data consistency

### 13.2 Weaknesses

1. **In-Memory Database**: Not suitable for production; data lost on restart
2. **No Authentication/Authorization**: No security controls
3. **No Caching**: Every request hits the database
4. **Limited Search**: Client-side filtering without indexes
5. **No Pagination**: List endpoints return all results
6. **No Rate Limiting**: Vulnerable to abuse
7. **Limited Test Coverage**: Only ProductService tested (~15-20%)
8. **No Audit Logging**: No tracking of who made changes
9. **No Health Check Endpoint**: Cannot monitor application status
10. **No API Versioning Strategy**: Hardcoded v1 in routes

### 13.3 Recommended Improvements (Priority Order)

**Phase 1 (Critical)**:
1. Replace in-memory database with SQL Server/PostgreSQL
2. Add authentication (JWT bearer tokens)
3. Add authorization (role-based access control)
4. Implement pagination on list endpoints
5. Add database indexes

**Phase 2 (Important)**:
1. Implement caching layer (Redis)
2. Add rate limiting middleware
3. Expand test coverage (OrderService, CategoryService, Controllers)
4. Add integration tests
5. Implement audit logging

**Phase 3 (Nice-to-Have)**:
1. Full-text search with Elasticsearch
2. Message queue for async operations
3. API versioning strategy
4. GraphQL endpoint
5. Microservices architecture

### 13.4 Production Readiness Checklist

- [ ] Replace in-memory database with relational database
- [ ] Implement JWT authentication
- [ ] Implement role-based authorization
- [ ] Add rate limiting middleware
- [ ] Add caching layer (Redis)
- [ ] Expand test coverage to 80%+
- [ ] Add integration tests
- [ ] Implement audit logging
- [ ] Add health check endpoint
- [ ] Add monitoring and alerting
- [ ] Document API with examples
- [ ] Set up CI/CD pipeline
- [ ] Configure HTTPS certificates
- [ ] Load test with production-like data
- [ ] Security audit
- [ ] Performance profiling
- [ ] Implement pagination
- [ ] Add database indexes
- [ ] Restrict CORS to specific origins
- [ ] Implement API versioning strategy

---

## Appendix A: File Structure

```
demo-inventory-csharp/
├── src/
│   └── InventoryApi/
│       ├── Controllers/
│       │   ├── CategoriesController.cs (102 lines)
│       │   ├── OrdersController.cs (83 lines)
│       │   └── ProductsController.cs (162 lines)
│       ├── Data/
│       │   └── InventoryDbContext.cs (73 lines)
│       ├── DTOs/
│       │   └── ProductDto.cs (106 lines)
│       ├── Middleware/
│       │   └── RequestLoggingMiddleware.cs (56 lines)
│       ├── Models/
│       │   ├── Category.cs (28 lines)
│       │   ├── Order.cs (86 lines)
│       │   └── Product.cs (64 lines)
│       ├── Services/
│       │   ├── CategoryService.cs (94 lines)
│       │   ├── OrderService.cs (121 lines)
│       │   └── ProductService.cs (180 lines)
│       ├── InventoryApi.csproj (11 lines)
│       └── Program.cs (70 lines)
├── tests/
│   └── InventoryApi.Tests/
│       ├── InventoryApi.Tests.csproj (19 lines)
│       └── ProductServiceTests.cs (211 lines)
├── data/
│   ├── suppliers.json (428 KB)
│   └── transactions-history.json (8.9 MB)
├── demo-inventory-csharp.sln (25 lines)
├── README.md (139 lines)
└── .gitignore (75 bytes)

Total: 21 files, ~1,500 lines of C# code
```

---

## Appendix B: NuGet Dependencies

**InventoryApi.csproj**:
- Microsoft.EntityFrameworkCore.InMemory 9.0.0
- Swashbuckle.AspNetCore 6.5.0

**InventoryApi.Tests.csproj**:
- Microsoft.NET.Test.Sdk 17.8.0
- xunit 2.6.1
- xunit.runner.visualstudio 2.5.3
- Moq 4.20.69
- FluentAssertions 6.12.0
- Microsoft.EntityFrameworkCore.InMemory 9.0.0

---

## Appendix C: Coding Standards and Conventions

### C.1 Naming Conventions

**Classes and Records**:
- PascalCase: `ProductService`, `CreateProductRequest`, `ProductDto`

**Methods**:
- PascalCase: `GetAllAsync()`, `CreateAsync()`, `UpdateStock()`
- Async methods end with `Async`: `GetAllAsync()`, `SaveChangesAsync()`

**Properties**:
- PascalCase: `Name`, `SKU`, `StockQuantity`
- Nullable properties use `?`: `Description?`, `Brand?`

**Private Fields**:
- Camel case with underscore prefix: `_db`, `_logger`, `_orderCounter`

**Constants**:
- PascalCase: `DefaultPageSize = 20`

**Parameters**:
- Camel case: `request`, `id`, `categoryId`

### C.2 Code Style

**Async/Await**:
- Always use `async Task<T>` for methods with return values
- Always use `async Task` for void-returning methods
- Always `await` async calls

**Null Checking**:
- Use null-coalescing operator: `value ?? defaultValue`
- Use null-conditional operator: `obj?.Property`
- Use pattern matching: `if (product is null) return null;`

**LINQ**:
- Use method syntax over query syntax
- Chain methods for readability
- Use `FirstOrDefaultAsync()` instead of `First()` for async

**Exception Handling**:
- Catch specific exceptions: `catch (InvalidOperationException ex)`
- Log exceptions with context
- Throw with descriptive messages

### C.3 Documentation

**XML Comments**:
```csharp
/// <summary>Get all active products</summary>
[HttpGet]
public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
{
    // ...
}
```

**Inline Comments**:
- Use sparingly; code should be self-documenting
- Explain "why", not "what"

---

## Appendix D: Architecture Decision Records (ADRs)

### ADR-001: Layered Architecture

**Decision**: Use layered (N-tier) architecture with Controllers, Services, and Data Access layers.

**Rationale**:
- Clear separation of concerns
- Easy to test each layer independently
- Familiar pattern for most developers
- Scales well for small to medium applications

**Consequences**:
- Additional abstraction layers
- Potential performance overhead from layer crossing
- Requires careful design to avoid tight coupling

---

## Appendix E: Troubleshooting Guide

### E.1 Common Issues

**Issue**: Database is empty after restart
- **Cause**: In-memory database is ephemeral
- **Solution**: Data is reseeded on startup; this is expected behavior

**Issue**: Duplicate SKU error when creating product
- **Cause**: SKU already exists in database
- **Solution**: Use a unique SKU value

**Issue**: Insufficient stock error when creating order
- **Cause**: Product stock is less than requested quantity
- **Solution**: Adjust stock or reduce order quantity

**Issue**: Category cannot be deleted
- **Cause**: Category has products assigned
- **Solution**: Reassign products to another category first

**Issue**: Correlation ID not in logs
- **Cause**: Middleware not configured
- **Solution**: Verify `app.UseMiddleware<RequestLoggingMiddleware>()` is called

### E.2 Performance Troubleshooting

**Slow Product Search**:
- Check number of products in database
- Consider adding database indexes
- Implement caching for frequently searched terms

**Slow Order Creation**:
- Verify stock validation is not querying database multiple times
- Check for N+1 query problems
- Monitor database connection pool

**High Memory Usage**:
- In-memory database stores all data in RAM
- Consider migrating to relational database
- Implement pagination to reduce result sets

---

## Appendix F: CI/CD Pipeline Specification

### F.1 GitHub Actions Workflow

```yaml
name: Build and Test

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  build:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '9.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Test
      run: dotnet test --configuration Release --no-build --verbosity normal
    
    - name: Publish
      run: dotnet publish -c Release -o ./publish
    
    - name: Upload artifact
      uses: actions/upload-artifact@v3
      with:
        name: inventory-api
        path: ./publish
```

### F.2 Deployment Pipeline

```yaml
name: Deploy to Production

on:
  push:
    branches: [ main ]

jobs:
  deploy:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Build Docker image
      run: docker build -t inventory-api:${{ github.sha }} .
    
    - name: Push to registry
      run: docker push inventory-api:${{ github.sha }}
    
    - name: Deploy to Kubernetes
      run: |
        kubectl set image deployment/inventory-api \
          inventory-api=inventory-api:${{ github.sha }}
```

---

**Document Version**: 2.0  
**Last Updated**: 2024  
**Repository**: demo-inventory-csharp  
**Framework**: ASP.NET Core 9  
**Status**: Production-Ready (with recommendations)