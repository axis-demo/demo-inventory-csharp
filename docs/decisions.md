# Architecture Decision Records: demo-inventory-csharp

## Executive Summary

This document captures the key architectural decisions made in the **demo-inventory-csharp** project, a demonstration ASP.NET Core 9 REST API for inventory management. The decisions cover technology choices, design patterns, trade-offs, and implementation strategies across all layers of the application.

---

## ADR-001: Technology Stack - ASP.NET Core 9 with Entity Framework Core

### Context

The project required a modern, production-ready web framework for building a REST API with database persistence. The team needed to balance developer productivity, performance, ecosystem maturity, and alignment with enterprise .NET standards.

### Decision

**Adopt ASP.NET Core 9 as the primary framework with Entity Framework Core (EF Core 9.0.0) for data access.**

**Evidence from codebase:**
- `src/InventoryApi/InventoryApi.csproj` specifies `<TargetFramework>net9.0</TargetFramework>`
- `Program.cs` (line 1-4) imports and configures ASP.NET Core services: `WebApplication.CreateBuilder()`, `AddControllers()`, `AddEndpointsApiExplorer()`
- EF Core is configured in `Program.cs` (line 20-21): `builder.Services.AddDbContext<InventoryDbContext>(options => options.UseInMemoryDatabase("InventoryDb"))`
- `InventoryDbContext.cs` inherits from `DbContext` and uses EF Core's fluent API for model configuration

### Consequences

**Positive:**
- **Modern Language Features:** .NET 9 provides latest C# language features (records, nullable reference types, implicit usings)
- **Performance:** ASP.NET Core is benchmarked as one of the fastest web frameworks (TechEmpower benchmarks)
- **Ecosystem:** Rich NuGet package ecosystem and extensive community support
- **Type Safety:** Strong typing with nullable reference types enabled (`<Nullable>enable</Nullable>` in .csproj)
- **Built-in DI:** Native dependency injection container eliminates need for external IoC libraries

**Negative:**
- **Platform Lock-in:** Tied to Microsoft's .NET ecosystem; not cross-platform for legacy systems
- **Learning Curve:** Developers unfamiliar with .NET require onboarding
- **Licensing:** While open-source, some enterprise features require commercial licensing

**Trade-offs:**
- Chose .NET 9 (latest LTS) over .NET 8 for access to newest features and longer support window
- Selected in-memory database over SQL Server for zero-configuration demo purposes (see ADR-002)

---

## ADR-002: Database Technology - Entity Framework Core In-Memory Database

### Context

The project is a demonstration API that needs to be runnable without external dependencies (no SQL Server, PostgreSQL, or Docker setup). The team needed a persistence mechanism that:
- Requires zero configuration
- Supports immediate startup
- Provides realistic ORM usage patterns
- Allows for easy testing

### Decision

**Use Entity Framework Core's in-memory database provider for all data persistence.**

**Evidence from codebase:**
- `InventoryApi.csproj` (line 8): `<PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />`
- `Program.cs` (line 20-21): `builder.Services.AddDbContext<InventoryDbContext>(options => options.UseInMemoryDatabase("InventoryDb"))`
- `InventoryDbContext.cs` (line 47-73): `SeedData()` method populates database on startup with 10 sample products across 5 categories
- `Program.cs` (line 44-48): Database seeding on application startup:
  ```csharp
  using (var scope = app.Services.CreateScope())
  {
      var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
      db.Database.EnsureCreated();
      InventoryDbContext.SeedData(db);
  }
  ```

### Consequences

**Positive:**
- **Zero Configuration:** No database server installation required; runs immediately after `dotnet run`
- **Fast Startup:** In-memory database initializes in milliseconds
- **Realistic ORM Patterns:** Uses full EF Core API (LINQ, eager loading, relationships) applicable to production databases
- **Testability:** Each test can use isolated in-memory database instance (see `ProductServiceTests.cs` line 18-25)
- **Demo-Friendly:** Perfect for demonstrations, tutorials, and quick prototyping

**Negative:**
- **Data Persistence:** All data is lost on application restart; not suitable for production
- **Concurrency Limitations:** In-memory database has different concurrency characteristics than real databases
- **Scale Limitations:** Cannot handle large datasets; memory-bound
- **No Real Transactions:** In-memory provider has simplified transaction semantics

**Trade-offs:**
- Sacrificed data persistence for ease of deployment
- Chose in-memory over SQLite for maximum simplicity (no file I/O)
- Suitable only for demonstration; production would require migration to SQL Server/PostgreSQL

---

## ADR-003: Architectural Pattern - Layered (N-Tier) Architecture

### Context

The application needed a clear separation of concerns to maintain code organization, testability, and maintainability. The team evaluated several patterns:
- Layered (N-tier)
- Clean Architecture (Hexagonal)
- CQRS
- Microservices

### Decision

**Implement a three-tier layered architecture: Controllers → Services → Data Access.**

**Evidence from codebase:**

**Layer 1 - API/Controller Layer:**
- `src/InventoryApi/Controllers/ProductsController.cs` (line 7-162): HTTP request handling with routing attributes `[Route("api/v1/[controller]")]` and action methods decorated with `[HttpGet]`, `[HttpPost]`, `[HttpPut]`, `[HttpDelete]`, `[HttpPatch]`
- `src/InventoryApi/Controllers/OrdersController.cs` (line 7-83): Order management endpoints
- `src/InventoryApi/Controllers/CategoriesController.cs` (line 7-102): Category management endpoints
- Controllers handle HTTP routing, validation, and response formatting

**Layer 2 - Service/Business Logic Layer:**
- `src/InventoryApi/Services/ProductService.cs` (line 8-180): Business logic for product operations including validation, stock management, and search
- `src/InventoryApi/Services/OrderService.cs` (line 8-121): Order creation with stock validation and deduction
- `src/InventoryApi/Services/CategoryService.cs` (line 8-94): Category management with deletion constraints
- Services contain business logic, validation, and orchestration

**Layer 3 - Data Access Layer:**
- `src/InventoryApi/Data/InventoryDbContext.cs` (line 6-73): DbContext manages entity relationships and database operations
- DbContext manages entity relationships and database operations

**Directory Structure:**
```
src/InventoryApi/
├── Controllers/      # HTTP request handling (ProductsController, OrdersController, CategoriesController)
├── Services/         # Business logic (ProductService, OrderService, CategoryService)
├── Data/            # EF Core DbContext (InventoryDbContext)
├── Models/          # Domain entities (Product, Category, Order, OrderLine)
├── DTOs/            # Request/response contracts (ProductDto, OrderDto, CategoryDto)
└── Middleware/      # Cross-cutting concerns (RequestLoggingMiddleware)
```

### Consequences

**Positive:**
- **Separation of Concerns:** Each layer has a single responsibility
- **Testability:** Services can be tested independently with mocked DbContext (see `ProductServiceTests.cs`)
- **Maintainability:** Clear code organization makes navigation and modification straightforward
- **Reusability:** Services can be reused across multiple controllers
- **Scalability:** Easy to add new features without affecting existing layers

**Negative:**
- **Boilerplate:** Requires mapping between DTOs and domain models (see `ProductService.MapToDto()` line 177-180)
- **Performance Overhead:** Additional abstraction layers add method call overhead
- **Coupling:** Controllers are tightly coupled to specific services (see `ProductsController.cs` line 17: `_productService = productService`)
- **Limited Flexibility:** Layered architecture doesn't scale well for complex domain logic

**Trade-offs:**
- Chose layered over Clean Architecture to reduce complexity for a demo project
- Chose layered over CQRS to avoid command/query separation overhead
- Chose layered over microservices to maintain single deployable unit

---

## ADR-004: Dependency Injection - Built-in ASP.NET Core Container

### Context

The application requires loose coupling between components and testability. The team needed to decide between:
- Built-in ASP.NET Core DI container
- Autofac
- Ninject
- Castle Windsor

### Decision

**Use the built-in ASP.NET Core dependency injection container for all service registration and resolution.**

**Evidence from codebase:**
- `Program.cs` (line 24-26): Service registration using `builder.Services.AddScoped<T>()`
  ```csharp
  builder.Services.AddScoped<ProductService>();
  builder.Services.AddScoped<OrderService>();
  builder.Services.AddScoped<CategoryService>();
  ```
- Controllers receive services via constructor injection (e.g., `ProductsController.cs` line 14-18):
  ```csharp
  public ProductsController(ProductService productService, ILogger<ProductsController> logger)
  {
      _productService = productService;
      _logger = logger;
  }
  ```
- Services receive DbContext via constructor injection (e.g., `ProductService.cs` line 12-16):
  ```csharp
  public ProductService(InventoryDbContext db, ILogger<ProductService> logger)
  {
      _db = db;
      _logger = logger;
  }
  ```

