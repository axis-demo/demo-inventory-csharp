alwaysApply: true

# .cursorrules - demo-inventory-csharp

This .cursorrules file serves as the AI assistant knowledge index for context-first development.
Whenever working in Cursor, the following documents must be preloaded and treated as the base 
architectural context before making any recommendations, refactoring, or implementing features.

## Mandatory Reference Documents

[docs/architecture.md](docs/architecture.md) - System architecture and design patterns
[docs/structure.md](docs/structure.md) - Project organization and folder structure
[docs/code.md](docs/code.md) - Code patterns and conventions
[docs/dataflow.md](docs/dataflow.md) - Data flow and system boundaries
[docs/decisions.md](docs/decisions.md) - Architectural decisions and rationale
[docs/glossary.md](docs/glossary.md) - Domain terminology and definitions
[docs/risk.md](docs/risk.md) - Security and risk management

## Purpose: Why These Docs Are Mandatory

These documentation packs collectively describe:

- **Architecture & Design**: System design patterns, layered architecture (Controllers → Services → Data), 
  dependency injection patterns, middleware pipeline, and API versioning strategy
- **Coding Standards & Patterns**: C# conventions, async/await patterns, record types for DTOs, 
  null safety (nullable reference types enabled), logging standards, and error handling
- **Data Flows & State Logic**: Entity relationships (Product ↔ Category, Order ↔ OrderLine), 
  stock management logic, order lifecycle (Pending → Confirmed → Processing → Shipped → Delivered), 
  and data persistence via EF Core InMemory
- **Security & Risk Management**: Input validation, authorization boundaries, data protection, 
  correlation ID tracking, and audit logging
- **Domain Terminology**: Products, Categories, Orders, SKU, Stock Quantity, Reorder Point, 
  Order Status, and business rules

**These docs are mandatory reading before:**
- Implementing new features or API endpoints
- Debugging issues or refactoring code
- Adding database models or modifying relationships
- Changing service layer logic or business rules
- Updating DTOs or request/response contracts

---

## Cursor Development Rules

### 1. Preload Context First
- Parse all mandatory reference documents before making any code recommendations
- Cross-reference architecture.md for design patterns before suggesting new classes
- Verify data flow in dataflow.md before proposing database changes
- Check code.md for naming conventions and patterns before writing code

### 2. Single Source of Truth
- Treat /docs as authoritative for architecture, standards, dataflows, and security
- Do not suggest patterns or approaches that contradict documented decisions
- When code conflicts with docs, flag it as a discrepancy and recommend alignment
- All architectural decisions must be documented in decisions.md

### 3. Cross-Check Ambiguous Details
- When code behavior is unclear, search the codebase for similar patterns
- Use CodeTools-search_code to find existing implementations before suggesting new ones
- Verify API contracts against DTOs and controllers before recommending changes
- Check service layer logic against business rules in docs

### 4. Update Docs as Part of Completion
- All new features must include documentation updates in /docs
- New API endpoints require updates to dataflow.md and architecture.md
- New domain entities require glossary.md updates
- Breaking changes require decisions.md entries with rationale
- Features are only "done" when both code AND docs are updated

### 5. TODO Management & Task Breakdown
- Break down complex tasks into subtasks with [ ] checkboxes
- Use [x] for completed items, [ ] for pending, [-] for blocked/deferred
- Track documentation updates as separate checklist items
- Link TODOs to specific files or sections in /docs

### 6. No Hallucination: Verify Before Suggesting
- Only suggest APIs, classes, or logic that exist in the repository or /docs
- Use CodeTools-search_code to verify class names, method signatures, and namespaces
- Do not invent new service methods without checking existing patterns
- Verify NuGet package versions against .csproj files before recommending usage

### 7. Verification-First Approach
- Use CodeTools-read_file to examine actual implementations before making recommendations
- Use CodeTools-search_code to find patterns and conventions in the codebase
- Use CodeTools-find_similar to locate related code before suggesting changes
- Verify that suggested changes don't break existing tests