### Consequences

**Positive:**
- **No External Dependencies:** Built-in container eliminates need for third-party libraries
- **Lightweight:** Minimal overhead compared to full-featured containers
- **Integration:** Seamlessly integrated with ASP.NET Core middleware and logging
- **Testability:** Easy to mock dependencies in unit tests
- **Convention-Based:** Follows .NET conventions; familiar to .NET developers

**Negative:**
- **Limited Features:** Lacks advanced features like property injection, interceptors, or decorators
- **Scoping Complexity:** Requires understanding of Transient/Scoped/Singleton lifetimes
- **No Auto-Registration:** Must manually register each service

**Trade-offs:**
- Chose built-in container over Autofac to reduce external dependencies
- Scoped lifetime chosen for services to ensure fresh instance per request (appropriate for stateless services)

---

## ADR-005: API Design - RESTful JSON API with Versioning

### Context

The application needed to expose data and operations to clients. The team evaluated:
- REST with JSON
- GraphQL
- gRPC
- SOAP

### Decision

**Implement a RESTful JSON API with explicit versioning in the URL path.**

**Evidence from codebase:**

**REST Principles:**
- `ProductsController.cs` (line 7): `[Route("api/v1/[controller]")]` - versioned route
- HTTP methods map to operations:
  - `GetAll()` (line 24): `[HttpGet]` - retrieve all
  - `GetById()` (line 34): `[HttpGet("{id:guid}")]` - retrieve single
  - `Create()` (line 97): `[HttpPost]` - create new
  - `Update()` (line 120): `[HttpPut("{id:guid}")]` - update existing
  - `Delete()` (line 145): `[HttpDelete("{id:guid}")]` - delete
  - `AdjustStock()` (line 156): `[HttpPost("{id:guid}/stock")]` - custom action

**JSON Serialization:**
- `Program.cs` (line 10): `builder.Services.AddControllers()` - default JSON serialization
- DTOs use C# records for immutability (e.g., `ProductDto.cs` line 5-20)

**Versioning:**
- All routes prefixed with `/api/v1/` (e.g., `ProductsController.cs` line 7)
- Allows future `/api/v2/` without breaking existing clients

**Status Codes:**
- Controllers return appropriate HTTP status codes:
  - `200 OK` for successful GET/PUT
  - `201 Created` for POST (e.g., `ProductsController.cs` line 104)
  - `204 No Content` for DELETE (e.g., `ProductsController.cs` line 152)
  - `400 Bad Request` for validation errors
  - `404 Not Found` for missing resources
  - `409 Conflict` for business logic violations

### Consequences

**Positive:**
- **Simplicity:** REST is well-understood and widely supported
- **Cacheability:** HTTP caching mechanisms work naturally with REST
- **Statelessness:** Each request is independent; no server-side session state
- **Discoverability:** API structure is self-evident from URL patterns
- **Tooling:** Excellent support in browsers, Postman, curl, etc.
- **Versioning:** URL-based versioning allows multiple API versions simultaneously

**Negative:**
- **Over-fetching:** Clients receive all fields even if only subset needed
- **Under-fetching:** Multiple requests needed for related data
- **Chattiness:** Complex operations require multiple round-trips
- **Versioning Overhead:** URL versioning requires maintaining multiple code paths

**Trade-offs:**
- Chose REST over GraphQL to avoid query complexity and N+1 query problems
- Chose REST over gRPC for browser compatibility and ease of testing
- Chose URL versioning over header-based versioning for explicit, discoverable versioning

---

## ADR-006: Data Transfer Objects (DTOs) - Explicit Request/Response Contracts

### Context

The application needed to decouple API contracts from domain models to:
- Prevent exposing internal implementation details
- Allow independent evolution of API and domain
- Provide validation at API boundary
- Support multiple representations of same entity

### Decision

**Use explicit Data Transfer Objects (DTOs) for all API requests and responses. Domain models remain internal to the service layer.**

**Evidence from codebase:**

**DTO Definitions:**
- `ProductDto.cs` (line 5-20): Response DTO with computed properties
  ```csharp
  public record ProductDto(
      Guid Id, string Name, string SKU, string? Description,
      decimal Price, decimal Cost, int StockQuantity, int ReorderPoint,
      bool IsActive, string? Brand, decimal? WeightKg,
      Guid CategoryId, string? CategoryName, bool IsLowStock,
      DateTime CreatedAt, DateTime UpdatedAt
  );
  ```
- `CreateProductRequest` (line 22-32): Request DTO with validation attributes
  ```csharp
  public record CreateProductRequest(
      [Required][MaxLength(200)] string Name,
      [Required][MaxLength(100)] string SKU,
      [Range(0, double.MaxValue)] decimal Price,
      ...
  );
  ```
- `CategoryDto.cs` (line 47-54): Category response DTO
  ```csharp
  public record CategoryDto(
      Guid Id, string Name, string? Description, string? Slug,
      bool IsActive, int ProductCount, DateTime CreatedAt, DateTime UpdatedAt
  );
  ```
- `OrderDto.cs` (line 73-83): Order response DTO with nested line items
  ```csharp
  public record OrderDto(
      Guid Id, string OrderNumber, string CustomerName, string? CustomerEmail,
      string Status, decimal TotalAmount, string? ShippingAddress, string? Notes,
      List<OrderLineDto> Lines, DateTime CreatedAt, DateTime UpdatedAt
  );
  ```

**Mapping from Domain to DTO:**
- `ProductService.cs` (line 177-180): Explicit mapping function
  ```csharp
  private static ProductDto MapToDto(Product p) => new(
      p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
      p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
      p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
      p.CreatedAt, p.UpdatedAt
  );
  ```
- `CategoryService.cs` (line 90-93): Category mapping
  ```csharp
  private static CategoryDto MapToDto(Category c) => new(
      c.Id, c.Name, c.Description, c.Slug, c.IsActive,
      c.Products.Count, c.CreatedAt, c.UpdatedAt
  );
  ```
- `OrderService.cs` (line 113-118): Order mapping with nested line items
  ```csharp
  private static OrderDto MapToDto(Order o) => new(
      o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
      o.Status.ToString(), o.TotalAmount, o.ShippingAddress, o.Notes,
      o.Lines.Select(l => new OrderLineDto(...)).ToList(),
      o.CreatedAt, o.UpdatedAt
  );
  ```

**Controller Usage:**
- `ProductsController.cs` (line 97-104): Controllers accept request DTOs and return response DTOs
  ```csharp
  [HttpPost]
  [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
  public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
  ```

### Consequences

**Positive:**
- **API Stability:** API contract independent of domain model changes
- **Validation:** Request validation at API boundary using data annotations
- **Security:** Prevents accidental exposure of internal fields (e.g., Cost field visible in ProductDto but not exposed in API)
- **Flexibility:** Different DTOs for different operations (CreateProductRequest vs UpdateProductRequest)
- **Documentation:** DTOs serve as self-documenting API contracts

**Negative:**
- **Boilerplate:** Requires mapping code between DTOs and domain models
- **Maintenance:** Changes to domain require corresponding DTO updates
- **Performance:** Additional object allocation and mapping overhead
- **Complexity:** Extra layer of indirection

**Trade-offs:**
- Chose explicit DTOs over exposing domain models directly for API stability
- Chose record types for DTOs for immutability and concise syntax
- Chose manual mapping over AutoMapper to keep dependencies minimal

---

## ADR-007: Validation Strategy - Data Annotations with Model State Validation

### Context

The application needed to validate user input at the API boundary. Options included:
- Data Annotations (built-in)
- FluentValidation
- Custom validation logic
- Hybrid approach

### Decision

**Use data annotations for declarative validation on DTOs, combined with explicit business logic validation in services.**

**Evidence from codebase:**

**Data Annotations on DTOs:**
- `ProductDto.cs` (line 24-32): Request validation attributes
  ```csharp
  public record CreateProductRequest(
      [Required][MaxLength(200)] string Name,
      [Required][MaxLength(100)] string SKU,
      [MaxLength(2000)] string? Description,
      [Range(0, double.MaxValue)] decimal Price,
      [Range(0, double.MaxValue)] decimal Cost,
      [Range(0, int.MaxValue)] int StockQuantity,
      [Range(0, int.MaxValue)] int ReorderPoint,
      ...
  );
  ```

**Controller-Level Validation:**
- `ProductsController.cs` (line 99-101): ModelState validation
  ```csharp
  if (!ModelState.IsValid)
      return BadRequest(ModelState);
  ```

**Business Logic Validation in Services:**
- `ProductService.cs` (line 88-89): Duplicate SKU check (service-layer validation)
  ```csharp
  if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
      throw new InvalidOperationException($"A product with SKU '{request.SKU}' already exists.");
  ```