### 8. Consistency Required
- All changes must align with architecture.md (layered architecture, DI patterns)
- All code must follow conventions in code.md (naming, async patterns, error handling)
- All data changes must respect relationships defined in dataflow.md
- All security changes must comply with risk.md guidelines

### 9. Security Checks
- Analyze input validation: All DTOs must have [Required] and [MaxLength] attributes
- Check authorization: Verify that sensitive operations are protected
- Verify secrets management: No hardcoded credentials, use configuration
- Assess external API risk: Validate third-party dependencies in .csproj
- Ensure correlation ID tracking for audit trails (RequestLoggingMiddleware pattern)

### 10. Performance Checks
- Evaluate query efficiency: Check for N+1 problems, use .Include() for eager loading
- Assess scalability: Consider InMemory database limitations for production
- Optimize polling: Avoid unnecessary database queries in loops
- Evaluate HTTP batching: Consider combining multiple API calls
- Observable impacts: Estimate response time changes, memory usage, CPU load

---

## Project-Specific Verification Rules

### Technology Stack
- **Framework**: ASP.NET Core 9 (net9.0)
- **Database**: Entity Framework Core 9.0.0 with InMemory provider
- **API Documentation**: Swagger/Swashbuckle 6.5.0
- **Testing**: xUnit 2.6.1, Moq 4.20.69, FluentAssertions 6.12.0
- **Language Features**: C# 13 with nullable reference types enabled, implicit usings enabled

### Code Quality Standards

#### C# Conventions
- [ ] Use nullable reference types: `#nullable enable` at file level or project-wide
- [ ] Prefer records for DTOs: `public record ProductDto(...)`
- [ ] Use async/await for all I/O operations: `public async Task<T> MethodAsync()`
- [ ] Use expression-bodied members for simple properties: `public bool IsLowStock => StockQuantity <= ReorderPoint`
- [ ] Use LINQ for queries: `.Where()`, `.Select()`, `.OrderBy()`, `.Include()` for EF Core
- [ ] Null-coalescing operators: `?? null` for default values
- [ ] Null-conditional operators: `?.` for safe navigation

#### Type Safety & Validation
- [ ] All DTOs must have data annotations: `[Required]`, `[MaxLength(n)]`, `[Range(min, max)]`
- [ ] All entity models must have `[Key]` and `[ForeignKey]` attributes
- [ ] Use `Guid` for entity IDs, not `int` or `string`
- [ ] Use `decimal(18,2)` for monetary values: `[Column(TypeName = "decimal(18,2)")]`
- [ ] Use enums for fixed sets: `OrderStatus` enum instead of string status
- [ ] Validate category existence before creating products: `categoryExists = await _db.Categories.AnyAsync(...)`

#### Complexity Limits
- [ ] Service methods should not exceed 50 lines (excluding logging and comments)
- [ ] Controllers should delegate to services, not contain business logic
- [ ] Avoid deeply nested conditionals: max 3 levels
- [ ] Use guard clauses to reduce nesting: `if (product is null) return null;`

#### Test Coverage
- [ ] Unit tests for all service methods using xUnit
- [ ] Use `IDisposable` for test cleanup: `public void Dispose() => _db.Dispose();`
- [ ] Use FluentAssertions for readable assertions: `.Should().Be()`, `.Should().HaveCount()`
- [ ] Mock dependencies with Moq: `new Mock<ILogger<T>>()`
- [ ] Test both happy path and error cases (duplicate SKU, insufficient stock, invalid category)
- [ ] Minimum 80% code coverage for services

### Development Workflow

#### Branch Strategy
- [ ] Feature branches: `feature/product-search`, `feature/order-status-update`
- [ ] Bugfix branches: `bugfix/stock-calculation-error`
- [ ] Hotfix branches: `hotfix/critical-security-issue`
- [ ] Main branch: Always deployable, all tests passing

#### Commit Standards
- [ ] Commit messages: `[FEATURE] Add product search endpoint` or `[FIX] Correct stock deduction logic`
- [ ] Atomic commits: One logical change per commit
- [ ] Include issue/ticket reference: `[FEATURE] Add product search (#42)`
- [ ] No debug code, console.WriteLine, or commented-out code in commits