- `CategoryService.cs` (line 45-46): Duplicate category name check
  ```csharp
  if (await _db.Categories.AnyAsync(c => c.Name == request.Name))
      throw new InvalidOperationException($"A category named '{request.Name}' already exists.");
  ```
- `OrderService.cs` (line 44-48): Stock availability check
  ```csharp
  if (product.StockQuantity < lineReq.Quantity)
      throw new InvalidOperationException($"Insufficient stock for '{product.Name}'...");
  ```

**Error Handling:**
- `ProductsController.cs` (line 105-110): Exception handling with appropriate HTTP status codes
  ```csharp
  catch (InvalidOperationException ex)
  {
      _logger.LogWarning("Failed to create product: {Message}", ex.Message);
      return Conflict(new { message = ex.Message });
  }
  ```

### Consequences

**Positive:**
- **Simplicity:** Data annotations are built-in; no external dependencies
- **Declarative:** Validation rules are visible on DTOs
- **Consistency:** ASP.NET Core automatically validates before controller action
- **Performance:** Validation happens early in request pipeline
- **Separation:** API validation separate from business logic validation

**Negative:**
- **Limited Expressiveness:** Complex validation rules difficult to express with annotations
- **Duplication:** Some validation rules duplicated in DTOs and domain models
- **Coupling:** DTOs coupled to validation framework
- **Testability:** Validation logic spread across annotations and service methods

**Trade-offs:**
- Chose data annotations over FluentValidation to minimize dependencies
- Chose hybrid approach (annotations + service validation) to handle both structural and business logic validation
- Business logic validation (e.g., duplicate SKU) remains in services for reusability

---

## ADR-008: Error Handling - Exception-Based with HTTP Status Code Mapping

### Context

The application needed a consistent approach to error handling and reporting. Options included:
- Exception-based with try-catch
- Result<T> pattern
- Global exception handler middleware
- Hybrid approach

### Decision

**Use exception-based error handling with explicit try-catch in controllers, mapping exceptions to appropriate HTTP status codes.**

**Evidence from codebase:**

**Exception Throwing in Services:**
- `ProductService.cs` (line 88-89): Business logic validation throws exceptions
  ```csharp
  if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
      throw new InvalidOperationException($"A product with SKU '{request.SKU}' already exists.");
  ```
- `OrderService.cs` (line 44-48): Stock validation throws exceptions
  ```csharp
  if (product.StockQuantity < lineReq.Quantity)
      throw new InvalidOperationException($"Insufficient stock for '{product.Name}'...");
  ```

**Exception Handling in Controllers:**
- `ProductsController.cs` (line 105-110): Catch and map to HTTP status
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
- `OrdersController.cs` (line 54-60): Similar pattern for orders
  ```csharp
  catch (InvalidOperationException ex)
  {
      _logger.LogWarning("Failed to create order: {Message}", ex.Message);
      return BadRequest(new { message = ex.Message });
  }
  ```
- `CategoriesController.cs` (line 85-91): Category deletion error handling
  ```csharp
  catch (InvalidOperationException ex)
  {
      return BadRequest(new { message = ex.Message });
  }
  ```

**HTTP Status Code Mapping:**
- `409 Conflict` for duplicate SKU (business rule violation) - `ProductsController.cs` line 110
- `400 Bad Request` for insufficient stock (validation failure) - `OrdersController.cs` line 60
- `404 Not Found` for missing resources - `ProductsController.cs` line 37
- `204 No Content` for successful deletion - `ProductsController.cs` line 152

### Consequences

**Positive:**
- **Simplicity:** Exception handling is straightforward and familiar
- **Control Flow:** Exceptions provide clear error paths
- **Logging:** Exceptions can be logged with full stack traces for debugging
- **Flexibility:** Different exception types can map to different status codes

**Negative:**
- **Performance:** Exception throwing is expensive; not suitable for control flow
- **Boilerplate:** Requires try-catch in every controller action
- **Coupling:** Controllers tightly coupled to specific exception types
- **Testability:** Exception-based testing can be verbose

**Trade-offs:**
- Chose exception-based over Result<T> pattern for simplicity
- Chose explicit try-catch over global exception handler to maintain control over status code mapping
- Chose InvalidOperationException for business logic errors (standard .NET convention)

---

## ADR-009: Logging Strategy - Console-Based with Correlation IDs

### Context

The application needed observability for debugging and monitoring. Options included:
- Console logging only
- File-based logging
- Structured logging (Serilog)
- Cloud logging (Application Insights)

### Decision

**Use ASP.NET Core's built-in console logging with request correlation IDs for request tracing.**

**Evidence from codebase:**

**Logging Configuration:**
- `Program.cs` (line 36-37): Console logging setup
  ```csharp
  builder.Logging.ClearProviders();
  builder.Logging.AddConsole();
  ```

**Request Correlation Middleware:**
- `RequestLoggingMiddleware.cs` (line 17-19): Correlation ID generation/propagation
  ```csharp
  var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                      ?? Guid.NewGuid().ToString("N")[..8];
  context.Response.Headers["X-Correlation-ID"] = correlationId;
  ```

**Request/Response Logging:**
- `RequestLoggingMiddleware.cs` (line 23-29): Log request start
  ```csharp
  _logger.LogInformation(
      "[{CorrelationId}] {Method} {Path}{Query} started",
      correlationId,
      context.Request.Method,
      context.Request.Path,
      context.Request.QueryString);
  ```
- `RequestLoggingMiddleware.cs` (line 39-48): Log response with duration
  ```csharp
  _logger.Log(
      level,
      "[{CorrelationId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms",
      correlationId,
      context.Request.Method,
      context.Request.Path,
      context.Response.StatusCode,
      sw.ElapsedMilliseconds);
  ```

**Service-Level Logging:**
- `ProductService.cs` (line 113): Log successful creation
  ```csharp
  _logger.LogInformation("Created product {SKU} - {Name}", product.SKU, product.Name);
  ```
- `OrderService.cs` (line 78-79): Log order creation
  ```csharp
  _logger.LogInformation("Order {OrderNumber} created for customer {Customer}, total {Total:C}",
      order.OrderNumber, order.CustomerName, order.TotalAmount);
  ```

### Consequences

**Positive:**
- **Simplicity:** Built-in logging requires no external dependencies
- **Correlation:** Correlation IDs enable request tracing across logs
- **Performance:** Console logging has minimal overhead
- **Debugging:** Logs include method, path, status code, and duration
- **Flexibility:** Easy to upgrade to Serilog or Application Insights later

**Negative:**
- **Limited Features:** No structured logging, no log levels per category
- **Persistence:** Console logs are ephemeral; not persisted
- **Scalability:** Console logging doesn't scale to distributed systems
- **Parsing:** Unstructured text logs difficult to parse and analyze

**Trade-offs:**
- Chose console logging over file-based for simplicity in demo
- Chose built-in logging over Serilog to minimize dependencies
- Chose correlation IDs for request tracing without full distributed tracing infrastructure

---

## ADR-010: Testing Strategy - Unit Tests with xUnit and Fluent Assertions

### Context

The application needed automated testing to ensure correctness and prevent regressions. Options included:
- Unit tests only
- Unit + integration tests
- Unit + integration + end-to-end tests
- Test framework selection (xUnit, NUnit, MSTest)

### Decision

**Implement unit tests using xUnit framework with Fluent Assertions for readable assertions. Focus on service layer testing with in-memory database.**

**Evidence from codebase:**

**Test Framework:**
- `InventoryApi.Tests.csproj` (line 8-13): Test dependencies
  ```xml
  <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.0" />
  <PackageReference Include="xunit" Version="2.6.1" />
  <PackageReference Include="xunit.runner.visualstudio" Version="2.5.3" />
  <PackageReference Include="Moq" Version="4.20.69" />
  <PackageReference Include="FluentAssertions" Version="6.12.0" />
  ```

**Test Class Structure:**
- `ProductServiceTests.cs` (line 11-27): Test class with setup/teardown
  ```csharp
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
          ...
      }

      public void Dispose() => _db.Dispose();
  }
  ```

**Test Examples:**

**Happy Path Test:**
- `ProductServiceTests.cs` (line 29-42): GetAllAsync returns active products
  ```csharp
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
  ```

**Exception Test:**
- `ProductServiceTests.cs` (line 107-119): CreateAsync throws on duplicate SKU
  ```csharp
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
  ```

**Fluent Assertions:**
- `ProductServiceTests.cs` (line 40-41): Readable assertions
  ```csharp
  result.Should().HaveCount(1);
  result.First().Name.Should().Be("Active Product");
  ```

### Consequences