#### PR Requirements
- [ ] All tests passing: `dotnet test` succeeds
- [ ] Code review approval from at least one team member
- [ ] Documentation updated: /docs changes included
- [ ] No merge conflicts
- [ ] Branch up-to-date with main

### ASP.NET Core & EF Core Guidelines

#### Controller Patterns
- [ ] Use `[ApiController]` and `[Route("api/v1/[controller]")]` attributes
- [ ] Use `[ProducesResponseType]` for all response codes: 200, 201, 400, 404, 409, 500
- [ ] Return `IActionResult` for flexibility: `Ok()`, `Created()`, `BadRequest()`, `NotFound()`
- [ ] Use `[FromBody]` for request bodies, `[FromQuery]` for query parameters
- [ ] Log warnings for business logic failures: `_logger.LogWarning("Failed to create product: {Message}", ex.Message)`
- [ ] Validate `ModelState.IsValid` before processing requests

#### Service Layer Patterns
- [ ] Inject `InventoryDbContext` and `ILogger<T>` via constructor
- [ ] Use async methods: `public async Task<T> GetByIdAsync(Guid id)`
- [ ] Use `.Include()` for eager loading related entities: `.Include(p => p.Category)`
- [ ] Use `.FirstOrDefaultAsync()` for single entity queries
- [ ] Use `.ToListAsync()` for collection queries
- [ ] Throw `InvalidOperationException` for business rule violations
- [ ] Log important operations: `_logger.LogInformation("Created product {SKU}", product.SKU)`

#### EF Core Data Access
- [ ] Use `DbSet<T>` properties: `public DbSet<Product> Products => Set<Product>();`
- [ ] Configure relationships in `OnModelCreating`: `.HasOne()`, `.WithMany()`, `.HasForeignKey()`
- [ ] Use `DeleteBehavior.Restrict` for referential integrity: prevent deleting categories with products
- [ ] Use `DeleteBehavior.Cascade` for dependent entities: delete order lines when order is deleted
- [ ] Create unique indexes: `.HasIndex(p => p.SKU).IsUnique()`
- [ ] Seed data in `SeedData()` static method called from Program.cs

#### Middleware & Logging
- [ ] Implement `RequestLoggingMiddleware` for correlation IDs and request/response logging
- [ ] Use `X-Correlation-ID` header for request tracing
- [ ] Log request start: method, path, query string
- [ ] Log response: status code, elapsed time in milliseconds
- [ ] Use appropriate log levels: `Information` for normal operations, `Warning` for business errors, `Error` for exceptions

### Security Requirements

#### Input Validation
- [ ] All DTOs must have `[Required]` on mandatory fields
- [ ] All string fields must have `[MaxLength(n)]` to prevent buffer overflows
- [ ] All numeric fields must have `[Range(min, max)]` to prevent invalid values
- [ ] Validate category existence before creating/updating products
- [ ] Validate product existence before creating order lines
- [ ] Check stock availability before deducting inventory

#### Authorization & Authentication
- [ ] Currently no authentication required (demo API)
- [ ] If adding auth: Use ASP.NET Core Identity or JWT tokens
- [ ] Implement role-based access control (RBAC) for admin operations
- [ ] Protect sensitive endpoints: product deletion, order cancellation, stock adjustments

#### Data Protection
- [ ] No sensitive data in logs: avoid logging customer emails, payment info
- [ ] Use correlation IDs for audit trails: track all operations by request
- [ ] Encrypt sensitive configuration: use `appsettings.json` for non-secrets, secrets manager for sensitive data
- [ ] Validate all external inputs: SKU format, email format, numeric ranges

#### Dependency Management
- [ ] Review NuGet package versions in .csproj files
- [ ] Keep dependencies up-to-date: run `dotnet outdated` regularly
- [ ] Scan for vulnerabilities: use `dotnet list package --vulnerable`
- [ ] Avoid deprecated packages: check for security advisories

### Performance Guidelines