**Positive:**
- **Readability:** Fluent assertions read like natural language
- **Isolation:** In-memory database provides test isolation
- **Speed:** Unit tests execute quickly
- **Maintainability:** xUnit is modern and well-supported
- **Coverage:** Service layer thoroughly tested

**Negative:**
- **Limited Scope:** Unit tests don't verify HTTP layer or full request/response cycle
- **Database Abstraction:** In-memory database doesn't catch SQL-specific issues
- **Integration Gaps:** No tests for controller-service integration
- **Coverage Gaps:** No end-to-end tests

**Trade-offs:**
- Chose unit tests over integration tests to keep test suite fast
- Chose xUnit over NUnit for modern syntax and better async support
- Chose in-memory database for tests to avoid external dependencies
- Chose Fluent Assertions over built-in assertions for readability

---

## ADR-011: CORS Policy - Allow All Origins

### Context

The API needed to be accessible from web browsers running on different origins. Options included:
- Allow all origins
- Allow specific origins
- No CORS (API-only)

### Decision

**Enable CORS with a permissive default policy allowing any origin, method, and header.**

**Evidence from codebase:**
- `Program.cs` (line 28-35): CORS configuration
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
- `Program.cs` (line 54): CORS middleware enabled
  ```csharp
  app.UseCors();
  ```

### Consequences

**Positive:**
- **Accessibility:** API accessible from any web origin
- **Development:** Simplifies development and testing from different origins
- **Flexibility:** No need to configure specific allowed origins

**Negative:**
- **Security Risk:** Allows cross-origin requests from any source
- **CSRF Vulnerability:** Susceptible to cross-site request forgery attacks
- **Data Exposure:** Sensitive data could be accessed from malicious origins
- **Production Unsuitable:** Not recommended for production APIs

**Trade-offs:**
- Chose permissive CORS for demo/development purposes
- Production would require restricting to specific trusted origins
- No CSRF tokens implemented (acceptable for demo, not for production)

---

## ADR-012: API Documentation - Swagger/OpenAPI with Swashbuckle

### Context

The API needed interactive documentation for developers. Options included:
- Swagger/OpenAPI with Swashbuckle
- Manual documentation
- GraphQL introspection
- API Blueprint

### Decision

**Use Swashbuckle to generate OpenAPI/Swagger documentation with interactive Swagger UI.**

**Evidence from codebase:**

**Swagger Configuration:**
- `InventoryApi.csproj` (line 9): Swashbuckle dependency
  ```xml
  <PackageReference Include="Swashbuckle.AspNetCore" Version="6.5.0" />
  ```
- `Program.cs` (line 11-18): Swagger generation configuration
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