#### Query Optimization
- [ ] Use `.Include()` to prevent N+1 queries: `Include(p => p.Category)`
- [ ] Use `.Select()` to project only needed fields when possible
- [ ] Use `.Where()` to filter at database level, not in memory
- [ ] Avoid `.ToList()` before `.Where()`: execute filters in database
- [ ] Use `.FirstOrDefaultAsync()` instead of `.ToListAsync().FirstOrDefault()`

#### Caching Strategies
- [ ] Cache category list: categories change infrequently
- [ ] Cache product search results: consider Redis for high-traffic scenarios
- [ ] Invalidate cache on updates: clear when products/categories change
- [ ] Use EF Core change tracking: avoid redundant queries in same request

#### Response Time Targets
- [ ] GET endpoints: < 100ms for single entity, < 500ms for collections
- [ ] POST endpoints: < 200ms for creation with stock deduction
- [ ] Search endpoints: < 1000ms for full-text search across 20,000 products
- [ ] Monitor with RequestLoggingMiddleware: track elapsed time per request

#### Scalability Considerations
- [ ] InMemory database is NOT suitable for production: plan migration to SQL Server/PostgreSQL
- [ ] Implement pagination for large result sets: `?page=1&pageSize=50`
- [ ] Use async/await throughout: prevent thread pool starvation
- [ ] Consider message queues for long-running operations: order processing, stock reconciliation
- [ ] Plan for horizontal scaling: stateless services, distributed caching

---

## Verification Checklist

### Before Committing Code

#### Compilation & Build
- [ ] `dotnet build` succeeds with no errors
- [ ] `dotnet build` succeeds with no warnings (or documented suppressions)
- [ ] No CS8600, CS8602, CS8603 (nullable reference type warnings)
- [ ] Solution builds in Release configuration: `dotnet build -c Release`

#### Code Quality
- [ ] No debug statements: `Console.WriteLine()`, `Debug.WriteLine()`, breakpoints
- [ ] No commented-out code blocks
- [ ] No TODO comments without issue reference: `// TODO: #42 - Implement pagination`
- [ ] No hardcoded values: use configuration, constants, or enums
- [ ] Proper naming: PascalCase for classes/methods, camelCase for parameters/locals

#### Formatting & Style
- [ ] Code follows C# conventions: use `dotnet format` if available
- [ ] Consistent indentation: 4 spaces (not tabs)
- [ ] Line length: prefer < 120 characters for readability
- [ ] Proper spacing: blank lines between methods, logical sections

#### Logging & Tracing
- [ ] All service methods log important operations
- [ ] No sensitive data in logs: avoid customer PII, payment info
- [ ] Use appropriate log levels: `Information`, `Warning`, `Error`
- [ ] Include context in log messages: entity IDs, operation names

### Before Submitting PR

#### Test Execution
- [ ] `dotnet test` passes all tests
- [ ] No flaky tests: tests pass consistently
- [ ] Test coverage >= 80% for modified code
- [ ] New features include corresponding unit tests
- [ ] Edge cases tested: null inputs, empty collections, boundary values

#### Manual Testing
- [ ] Test happy path: successful create, read, update, delete
- [ ] Test error cases: duplicate SKU, insufficient stock, invalid category
- [ ] Test API with Swagger UI: verify request/response contracts
- [ ] Test with curl or Postman: verify HTTP status codes
- [ ] Test with invalid input: empty strings, negative numbers, missing fields

#### Code Review Checklist
- [ ] Code follows architecture.md patterns: layered architecture, DI
- [ ] Code follows code.md conventions: naming, async patterns, error handling
- [ ] No breaking changes to public APIs
- [ ] No performance regressions: check query efficiency
- [ ] Error handling is complete: all exceptions caught and logged
- [ ] No security vulnerabilities: input validation, authorization checks

#### Documentation Requirements
- [ ] README.md updated if API changes
- [ ] Swagger/XML comments added to public methods: `/// <summary>`
- [ ] /docs/architecture.md updated for new features
- [ ] /docs/dataflow.md updated for new data flows
- [ ] /docs/glossary.md updated for new domain terms
- [ ] CHANGELOG.md updated with feature/fix description