**Swagger UI Middleware:**
- `Program.cs` (line 49-51): Swagger UI enabled in development
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
  }
  ```

**XML Documentation:**
- `ProductsController.cs` (line 21-22): XML doc comments for Swagger
  ```csharp
  /// <summary>Get all active products</summary>
  [HttpGet]
  ```

**Response Type Metadata:**
- `ProductsController.cs` (line 23-24): ProducesResponseType attributes
  ```csharp
  [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
  ```

### Consequences

**Positive:**
- **Interactive Documentation:** Swagger UI allows testing endpoints directly
- **Auto-Generated:** Documentation generated from code; stays in sync
- **OpenAPI Standard:** Follows industry standard for API documentation
- **Client Generation:** OpenAPI spec can generate client libraries
- **Discoverability:** Developers can explore API without external docs

**Negative:**
- **Maintenance:** XML doc comments must be kept up-to-date
- **Complexity:** Swagger configuration adds setup overhead
- **Security:** Swagger UI exposes API structure (disable in production)
- **Dependency:** Adds Swashbuckle dependency

**Trade-offs:**
- Chose Swagger over manual documentation for auto-generation
- Chose Swashbuckle over NSwag for broader ecosystem support
- Swagger UI disabled in production (line 49: `if (app.Environment.IsDevelopment())`)

---

## ADR-013: Entity Relationships - Foreign Keys with Cascade/Restrict Delete Behavior

### Context

The application has relationships between entities (Product-Category, Order-OrderLine). The team needed to decide on delete behavior and relationship configuration.

### Decision

**Use explicit foreign key constraints with selective cascade/restrict delete behavior configured via EF Core Fluent API.**

**Evidence from codebase:**

**Product-Category Relationship:**
- `InventoryDbContext.cs` (line 18-24): Product-Category configuration
  ```csharp
  modelBuilder.Entity<Product>(entity =>
  {
      entity.HasIndex(p => p.SKU).IsUnique();
      entity.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
  });
  ```
- `Product.cs` (line 43-46): Foreign key property and navigation
  ```csharp
  public Guid CategoryId { get; set; }

  [ForeignKey(nameof(CategoryId))]
  public Category? Category { get; set; }
  ```

**Order-OrderLine Relationship:**
- `InventoryDbContext.cs` (line 26-33): Order-OrderLine configuration
  ```csharp
  modelBuilder.Entity<Order>(entity =>
  {
      entity.HasIndex(o => o.OrderNumber).IsUnique();
      entity.HasMany(o => o.Lines)
            .WithOne(l => l.Order)
            .HasForeignKey(l => l.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
  });
  ```

**OrderLine-Product Relationship:**
- `InventoryDbContext.cs` (line 35-42): OrderLine-Product configuration
  ```csharp
  modelBuilder.Entity<OrderLine>(entity =>
  {
      entity.HasOne(l => l.Product)
            .WithMany(p => p.OrderLines)
            .HasForeignKey(l => l.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
  });
  ```

**Category-Product Relationship (Inverse):**
- `Category.cs` (line 24): Navigation property for products
  ```csharp
  public ICollection<Product> Products { get; set; } = new List<Product>();
  ```
- `InventoryDbContext.cs` (line 20-24): Configured with Restrict delete behavior to prevent accidental category deletion with products

**Unique Constraints:**
- `InventoryDbContext.cs` (line 19): SKU uniqueness
  ```csharp
  entity.HasIndex(p => p.SKU).IsUnique();
  ```
- `InventoryDbContext.cs` (line 28): OrderNumber uniqueness
  ```csharp
  entity.HasIndex(o => o.OrderNumber).IsUnique();
  ```

### Consequences

**Positive:**
- **Data Integrity:** Foreign key constraints prevent orphaned records
- **Referential Integrity:** Database enforces relationship validity
- **Explicit Configuration:** Delete behavior clearly specified
- **Uniqueness:** Unique constraints prevent duplicates (SKU, OrderNumber)

**Negative:**
- **Delete Restrictions:** Restrict behavior prevents deletion of categories with products
- **Cascade Complexity:** Cascade delete can have unintended consequences
- **Performance:** Foreign key checks add database overhead

**Trade-offs:**
- Chose `DeleteBehavior.Restrict` for Product-Category to prevent accidental data loss
- Chose `DeleteBehavior.Cascade` for Order-OrderLine to clean up line items when order deleted
- Chose `DeleteBehavior.Restrict` for OrderLine-Product to prevent product deletion if referenced in orders

---

## ADR-014: Stock Management - Optimistic Locking with UpdateStock Method

### Context

The application manages product stock quantities. The team needed to prevent race conditions where concurrent orders could over-sell inventory.

### Decision

**Implement stock management with explicit UpdateStock method that validates availability before deduction. Use in-memory database's implicit locking for concurrency control.**

**Evidence from codebase:**

**UpdateStock Method:**
- `Product.cs` (line 54-61): Stock update with validation
  ```csharp
  public void UpdateStock(int quantity)
  {
      if (StockQuantity + quantity < 0)
          throw new InvalidOperationException($"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}");

      StockQuantity += quantity;
      UpdatedAt = DateTime.UtcNow;
  }
  ```

**Stock Validation in OrderService:**
- `OrderService.cs` (line 50-54): Pre-order stock check
  ```csharp
  foreach (var lineReq in request.Lines)
  {
      var product = products.First(p => p.Id == lineReq.ProductId);
      if (product.StockQuantity < lineReq.Quantity)
          throw new InvalidOperationException($"Insufficient stock for '{product.Name}'...");
  }
  ```

**Stock Deduction:**
- `OrderService.cs` (line 63-64): Deduct stock during order creation
  ```csharp
  var product = products.First(p => p.Id == lineReq.ProductId);
  product.UpdateStock(-lineReq.Quantity);
  ```

**Stock Adjustment Endpoint:**
- `ProductsController.cs` (line 156-170): Manual stock adjustment
  ```csharp
  [HttpPost("{id:guid}/stock")]
  public async Task<IActionResult> AdjustStock(Guid id, [FromBody] StockAdjustmentRequest request)
  {
      try
      {
          var product = await _productService.AdjustStockAsync(id, request);
          ...
      }
  }
  ```

### Consequences

**Positive:**
- **Validation:** Stock availability checked before deduction
- **Atomicity:** Stock update and order creation happen together
- **Auditability:** UpdatedAt timestamp tracks changes
- **Simplicity:** No complex locking mechanisms needed for demo

**Negative:**
- **Race Conditions:** In-memory database doesn't prevent concurrent updates
- **No Optimistic Locking:** No version field for conflict detection
- **Scalability:** Approach doesn't scale to distributed systems
- **Concurrency Issues:** Multiple concurrent orders could exceed available stock

**Trade-offs:**
- Chose validation-based approach over pessimistic locking for simplicity
- Chose in-memory database's implicit locking over explicit row-level locks
- Production would require optimistic locking with version fields or distributed transactions

---

## ADR-015: Async/Await - Asynchronous Operations Throughout

### Context

The application needed to handle multiple concurrent requests efficiently. The team decided on async/await patterns.

### Decision

**Use async/await throughout the application stack: controllers, services, and data access.**

**Evidence from codebase:**

**Async Controllers:**
- `ProductsController.cs` (line 24): Async action methods
  ```csharp
  public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
  {
      var products = await _productService.GetAllAsync(activeOnly);
      return Ok(products);
  }
  ```

**Async Services:**
- `ProductService.cs` (line 19-27): Async service methods
  ```csharp
  public async Task<IEnumerable<ProductDto>> GetAllAsync(bool activeOnly = true)
  {
      var query = _db.Products.Include(p => p.Category).AsQueryable();
      if (activeOnly)
          query = query.Where(p => p.IsActive);
      var products = await query.OrderBy(p => p.Name).ToListAsync();
      return products.Select(MapToDto);
  }
  ```

**Async Data Access:**
- `ProductService.cs` (line 31-36): Async EF Core queries
  ```csharp
  public async Task<ProductDto?> GetByIdAsync(Guid id)
  {
      var product = await _db.Products
          .Include(p => p.Category)
          .FirstOrDefaultAsync(p => p.Id == id);
      return product is null ? null : MapToDto(product);
  }
  ```

**Async Tests:**
- `ProductServiceTests.cs` (line 29-42): Async test methods
  ```csharp
  [Fact]
  public async Task GetAllAsync_ReturnsActiveProducts()
  {
      // Arrange
      _db.Products.AddRange(...);
      await _db.SaveChangesAsync();

      // Act
      var result = await _sut.GetAllAsync(activeOnly: true);

      // Assert
      result.Should().HaveCount(1);
  }
  ```

### Consequences

**Positive:**
- **Scalability:** Async operations free up thread pool threads for other requests
- **Responsiveness:** Application can handle more concurrent requests
- **Resource Efficiency:** Threads not blocked on I/O operations
- **Modern Pattern:** Async/await is standard in modern .NET applications

**Negative:**
- **Complexity:** Async code is more complex than synchronous equivalents
- **Debugging:** Async stack traces can be harder to follow
- **Testing:** Async tests require special handling
- **Performance Overhead:** Async has minimal overhead but not zero

**Trade-offs:**
- Chose async throughout for consistency and scalability
- Chose Task-based async over callback-based for readability
- No synchronous fallbacks (would complicate API)

---

## ADR-016: Nullable Reference Types - Enabled for Type Safety

### Context

The application needed to prevent null reference exceptions. The team enabled nullable reference types.

### Decision

**Enable nullable reference types (`<Nullable>enable</Nullable>`) in all projects for compile-time null safety.**

**Evidence from codebase:**

**Project Configuration:**
- `InventoryApi.csproj` (line 4): Nullable reference types enabled
  ```xml
  <Nullable>enable</Nullable>
  ```
- `InventoryApi.Tests.csproj` (line 4): Enabled in test project too
  ```xml
  <Nullable>enable</Nullable>
  ```

**Nullable Annotations:**
- `Product.cs` (line 18): Required non-nullable string
  ```csharp
  [Required]
  [MaxLength(200)]
  public string Name { get; set; } = string.Empty;
  ```
- `Product.cs` (line 23): Nullable string
  ```csharp
  [MaxLength(2000)]
  public string? Description { get; set; }
  ```
- `ProductDto.cs` (line 7): Nullable in DTO
  ```csharp
  public record ProductDto(
      ...
      string? Description,
      ...
      string? CategoryName,
      ...
  );
  ```

**Null-Coalescing:**
- `OrderService.cs` (line 95): Null-coalescing in mapping
  ```csharp
  o.Lines.Select(l => new OrderLineDto(l.Id, l.ProductId, l.ProductName, l.ProductSKU, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
  ```

### Consequences

**Positive:**
- **Type Safety:** Compiler prevents null reference exceptions
- **Documentation:** Nullable annotations document intent
- **Debugging:** Null-related bugs caught at compile time
- **Confidence:** Code is safer and more reliable

**Negative:**
- **Verbosity:** Requires explicit nullable annotations
- **Strictness:** Can be overly strict in some cases
- **Migration:** Existing code requires updates
- **Learning Curve:** Developers must understand nullable semantics

**Trade-offs:**
- Chose to enable globally for consistency
- Chose to use `string.Empty` instead of `null` for required strings
- Chose `?` annotation for optional fields

---

## ADR-017: Implicit Usings - Enabled for Cleaner Code

### Context

The application needed to reduce boilerplate in C# files. The team enabled implicit usings.

### Decision

**Enable implicit usings (`<ImplicitUsings>enable</ImplicitUsings>`) to automatically include common namespaces.**

**Evidence from codebase:**

**Project Configuration:**
- `InventoryApi.csproj` (line 5): Implicit usings enabled
  ```xml
  <ImplicitUsings>enable</ImplicitUsings>
  ```

**Result:**
- No explicit `using System;`, `using System.Linq;`, etc. in code files
- Cleaner file headers (e.g., `ProductService.cs` starts with namespace imports only)

### Consequences

**Positive:**
- **Cleaner Code:** Reduces boilerplate using statements
- **Readability:** Less visual clutter at top of files
- **Consistency:** Standard namespaces automatically included

**Negative:**
- **Discoverability:** Implicit namespaces less obvious to new developers
- **Debugging:** Harder to trace which namespace a type comes from
- **Customization:** Limited control over which namespaces are implicit

**Trade-offs:**
- Chose implicit usings for modern C# style
- Explicit imports still used for non-standard namespaces

---

## ADR-018: Record Types for DTOs - Immutability and Conciseness

### Context

The application needed immutable data transfer objects. Options included:
- Classes with properties
- Records
- Tuples

### Decision

**Use C# record types for all DTOs to provide immutability and concise syntax.**

**Evidence from codebase:**

**Record DTOs:**
- `ProductDto.cs` (line 5-20): ProductDto as record
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

**Request Records:**
- `ProductDto.cs` (line 22-32): CreateProductRequest as record
  ```csharp
  public record CreateProductRequest(
      [Required][MaxLength(200)] string Name,
      [Required][MaxLength(100)] string SKU,
      [MaxLength(2000)] string? Description,
      [Range(0, double.MaxValue)] decimal Price,
      ...
  );
  ```

### Consequences

**Positive:**
- **Immutability:** Records are immutable by default
- **Conciseness:** Positional parameters reduce boilerplate
- **Value Semantics:** Records use value-based equality
- **Deconstruction:** Records support deconstruction syntax
- **Modern:** Records are modern C# feature

**Negative:**
- **Compatibility:** Records are C# 9+ feature
- **Complexity:** Record syntax can be confusing for beginners
- **Serialization:** JSON serialization requires careful configuration
- **Mutability:** Immutability can be limiting in some scenarios

**Trade-offs:**
- Chose records over classes for immutability
- Chose positional records for conciseness
- Chose records over tuples for named properties

---

## ADR-019: Eager Loading - Include() for Related Entities

### Context

The application has relationships between entities. The team needed to decide on loading strategies.

### Decision

**Use explicit eager loading with `.Include()` to load related entities in a single query.**

**Evidence from codebase:**

**Product-Category Eager Loading:**
- `ProductService.cs` (line 20): Include category in GetAllAsync
  ```csharp
  var query = _db.Products.Include(p => p.Category).AsQueryable();
  ```
- `ProductService.cs` (line 31-33): Include in GetByIdAsync
  ```csharp
  var product = await _db.Products
      .Include(p => p.Category)
      .FirstOrDefaultAsync(p => p.Id == id);
  ```

**Order-OrderLine Eager Loading:**
- `OrderService.cs` (line 20-24): Include lines and products
  ```csharp
  var orders = await _db.Orders
      .Include(o => o.Lines)
      .ThenInclude(l => l.Product)
      .OrderByDescending(o => o.CreatedAt)
      .ToListAsync();
  ```

**Category-Products Eager Loading:**
- `CategoryService.cs` (line 20-24): Include products in GetAllAsync
  ```csharp
  var categories = await _db.Categories
      .Include(c => c.Products)
      .OrderBy(c => c.Name)
      .ToListAsync();
  ```

### Consequences

**Positive:**
- **Performance:** Single query instead of N+1 queries
- **Simplicity:** Explicit and easy to understand
- **Completeness:** All related data loaded upfront
- **Predictability:** Query behavior is explicit

**Negative:**
- **Over-fetching:** Loads all related data even if not needed
- **Memory:** Large result sets consume more memory
- **Complexity:** Multiple includes can create complex queries
- **Flexibility:** Difficult to load different subsets for different use cases

**Trade-offs:**
- Chose eager loading over lazy loading to avoid N+1 queries
- Chose eager loading over explicit queries for simplicity
- Chose ThenInclude for nested relationships

---

## ADR-020: Middleware Pipeline - Request Logging Middleware

### Context

The application needed cross-cutting concerns like request logging. The team implemented custom middleware.

### Decision

**Implement custom RequestLoggingMiddleware to log all requests with correlation IDs and response times.**

**Evidence from codebase:**

**Middleware Implementation:**
- `RequestLoggingMiddleware.cs` (line 6-56): Custom middleware class
  ```csharp
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
          var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                              ?? Guid.NewGuid().ToString("N")[..8];
          context.Response.Headers["X-Correlation-ID"] = correlationId;
          
          var sw = Stopwatch.StartNew();
          
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
              var level = context.Response.StatusCode >= 500
                  ? LogLevel.Error
                  : context.Response.StatusCode >= 400
                      ? LogLevel.Warning
                      : LogLevel.Information;

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

**Middleware Registration:**
- `Program.cs` (line 53): Register middleware in pipeline
  ```csharp
  app.UseMiddleware<RequestLoggingMiddleware>();
  ```

**Middleware Pipeline Order:**
- `Program.cs` (line 49-57): Middleware registration order
  ```csharp
  if (app.Environment.IsDevelopment())
  {
      app.UseSwagger();
      app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
  }

  app.UseMiddleware<RequestLoggingMiddleware>();  // Custom logging middleware
  app.UseCors();                                    // CORS middleware
  app.UseHttpsRedirection();                        // HTTPS redirection
  app.UseAuthorization();                           // Authorization
  app.MapControllers();                             // Route mapping
  ```

**Pipeline Implications:**
- RequestLoggingMiddleware is registered before CORS, allowing it to capture all requests including CORS preflight requests
- Middleware executes in reverse order during response phase, so logging happens after all downstream middleware completes
- Correlation ID is set in response headers before returning to client, enabling end-to-end request tracing

### Consequences

**Positive:**
- **Observability:** All requests logged with timing information
- **Correlation:** Correlation IDs enable request tracing
- **Debugging:** Logs help diagnose issues
- **Monitoring:** Request metrics available for analysis
- **Flexibility:** Custom middleware allows fine-grained control

**Negative:**
- **Performance:** Logging adds overhead to every request
- **Complexity:** Custom middleware requires careful implementation
- **Maintenance:** Middleware must be maintained and tested
- **Scalability:** Logging to console doesn't scale to distributed systems

**Trade-offs:**
- Chose custom middleware over third-party logging frameworks for simplicity
- Chose console logging over file/cloud logging for demo purposes
- Chose correlation IDs for request tracing without full distributed tracing

---

## ADR-021: Seed Data - In-Memory Database Population on Startup

### Context

The application needed sample data for demonstration and testing. The team implemented database seeding.

### Decision

**Populate the in-memory database with seed data on application startup using a static SeedData method.**

**Evidence from codebase:**

**Seed Data Method:**
- `InventoryDbContext.cs` (line 47-73): SeedData static method
  ```csharp
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
          ...
      );

      db.SaveChanges();
  }
  ```

**Seed Data Invocation:**
- `Program.cs` (line 44-48): Call SeedData on startup
  ```csharp
  using (var scope = app.Services.CreateScope())
  {
      var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
      db.Database.EnsureCreated();
      InventoryDbContext.SeedData(db);
  }
  ```

### Consequences

**Positive:**
- **Demo-Ready:** Application starts with sample data
- **Testing:** Seed data provides consistent test data
- **Exploration:** Users can immediately explore API without creating data
- **Documentation:** Seed data serves as usage examples

**Negative:**
- **Startup Time:** Seeding adds to application startup time
- **Hardcoded Data:** Seed data is hardcoded; difficult to change
- **Scalability:** Seeding approach doesn't scale to large datasets
- **Maintenance:** Seed data must be updated manually

**Trade-offs:**
- Chose in-memory seeding over external data files for simplicity
- Chose static method over EF Core data seeding for flexibility
- Chose idempotent seeding (checks if data exists) to allow multiple runs

---

## ADR-023: Slug Generation Strategy - Auto-Generated URL-Friendly Identifiers

### Context

The application needed URL-friendly identifiers for categories to support SEO-friendly URLs and human-readable references. The team needed to decide between:
- Manual slug entry
- Auto-generated slugs from names
- Hybrid approach with optional override

### Decision

**Auto-generate URL-friendly slugs from category names, with optional manual override via request parameter.**

**Evidence from codebase:**

**Slug Generation Logic:**
- `CategoryService.cs` (line 48): Auto-generation with optional override
  ```csharp
  var slug = request.Slug ?? request.Name.ToLower().Replace(" ", "-").Replace("&", "and");
  ```

**Slug in Category Model:**
- `Category.cs` (line 16): Slug property
  ```csharp
  [MaxLength(50)]
  public string? Slug { get; set; }
  ```

**Slug in DTO:**
- `ProductDto.cs` (line 51): Slug included in CategoryDto response
  ```csharp
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
  ```

**Request DTO:**
- `ProductDto.cs` (line 57-60): Optional slug in request
  ```csharp
  public record CreateCategoryRequest(
      [Required][MaxLength(100)] string Name,
      [MaxLength(500)] string? Description,
      [MaxLength(50)] string? Slug
  );
  ```

### Consequences

**Positive:**
- **SEO-Friendly:** Slugs enable clean URLs like `/categories/electronics`
- **User-Friendly:** Slugs are human-readable and memorable
- **Automation:** Auto-generation reduces manual data entry
- **Flexibility:** Optional override allows custom slugs when needed
- **Consistency:** Standardized slug format across categories

**Negative:**
- **Uniqueness:** No uniqueness constraint on slugs; duplicates possible
- **Collision Risk:** Different names could generate same slug
- **Immutability:** Slug doesn't update if category name changes
- **Complexity:** String manipulation adds processing overhead

**Trade-offs:**
- Chose auto-generation with optional override for balance between automation and control
- Chose simple string replacement over sophisticated slug libraries for minimal dependencies
- No uniqueness constraint enforced (acceptable for demo)

---

## ADR-024: Order Number Generation - Static Counter-Based Sequencing

### Context

The application needed to generate unique, human-readable order numbers. Options included:
- Database sequences
- GUIDs
- Static counter
- Timestamp-based
- Custom format with counter

### Decision

**Use a static thread-safe counter with `Interlocked.Increment()` to generate sequential order numbers in format `ORD-XXXXXX`.**

**Evidence from codebase:**

**Counter Implementation:**
- `OrderService.cs` (line 11): Static counter initialization
  ```csharp
  private static int _orderCounter = 1000;
  ```

**Order Number Generation:**
- `OrderService.cs` (line 60): Thread-safe increment with formatting
  ```csharp
  var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
  ```

**Unique Index:**
- `InventoryDbContext.cs` (line 28): Uniqueness enforced at database level
  ```csharp
  entity.HasIndex(o => o.OrderNumber).IsUnique();
  ```

### Consequences

**Positive:**
- **Human-Readable:** Order numbers are easy to reference and communicate
- **Sequential:** Numbers increase predictably, useful for auditing
- **Thread-Safe:** `Interlocked.Increment()` ensures no duplicates in concurrent scenarios
- **Simple:** No external dependencies or database sequences needed
- **Efficient:** Fast generation with minimal overhead

**Negative:**
- **Non-Persistent:** Counter resets on application restart
- **Predictable:** Sequential numbers reveal order volume
- **Single-Instance:** Doesn't work in distributed/multi-instance deployments
- **Collision Risk:** If counter resets, could generate duplicate numbers
- **Not Production-Ready:** In-memory counter unsuitable for production

**Trade-offs:**
- Chose static counter over database sequences for simplicity in demo
- Chose `Interlocked.Increment()` over locks for performance
- Chose format `ORD-XXXXXX` for readability and consistency
- Production would require database sequences or distributed ID generation

---

## ADR-025: Soft Delete vs Hard Delete - Hard Delete Strategy

### Context

The application needed to decide on deletion behavior for entities. Options included:
- Hard delete (permanent removal)
- Soft delete (logical deletion with IsActive flag)
- Hybrid approach (soft delete with hard delete option)

### Decision

**Use hard delete (permanent removal) for all entities. Soft delete via IsActive flag used only for deactivation, not deletion.**

**Evidence from codebase:**

**Hard Delete Implementation:**
- `ProductService.cs` (line 165-170): Hard delete removes entity
  ```csharp
  public async Task<bool> DeleteAsync(Guid id)
  {
      var product = await _db.Products.FindAsync(id);
      if (product is null) return false;

      _db.Products.Remove(product);
      await _db.SaveChangesAsync();

      _logger.LogInformation("Deleted product {Id}", id);
      return true;
  }
  ```

- `CategoryService.cs` (line 82-89): Hard delete with constraint check
  ```csharp
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

**IsActive Flag for Deactivation:**
- `Product.cs` (line 35): IsActive flag for soft deactivation
  ```csharp
  public bool IsActive { get; set; } = true;
  ```

- `ProductService.cs` (line 20-24): Filtering by IsActive in queries
  ```csharp
  var query = _db.Products.Include(p => p.Category).AsQueryable();

  if (activeOnly)
      query = query.Where(p => p.IsActive);
  ```

### Consequences

**Positive:**
- **Simplicity:** Hard delete is straightforward; no soft delete logic needed
- **Data Cleanup:** Permanently removes unwanted data
- **Storage:** Doesn't accumulate deleted records over time
- **Performance:** No need to filter soft-deleted records in queries
- **Clarity:** Clear distinction between active/inactive (IsActive) and deleted

**Negative:**
- **Data Loss:** Permanent removal; no recovery option
- **Audit Trail:** No history of deleted entities
- **Compliance:** May violate data retention requirements
- **Relationships:** Requires careful handling of foreign keys (see ADR-013)

**Trade-offs:**
- Chose hard delete for simplicity in demo application
- Chose IsActive flag for deactivation without deletion
- Production would likely require soft delete for audit trails and compliance
- Constraint checks prevent deletion of categories with products (referential integrity)

---

## ADR-026: Computed Properties - Expression-Bodied Members for Derived Values

### Context

The application needed to compute derived values from entity properties. Options included:
- Stored columns in database
- Computed properties in C#
- Database computed columns
- Separate calculation methods

### Decision

**Use C# expression-bodied computed properties for derived values that don't require persistence.**

**Evidence from codebase:**

**Product IsLowStock:**
- `Product.cs` (line 52): Computed property
  ```csharp
  public bool IsLowStock => StockQuantity <= ReorderPoint;
  ```

**Category ProductCount:**
- `Category.cs` (line 27): Computed property
  ```csharp
  public int ProductCount => Products.Count;
  ```

**OrderLine LineTotal:**
- `Order.cs` (line 73): Computed property
  ```csharp
  public decimal LineTotal => Quantity * UnitPrice;
  ```

**Order RecalculateTotal Method:**
- `Order.cs` (line 47-51): Method to recalculate total
  ```csharp
  public void RecalculateTotal()
  {
      TotalAmount = Lines.Sum(l => l.LineTotal);
      UpdatedAt = DateTime.UtcNow;
  }
  ```

**DTO Mapping with Computed Values:**
- `ProductService.cs` (line 177-180): IsLowStock included in DTO
  ```csharp
  private static ProductDto MapToDto(Product p) => new(
      p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
      p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
      p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
      p.CreatedAt, p.UpdatedAt
  );
  ```

### Consequences

**Positive:**
- **Simplicity:** No database columns needed for derived values
- **Consistency:** Always calculated from current data
- **Maintainability:** Logic in code, not database
- **Flexibility:** Easy to change calculation logic
- **Performance:** Minimal overhead for simple calculations

**Negative:**
- **Recalculation:** Computed on every access; no caching
- **Database Queries:** Can't filter by computed properties in LINQ queries
- **Complexity:** Complex calculations difficult to express as expressions
- **Performance:** Expensive calculations repeated unnecessarily

**Trade-offs:**
- Chose computed properties over stored columns for simplicity
- Chose expression-bodied syntax for conciseness
- Chose method (RecalculateTotal) for complex calculations
- Production might cache computed values for performance

---

## ADR-027: Search Implementation - LINQ Contains() for Full-Text Search

### Context

The application needed to search products by multiple fields. Options included:
- Database full-text search
- LINQ Contains() filtering
- Elasticsearch
- Lucene.NET

### Decision

**Use LINQ Contains() for in-memory filtering across multiple product fields (name, SKU, description, brand).**

**Evidence from codebase:**

**Search Method:**
- `ProductService.cs` (line 48-56): Multi-field search with Contains()
  ```csharp
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

**Search Endpoint:**
- `ProductsController.cs` (line 60-67): Search route
  ```csharp
  /// <summary>Search products by name, SKU, description or brand</summary>
  [HttpGet("search")]
  [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
  public async Task<IActionResult> Search([FromQuery] string q = "")
  {
      var results = await _productService.SearchAsync(q);
      return Ok(results);
  }
  ```

### Consequences

**Positive:**
- **Simplicity:** No external dependencies or complex setup
- **Flexibility:** Easy to add/remove search fields
- **Consistency:** Works with in-memory database
- **Readability:** Clear intent in code

**Negative:**
- **Performance:** Case-insensitive search requires ToLower() on all records
- **Scalability:** Doesn't scale to large datasets
- **Relevance:** No ranking or relevance scoring
- **Complexity:** Complex queries difficult to express
- **Database Inefficiency:** Full table scan required

**Trade-offs:**
- Chose LINQ Contains() over database full-text search for simplicity
- Chose case-insensitive search for better UX
- Chose multiple fields for comprehensive search
- Production would require Elasticsearch or database full-text search for performance

---

## ADR-028: Order Status Lifecycle - Enum-Based State Machine

### Context

The application needed to manage order states through their lifecycle. Options included:
- String-based status
- Enum-based status
- State machine pattern
- Database lookup table

### Decision

**Use C# enum for order status with predefined valid states: Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Refunded.**

**Evidence from codebase:**

**OrderStatus Enum:**
- `Order.cs` (line 8-15): Enum definition
  ```csharp
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
  ```

**Status Property:**
- `Order.cs` (line 30): Status property with default
  ```csharp
  public OrderStatus Status { get; set; } = OrderStatus.Pending;
  ```

**Status Update Endpoint:**
- `OrdersController.cs` (line 68-79): Update status with validation
  ```csharp
  [HttpPatch("{id:guid}/status")]
  [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateOrderStatusRequest request)
  {
      if (!Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status))
          return BadRequest(new { message = $"Invalid status '{request.Status}'. Valid values: {string.Join(", ", Enum.GetNames<OrderStatus>())}" });

      var order = await _orderService.UpdateStatusAsync(id, status);
      if (order is null)
          return NotFound(new { message = $"Order {id} not found." });

      return Ok(order);
  }
  ```

**Status in DTO:**
- `ProductDto.cs` (line 76): Status as string in response
  ```csharp
  public record OrderDto(
      Guid Id,
      string OrderNumber,
      string CustomerName,
      string? CustomerEmail,
      string Status,  // Converted from enum to string
      decimal TotalAmount,
      ...
  );
  ```

**Status Conversion:**
- `OrderService.cs` (line 113-118): Enum to string conversion in mapping
  ```csharp
  private static OrderDto MapToDto(Order o) => new(
      o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
      o.Status.ToString(), o.TotalAmount, o.ShippingAddress, o.Notes,
      ...
  );
  ```

### Consequences

**Positive:**
- **Type Safety:** Enum prevents invalid status values
- **Discoverability:** Valid states visible in code
- **Simplicity:** No database lookup needed
- **Performance:** Enum stored as integer in database
- **Validation:** Compiler enforces valid values

**Negative:**
- **Inflexibility:** Adding new statuses requires code change
- **No Transitions:** No validation of valid state transitions
- **Serialization:** Enum must be converted to string for JSON
- **Extensibility:** Hard to extend with custom statuses

**Trade-offs:**
- Chose enum over string for type safety
- Chose predefined states for simplicity
- No state machine validation (acceptable for demo)
- Production might implement state machine pattern for complex workflows

---

## ADR-029: Decimal Precision - Column Type Specification for Financial Data

### Context

The application handles financial data (prices, costs, totals) and measurements (weight). The team needed to decide on decimal precision.

### Decision

**Use `decimal(18,2)` for prices and totals, `decimal(10,2)` for weights to match financial and measurement precision requirements.**

**Evidence from codebase:**

**Price and Cost Columns:**
- `Product.cs` (line 20): Price column type
  ```csharp
  [Column(TypeName = "decimal(18,2)")]
  [Range(0, double.MaxValue)]
  public decimal Price { get; set; }
  ```

- `Product.cs` (line 26): Cost column type
  ```csharp
  [Column(TypeName = "decimal(18,2)")]
  [Range(0, double.MaxValue)]
  public decimal Cost { get; set; }
  ```

**Weight Column:**
- `Product.cs` (line 40): Weight column type
  ```csharp
  [Column(TypeName = "decimal(10,2)")]
  public decimal? WeightKg { get; set; }
  ```

**Order Total:**
- `Order.cs` (line 32): Order total column type
  ```csharp
  [Column(TypeName = "decimal(18,2)")]
  public decimal TotalAmount { get; set; }
  ```

**OrderLine Unit Price and Total:**
- `Order.cs` (line 68): Unit price column type
  ```csharp
  [Column(TypeName = "decimal(18,2)")]
  public decimal UnitPrice { get; set; }
  ```

### Consequences

**Positive:**
- **Precision:** 2 decimal places matches currency standards
- **Range:** 18,2 supports large amounts (up to 999,999,999,999,999.99)
- **Accuracy:** Avoids floating-point rounding errors
- **Standards:** Follows financial industry conventions
- **Consistency:** Uniform precision across financial fields

**Negative:**
- **Storage:** Decimal takes more space than float
- **Performance:** Decimal arithmetic slower than float
- **Complexity:** Different precisions for different fields
- **Maintenance:** Must remember to specify column types

**Trade-offs:**
- Chose `decimal(18,2)` for prices to support large amounts with 2 decimal places
- Chose `decimal(10,2)` for weights to support up to 99,999,999.99 kg with 2 decimal places
- Chose explicit column type specification over defaults for clarity

---

## ADR-030: Category Deletion Constraints - Referential Integrity Enforcement

### Context

The application needed to prevent deletion of categories that contain products. The team needed to decide on enforcement strategy.

### Decision

**Prevent deletion of non-empty categories by checking for related products before deletion and throwing an exception.**

**Evidence from codebase:**

**Deletion Constraint Check:**
- `CategoryService.cs` (line 82-83): Check for related products
  ```csharp
  if (category.Products.Any())
      throw new InvalidOperationException("Cannot delete a category that contains products. Reassign or delete products first.");
  ```

**Full Delete Method:**
- `CategoryService.cs` (line 78-89): Complete deletion logic
  ```csharp
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

**Database-Level Constraint:**
- `InventoryDbContext.cs` (line 20-24): Foreign key with Restrict delete behavior
  ```csharp
  entity.HasOne(p => p.Category)
        .WithMany(c => c.Products)
        .HasForeignKey(p => p.CategoryId)
        .OnDelete(DeleteBehavior.Restrict);
  ```

**Controller Error Handling:**
- `CategoriesController.cs` (line 85-91): Exception handling
  ```csharp
  catch (InvalidOperationException ex)
  {
      return BadRequest(new { message = ex.Message });
  }
  ```

### Consequences

**Positive:**
- **Data Integrity:** Prevents orphaned products
- **User Feedback:** Clear error message explains why deletion failed
- **Consistency:** Matches database constraint (DeleteBehavior.Restrict)
- **Safety:** Prevents accidental data loss

**Negative:**
- **Inflexibility:** Can't delete category without reassigning products
- **Complexity:** Requires additional logic in service
- **User Experience:** Users must handle products before deletion
- **Cascading:** No automatic cascade delete option

**Trade-offs:**
- Chose explicit check over cascade delete to prevent data loss
- Chose service-level validation over relying only on database constraint
- Chose clear error message for better UX
- Production might offer cascade delete with confirmation

---

## Summary of Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| **Framework** | ASP.NET Core 9 | Modern, performant, enterprise-ready |
| **Database** | EF Core In-Memory | Zero configuration, demo-friendly |
| **Architecture** | Layered (3-tier) | Clear separation of concerns, testable |
| **DI Container** | Built-in ASP.NET Core | No external dependencies |
| **API Style** | REST with JSON | Simple, cacheable, widely supported |
| **DTOs** | Explicit records | Immutable, decoupled from domain |
| **Validation** | Data annotations + services | Declarative + business logic |
| **Error Handling** | Exception-based | Familiar, flexible status code mapping |
| **Logging** | Console with correlation IDs | Simple, traceable |
| **Testing** | xUnit + Fluent Assertions | Modern, readable, fast |
| **CORS** | Allow all origins | Demo-friendly, not production-ready |
| **Documentation** | Swagger/OpenAPI | Auto-generated, interactive |
| **Relationships** | Eager loading with Include() | Prevents N+1 queries |
| **Async/Await** | Throughout stack | Scalable, responsive |
| **Nullable Types** | Enabled | Type-safe, prevents null exceptions |
| **Middleware** | Custom request logging | Observable, traceable requests |
| **Seed Data** | In-memory on startup | Demo-ready, no external dependencies |
| **Slug Generation** | Auto-generated from name | SEO-friendly, optional override |
| **Order Numbers** | Static counter with Interlocked | Human-readable, thread-safe |
| **Deletion** | Hard delete with constraints | Simple, prevents orphaned data |
| **Computed Properties** | Expression-bodied members | Derived values without persistence |
| **Search** | LINQ Contains() | Simple, flexible, in-memory friendly |
| **Order Status** | Enum-based | Type-safe, predefined states |
| **Decimal Precision** | 18,2 for prices, 10,2 for weights | Financial accuracy, standards-compliant |
| **Category Deletion** | Constraint check with exception | Prevents orphaned products |

---

## Trade-offs and Future Considerations

### Production Readiness

The current architecture is optimized for demonstration and learning. For production deployment:

1. **Database:** Replace in-memory with SQL Server, PostgreSQL, or cloud database
2. **Logging:** Integrate Serilog with structured logging and cloud sinks (Application Insights, DataDog)
3. **Authentication:** Add JWT or OAuth2 authentication
4. **Authorization:** Implement role-based access control (RBAC)
5. **Caching:** Add Redis for distributed caching
6. **Monitoring:** Integrate Application Insights or similar APM
7. **CORS:** Restrict to specific trusted origins
8. **Rate Limiting:** Implement rate limiting and throttling
9. **Validation:** Add FluentValidation for complex validation rules
10. **Concurrency:** Implement optimistic locking with version fields
11. **Order Numbers:** Use database sequences or distributed ID generation
12. **Search:** Implement Elasticsearch for full-text search
13. **Soft Deletes:** Implement soft delete for audit trails and compliance
14. **State Machine:** Implement state machine pattern for order status transitions

### Scalability Considerations

For scaling to handle higher load:

1. **Horizontal Scaling:** Containerize with Docker, deploy to Kubernetes
2. **Database:** Implement read replicas, sharding for large datasets
3. **Caching:** Implement distributed caching layer
4. **Async Processing:** Use message queues (RabbitMQ, Azure Service Bus) for long-running operations
5. **API Gateway:** Add API gateway for routing, rate limiting, authentication
6. **Monitoring:** Implement distributed tracing (Application Insights, Jaeger)
7. **Pagination:** Implement pagination for list endpoints
8. **Computed Properties:** Cache computed values for performance

### Maintainability Improvements

For long-term maintainability:

1. **Testing:** Add integration and end-to-end tests
2. **Documentation:** Maintain architecture decision records (ADRs)
3. **Code Quality:** Implement static analysis (SonarQube, Roslyn analyzers)
4. **CI/CD:** Automate testing, building, and deployment
5. **Versioning:** Implement semantic versioning for API
6. **Deprecation:** Plan for API versioning and deprecation strategy
7. **Constants:** Extract magic strings to configuration
8. **Logging:** Add logging for read operations
9. **Repository Pattern:** Consider adding repository abstraction for testability
10. **Audit Logging:** Implement audit trail for product/order changes

---

## Conclusion

The **demo-inventory-csharp** project demonstrates a well-structured, modern ASP.NET Core application with clear architectural decisions. The layered architecture, explicit DTOs, comprehensive testing, and observability features provide a solid foundation for learning and demonstration purposes. While optimized for simplicity and ease of deployment, the architecture can be extended with production-grade features as needed.

The 30 architecture decision records (ADRs) document not only the major technology and pattern choices but also the specific implementation details, trade-offs, and future considerations. This comprehensive documentation serves as a reference for developers, architects, and stakeholders to understand the reasoning behind each decision and the implications for the system's evolution.