### Security Verification

#### Input Validation
- [ ] All DTOs have `[Required]` on mandatory fields
- [ ] All string fields have `[MaxLength(n)]`
- [ ] All numeric fields have `[Range(min, max)]`
- [ ] Business logic validates related entities exist
- [ ] Stock availability checked before deduction

#### Authorization & Authentication
- [ ] Sensitive operations protected (if auth implemented)
- [ ] No hardcoded credentials in code
- [ ] Configuration uses appsettings.json or secrets manager
- [ ] Correlation IDs logged for audit trail

#### Data Protection
- [ ] No sensitive data in logs
- [ ] No sensitive data in error responses
- [ ] Encryption used for sensitive fields (if applicable)
- [ ] HTTPS enforced in production

#### Dependency Security
- [ ] No known vulnerabilities in NuGet packages
- [ ] Dependencies are from official sources
- [ ] License compliance checked for new dependencies

### Performance Validation

#### Query Efficiency
- [ ] No N+1 queries: use `.Include()` for related entities
- [ ] No unnecessary `.ToList()` calls before filtering
- [ ] Queries execute at database level, not in memory
- [ ] Indexes created for frequently queried fields (SKU, OrderNumber)

#### Load Testing
- [ ] Response times acceptable under normal load
- [ ] No memory leaks: monitor memory usage over time
- [ ] Connection pooling configured for database
- [ ] Async/await used throughout to prevent thread starvation

#### Caching & Optimization
- [ ] Frequently accessed data cached (categories, popular products)
- [ ] Cache invalidation implemented on updates
- [ ] Pagination implemented for large result sets
- [ ] Bundle size acceptable (if frontend involved)

### Code Review Checklist

#### Conventions & Maintainability
- [ ] Code is readable: clear variable names, logical structure
- [ ] Code is DRY: no duplicate logic, reusable methods
- [ ] Code is SOLID: single responsibility, open/closed, Liskov, interface segregation, dependency inversion
- [ ] Comments explain "why", not "what": code should be self-documenting
- [ ] No magic numbers: use named constants or enums

#### Error Handling & Logging
- [ ] All exceptions caught and logged appropriately
- [ ] User-friendly error messages in API responses
- [ ] Stack traces not exposed to clients
- [ ] Correlation IDs included in error logs
- [ ] Graceful degradation for non-critical failures

#### Testing & Coverage
- [ ] Unit tests cover happy path and error cases
- [ ] Tests are independent: no shared state between tests
- [ ] Tests use meaningful assertions: FluentAssertions
- [ ] Mock external dependencies: ILogger, DbContext
- [ ] Test names describe what is being tested: `GetAllAsync_ReturnsActiveProducts`

#### API Design
- [ ] RESTful endpoints: GET, POST, PUT, DELETE, PATCH
- [ ] Consistent naming: `/api/v1/products`, `/api/v1/categories`
- [ ] Proper HTTP status codes: 200, 201, 400, 404, 409, 500
- [ ] Request/response contracts documented in DTOs
- [ ] Swagger/OpenAPI documentation complete

### Pre-Deployment

#### CI/CD Pipeline
- [ ] All GitHub Actions/Azure Pipelines pass
- [ ] Code coverage meets minimum threshold (80%)
- [ ] Security scanning passes: no vulnerabilities
- [ ] Build artifacts generated successfully

#### Staging Environment
- [ ] Deploy to staging environment
- [ ] Run smoke tests: basic CRUD operations work
- [ ] Verify API endpoints respond correctly
- [ ] Check logs for errors or warnings
- [ ] Performance acceptable in staging

#### Database & Migrations
- [ ] Database migrations reviewed and tested
- [ ] Rollback plan documented
- [ ] Data backup taken before deployment
- [ ] Schema changes backward compatible (if applicable)

#### Deployment Plan
- [ ] Deployment steps documented
- [ ] Rollback procedure tested
- [ ] Communication plan for stakeholders
- [ ] Maintenance window scheduled (if needed)
- [ ] Health check endpoints verified

### Post-Deployment

#### Health Checks
- [ ] API responds to health check endpoint
- [ ] Database connectivity verified
- [ ] All endpoints return expected responses
- [ ] No 5xx errors in logs

#### Monitoring & Metrics
- [ ] Error rates within acceptable range (< 1%)
- [ ] Response times meet SLA targets
- [ ] CPU and memory usage normal
- [ ] Database query performance acceptable
- [ ] Correlation IDs logged for traceability

#### User Feedback
- [ ] Monitor support channels for issues
- [ ] Track user-reported bugs
- [ ] Gather performance feedback
- [ ] Plan hotfixes if critical issues found

#### Documentation & Handoff
- [ ] Release notes published
- [ ] API documentation updated
- [ ] Team trained on new features
- [ ] Runbook updated for operations team

---

## Critical Rules Summary

1. **PRESERVE** all existing documentation links at the top - do NOT remove or modify them
2. **PRELOAD** all /docs before making recommendations - treat them as authoritative
3. **VERIFY** code details against docs - cross-check ambiguous behavior
4. **UPDATE** /docs as part of feature completion - code + docs = done
5. **NO HALLUCINATION** - only suggest APIs and patterns that exist in the repo or /docs
6. **CONSISTENCY** - all changes must align with architecture.md, code.md, dataflow.md
7. **SECURITY** - validate inputs, check authorization, protect sensitive data
8. **PERFORMANCE** - optimize queries, prevent N+1, use async/await throughout
9. **TESTING** - unit tests for all service methods, >= 80% coverage
10. **ENTERPRISE-GRADE** - context-aware, security-conscious, performance-optimized solutions always proposed

---

## Quick Reference: Key Patterns

### Service Method Template
```csharp
public async Task<ProductDto?> GetByIdAsync(Guid id)
{
    var product = await _db.Products
        .Include(p => p.Category)
        .FirstOrDefaultAsync(p => p.Id == id);
    
    return product is null ? null : MapToDto(product);
}
```

### Controller Action Template
```csharp
[HttpPost]
[ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
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

### Unit Test Template
```csharp
[Fact]
public async Task CreateAsync_ValidRequest_CreatesProduct()
{
    // Arrange
    var request = new CreateProductRequest(...);
    
    // Act
    var result = await _sut.CreateAsync(request);
    
    // Assert
    result.Should().NotBeNull();
    result.Name.Should().Be(request.Name);
}
```

### DTO Record Template
```csharp
public record ProductDto(
    Guid Id,
    [Required][MaxLength(200)] string Name,
    [Required][MaxLength(100)] string SKU,
    [Range(0, double.MaxValue)] decimal Price
);
```

---

## Project Structure Reference

```
demo-inventory-csharp/
├── src/
│   └── InventoryApi/
│       ├── Controllers/          # API endpoints (Products, Categories, Orders)
│       ├── Services/             # Business logic (ProductService, OrderService, CategoryService)
│       ├── Models/               # Domain entities (Product, Category, Order, OrderLine)
│       ├── Data/                 # EF Core DbContext + seed data
│       ├── DTOs/                 # Request/response records
│       ├── Middleware/           # RequestLoggingMiddleware
│       ├── Program.cs            # Startup configuration, DI setup
│       └── InventoryApi.csproj   # Project file (net9.0, EF Core, Swagger)
├── tests/
│   └── InventoryApi.Tests/
│       ├── ProductServiceTests.cs # xUnit tests with FluentAssertions
│       └── InventoryApi.Tests.csproj
├── data/                         # Demo data files
├── docs/                         # Architecture documentation
├── README.md
└── demo-inventory-csharp.sln
```

---

## Success Criteria

✓ Cursor must not operate without context docs loaded
✓ All decisions rely on the context packs (architecture, code, dataflow, decisions, glossary, risk)
✓ Features are only "done" if both code and /docs are updated
✓ No unsupported assumptions; verify context across packs
✓ Enterprise-grade, security-conscious, performance-optimized solutions always proposed
✓ All verification rules specific to .NET 9, EF Core, ASP.NET Core, xUnit, FluentAssertions
✓ Checklists are actionable and project-specific
```