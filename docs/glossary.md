# Domain Glossary: demo-inventory-csharp

## Overview

This glossary documents domain-specific terms, technical terminology, abbreviations, entity names, API conventions, and configuration variables used throughout the demo-inventory-csharp repository. Each term includes its definition and references to where it appears in the codebase.

---

## Abbreviations and Acronyms

| Term | Full Form | Definition | Reference |
|------|-----------|-----------|-----------|
| CORS | Cross-Origin Resource Sharing | HTTP mechanism allowing restricted resources on a web page to be requested from another domain | `Program.cs` (line 28) |
| CRUD | Create, Read, Update, Delete | Standard operations for persistent storage | Controllers throughout codebase |
| DTO | Data Transfer Object | Object used to transfer data between layers, decoupling internal models from API contracts | `ProductDto`, `CategoryDto`, `OrderDto` in `DTOs/ProductDto.cs` |
| EF Core | Entity Framework Core | Object-relational mapping (ORM) framework for .NET | `InventoryDbContext.cs` |
| GUID | Globally Unique Identifier | 128-bit identifier guaranteed to be unique across systems and time | `Product.Id`, `Order.Id`, `Category.Id` in model classes |
| HTTP | HyperText Transfer Protocol | Protocol for transferring data over the web | API Controllers |
| JSON | JavaScript Object Notation | Data format for API requests and responses | All API endpoints |
| ORM | Object-Relational Mapping | Programming technique for converting between incompatible type systems | Entity Framework Core usage |
| REST | Representational State Transfer | Architectural style for designing networked applications | API design pattern |
| SKU | Stock Keeping Unit | Unique identifier for a product used in inventory management and tracking | `Product.SKU` in `src/InventoryApi/Models/Product.cs` (line 13) |
| SOLID | Single Responsibility, Open/Closed, Liskov Substitution, Interface Segregation, Dependency Inversion | Software design principles | Architecture pattern |

---

## API Terminology and Conventions

### API Versioning

**API Version (v1)**
- **Definition:** The current API version identifier used in all endpoint routes
- **Convention:** Routes follow pattern `/api/v1/{resource}`
- **Reference:** `ProductsController` in `src/InventoryApi/Controllers/ProductsController.cs` (line 8)
- **Example:** `GET /api/v1/products`, `POST /api/v1/categories`

### HTTP Methods and Operations

**GET**
- **Definition:** HTTP method for retrieving resources without modifying state
- **Usage:** Fetching products, categories, orders, and search operations
- **Reference:** `GetAll()`, `GetById()`, `Search()` methods in all controllers

**POST**
- **Definition:** HTTP method for creating new resources
- **Usage:** Creating new products, categories, and orders
- **Reference:** `Create()` methods in all controllers

**PUT**
- **Definition:** HTTP method for updating entire resources
- **Usage:** Updating product details, category information, order status
- **Reference:** `Update()` methods in all controllers

**PATCH**
- **Definition:** HTTP method for partial updates to resources
- **Usage:** Updating specific fields like order status
- **Reference:** `UpdateStatus()` in `OrdersController.cs` (line 60)

**DELETE**
- **Definition:** HTTP method for removing resources
- **Usage:** Deleting products and categories
- **Reference:** `Delete()` methods in all controllers

### Response Status Codes

**200 OK**
- **Definition:** Request succeeded and resource is returned
- **Usage:** Successful GET, PUT operations
- **Reference:** Standard HTTP response in all GET and PUT endpoints

**201 Created**
- **Definition:** Request succeeded and new resource was created
- **Usage:** Successful POST operations
- **Reference:** POST endpoints in all controllers, returned via `CreatedAtAction()` pattern

**204 No Content**
- **Definition:** Request succeeded but no content to return
- **Usage:** Successful DELETE operations
- **Reference:** DELETE endpoints in all controllers

**400 Bad Request**
- **Definition:** Request contains invalid data or parameters
- **Usage:** Invalid product data, malformed requests, validation failures
- **Reference:** Validation error responses in controllers

**404 Not Found**
- **Definition:** Requested resource does not exist
- **Usage:** Product, category, or order not found by ID
- **Reference:** Error handling in service methods

**409 Conflict**
- **Definition:** Request conflicts with current state of the server (e.g., duplicate SKU)
- **Usage:** Attempting to create product with duplicate SKU, category constraint violations
- **Reference:** `ProductsController.Create()` in `src/InventoryApi/Controllers/ProductsController.cs` (line 108)

**500 Internal Server Error**
- **Definition:** Server encountered an unexpected condition
- **Usage:** Unhandled exceptions during request processing
- **Reference:** Global exception handling in middleware

### Response Patterns

**CreatedAtAction**
- **Definition:** Response pattern that returns 201 Created with Location header pointing to newly created resource
- **Syntax:** `CreatedAtAction(nameof(GetById), new { id = product.Id }, product)`
- **Purpose:** Provides client with URL to retrieve the newly created resource
- **Reference:** `ProductsController.Create()` in `src/InventoryApi/Controllers/ProductsController.cs` (line 109)

**ProducesResponseType**
- **Definition:** Attribute documenting possible HTTP response types and status codes for an action
- **Syntax:** `[ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]`
- **Purpose:** Enables Swagger/OpenAPI documentation of all possible responses
- **Reference:** All controller action methods in `ProductsController.cs`, `OrdersController.cs`, `CategoriesController.cs`

### Request Binding

**FromQuery**
- **Definition:** Attribute binding request data from URL query string parameters
- **Syntax:** `[FromQuery] bool activeOnly = true`
- **Usage:** Filtering and pagination parameters
- **Reference:** `ProductsController.GetAll()` in `src/InventoryApi/Controllers/ProductsController.cs` (line 24)

**FromBody**
- **Definition:** Attribute binding request data from HTTP request body (typically JSON)
- **Syntax:** `[FromBody] CreateProductRequest request`
- **Purpose:** Deserializes JSON payload into strongly-typed objects
- **Reference:** All POST/PUT/PATCH endpoints in controllers

### Model Validation

**ModelState**
- **Definition:** Dictionary containing validation state of model properties
- **Usage:** Checking if request data passes validation rules before processing
- **Syntax:** `if (!ModelState.IsValid) return BadRequest(ModelState);`
- **Reference:** `ProductsController.Create()` in `src/InventoryApi/Controllers/ProductsController.cs` (line 103)

---

## Configuration and Environment Variables

### Application Settings

**AllowedHosts**
- **Definition:** Comma-separated list of hosts allowed to access the application
- **Type:** String
- **Default Value:** `*` (all hosts allowed)
- **Reference:** `appsettings.json` (line 2)
- **Usage:** CORS and host validation

**Logging:LogLevel:Default**
- **Definition:** Default logging level for all loggers
- **Type:** String
- **Value:** `Information`
- **Reference:** `appsettings.json` (line 4)
- **Levels:** Trace, Debug, Information, Warning, Error, Critical, None

**Logging:LogLevel:Microsoft**
- **Definition:** Logging level specifically for Microsoft namespace loggers
- **Type:** String
- **Value:** `Warning`
- **Reference:** `appsettings.json` (line 5)
- **Purpose:** Reduces verbosity of framework logs

---

## Domain-Specific Terms

### Business Entities

**Category**
- **Definition:** A classification or grouping for products (e.g., Electronics, Clothing, Food)
- **Business Purpose:** Organizes products for easier browsing and management
- **Entity Class:** `Category` in `src/InventoryApi/Models/Category.cs`
- **Properties:** `Id`, `Name`, `Description`, `Slug`, `IsActive`, `CreatedAt`, `UpdatedAt`, `ProductCount`
- **Relationships:** One-to-Many with Products
- **Reference:** `CategoryService` in `src/InventoryApi/Services/CategoryService.cs`

**Order**
- **Definition:** A customer purchase request containing one or more order line items
- **Business Purpose:** Tracks customer purchases and manages inventory deductions
- **Entity Class:** `Order` in `src/InventoryApi/Models/Order.cs`
- **Properties:** `Id`, `OrderNumber`, `CustomerName`, `CustomerEmail`, `OrderDate`, `TotalAmount`, `Status`, `ShippingAddress`, `Notes`, `Lines`, `CreatedAt`, `UpdatedAt`
- **Relationships:** One-to-Many with OrderLines
- **Reference:** `OrderService` in `src/InventoryApi/Services/OrderService.cs`

**OrderLine**
- **Definition:** A line item within an order representing a specific product, quantity, and price at time of order
- **Business Purpose:** Tracks individual products within an order and their quantities, preserving historical pricing
- **Entity Class:** `OrderLine` in `src/InventoryApi/Models/Order.cs` (line 48)
- **Properties:** `Id`, `OrderId`, `ProductId`, `ProductName`, `ProductSKU`, `Quantity`, `UnitPrice`, `LineTotal`
- **Relationships:** Many-to-One with Order, Many-to-One with Product
- **Reference:** `Order.Lines` collection in `src/InventoryApi/Models/Order.cs` (line 45)

**Product**
- **Definition:** A physical or digital item available for sale in the inventory system
- **Business Purpose:** Core entity representing items that can be ordered and tracked
- **Entity Class:** `Product` in `src/InventoryApi/Models/Product.cs`
- **Properties:** `Id`, `Name`, `SKU`, `Description`, `Price`, `Cost`, `StockQuantity`, `ReorderPoint`, `IsActive`, `Brand`, `WeightKg`, `CategoryId`, `Category`, `IsLowStock`, `CreatedAt`, `UpdatedAt`
- **Relationships:** Many-to-One with Category, One-to-Many with OrderLines
- **Reference:** `ProductService` in `src/InventoryApi/Services/ProductService.cs`

### Inventory Management Concepts

**Active Status**
- **Definition:** Boolean flag indicating whether an entity (Product or Category) is currently available for use
- **Business Purpose:** Enables logical deactivation without removing data
- **Property Name:** `IsActive`
- **Default Value:** `true`
- **Reference:** `Product.IsActive` and `Category.IsActive` in model classes

**Brand**
- **Definition:** Manufacturer or brand name associated with a product
- **Data Type:** `string` (nullable, max 100 characters)
- **Business Purpose:** Enables filtering and searching by brand
- **Reference:** `Product.Brand` in `src/InventoryApi/Models/Product.cs` (line 37)

**Cost**
- **Definition:** The cost price of a product (what the business paid for it)
- **Data Type:** `decimal(18,2)`
- **Business Purpose:** Enables profit margin calculations and cost analysis
- **Reference:** `Product.Cost` in `src/InventoryApi/Models/Product.cs` (line 23)

**IsLowStock**
- **Definition:** Computed property indicating whether product stock is at or below reorder point
- **Data Type:** `bool` (computed expression)
- **Syntax:** `public bool IsLowStock => StockQuantity <= ReorderPoint;`
- **Business Purpose:** Identifies products needing reordering
- **Reference:** `Product.IsLowStock` in `src/InventoryApi/Models/Product.cs` (line 42)

**Reorder Point**
- **Definition:** Minimum stock level threshold that triggers a reorder alert
- **Data Type:** `int`
- **Default Value:** `10`
- **Business Purpose:** Prevents stock-outs by alerting when inventory falls below threshold
- **Reference:** `Product.ReorderPoint` in `src/InventoryApi/Models/Product.cs` (line 31)

**Slug**
- **Definition:** URL-friendly identifier derived from category name for use in URLs
- **Data Type:** `string` (nullable, max 50 characters)
- **Format:** Lowercase with hyphens replacing spaces (e.g., "food-beverage")
- **Business Purpose:** Enables human-readable URLs and SEO-friendly links
- **Reference:** `Category.Slug` in `src/InventoryApi/Models/Category.cs` (line 12)

**Stock Quantity**
- **Definition:** The current number of units of a product available in inventory
- **Business Purpose:** Tracks available inventory and prevents overselling
- **Property Name:** `StockQuantity`
- **Data Type:** `int`
- **Reference:** `Product.StockQuantity` in `src/InventoryApi/Models/Product.cs` (line 27)

**Stock Management**
- **Definition:** Process of tracking and updating product quantities based on orders and restocking
- **Business Purpose:** Ensures accurate inventory levels and prevents stock-outs
- **Implementation:** `Product.UpdateStock()` method deducts/adds stock; `OrderService.CreateAsync()` deducts stock on order creation
- **Reference:** `Product.UpdateStock()` in `src/InventoryApi/Models/Product.cs` (line 45)

**Soft Delete**
- **Definition:** Marking a record as inactive rather than permanently removing it from the database
- **Business Purpose:** Preserves historical data and audit trails while hiding inactive items
- **Implementation:** Sets `IsActive = false` instead of removing the record
- **Reference:** Product and Category models support soft delete via `IsActive` flag

### Data and Timestamps

**CreatedAt**
- **Definition:** Timestamp indicating when an entity was first created
- **Data Type:** `DateTime`
- **Set By:** Automatically set to current UTC time when entity is created
- **Reference:** All model classes in `src/InventoryApi/Models/`

**UpdatedAt**
- **Definition:** Timestamp indicating when an entity was last modified
- **Data Type:** `DateTime`
- **Set By:** Automatically updated to current UTC time on any modification
- **Reference:** All model classes in `src/InventoryApi/Models/`

---

## Entity Names and Business Meaning

### Model Classes

**Category Model**
- **Namespace:** `InventoryApi.Models`
- **File:** `src/InventoryApi/Models/Category.cs`
- **Purpose:** Represents a product category in the inventory system
- **Key Properties:**
  - `Id` (Guid): Unique identifier
  - `Name` (string): Category name
  - `Description` (string): Detailed description
  - `Slug` (string): URL-friendly identifier
  - `IsActive` (bool): Availability flag
  - `Products` (ICollection<Product>): Related products
  - `ProductCount` (int): Computed count of products in category
  - `CreatedAt` (DateTime): Creation timestamp
  - `UpdatedAt` (DateTime): Last modification timestamp

**Order Model**
- **Namespace:** `InventoryApi.Models`
- **File:** `src/InventoryApi/Models/Order.cs`
- **Purpose:** Represents a customer purchase order
- **Key Properties:**
  - `Id` (Guid): Unique identifier
  - `OrderNumber` (string): Human-readable order reference
  - `CustomerName` (string): Customer name
  - `CustomerEmail` (string): Customer email address
  - `Status` (OrderStatus): Current order status enum
  - `TotalAmount` (decimal): Order total value
  - `ShippingAddress` (string): Delivery address
  - `Notes` (string): Order notes
  - `Lines` (ICollection<OrderLine>): Line items in order
  - `CreatedAt` (DateTime): Creation timestamp
  - `UpdatedAt` (DateTime): Last modification timestamp

**OrderLine Model**
- **Namespace:** `InventoryApi.Models`
- **File:** `src/InventoryApi/Models/Order.cs` (line 48)
- **Purpose:** Represents a single line item within an order
- **Key Properties:**
  - `Id` (Guid): Unique identifier
  - `OrderId` (Guid): Foreign key to Order
  - `ProductId` (Guid): Foreign key to Product
  - `ProductName` (string): Product name at time of order
  - `ProductSKU` (string): Product SKU at time of order
  - `Quantity` (int): Number of units ordered
  - `UnitPrice` (decimal): Price per unit at time of order
  - `LineTotal` (decimal): Computed Quantity × UnitPrice
  - `Product` (Product): Navigation property to product

**OrderStatus Enum**
- **Namespace:** `InventoryApi.Models`
- **File:** `src/InventoryApi/Models/Order.cs` (line 6)
- **Purpose:** Defines valid states for an order throughout its lifecycle
- **Values:**
  - `Pending`: Order created but not yet confirmed
  - `Confirmed`: Order confirmed and ready for processing
  - `Processing`: Order is being prepared for shipment
  - `Shipped`: Order has been shipped
  - `Delivered`: Order has been delivered to customer
  - `Cancelled`: Order has been cancelled
  - `Refunded`: Order has been refunded
- **Reference:** `Order.Status` property in `src/InventoryApi/Models/Order.cs` (line 24)

**Product Model**
- **Namespace:** `InventoryApi.Models`
- **File:** `src/InventoryApi/Models/Product.cs`
- **Purpose:** Represents a product in the inventory system
- **Key Properties:**
  - `Id` (Guid): Unique identifier
  - `Name` (string): Product name
  - `SKU` (string): Stock Keeping Unit - unique product identifier
  - `Description` (string): Product description
  - `Price` (decimal): Selling price
  - `Cost` (decimal): Cost price
  - `StockQuantity` (int): Available units
  - `ReorderPoint` (int): Minimum stock threshold
  - `IsActive` (bool): Availability flag
  - `Brand` (string): Brand/manufacturer name
  - `WeightKg` (decimal): Product weight in kilograms
  - `CategoryId` (Guid): Foreign key to Category
  - `Category` (Category): Navigation property to category
  - `IsLowStock` (bool): Computed property indicating low stock status
  - `CreatedAt` (DateTime): Creation timestamp
  - `UpdatedAt` (DateTime): Last modification timestamp

### Service Classes

**CategoryService**
- **Namespace:** `InventoryApi.Services`
- **File:** `src/InventoryApi/Services/CategoryService.cs`
- **Purpose:** Business logic for category management
- **Key Methods:**
  - `GetAllAsync()`: Retrieve all categories with product counts
  - `GetByIdAsync(Guid id)`: Retrieve specific category
  - `CreateAsync(CreateCategoryRequest request)`: Create new category with auto-generated slug
  - `UpdateAsync(Guid id, CreateCategoryRequest request)`: Update category
  - `DeleteAsync(Guid id)`: Delete category (prevents deletion if products exist)

**OrderService**
- **Namespace:** `InventoryApi.Services`
- **File:** `src/InventoryApi/Services/OrderService.cs`
- **Purpose:** Business logic for order management and stock control
- **Key Methods:**
  - `GetAllAsync()`: Retrieve all orders with line items
  - `GetByIdAsync(Guid id)`: Retrieve specific order
  - `CreateAsync(CreateOrderRequest request)`: Create order, validate stock, deduct inventory
  - `UpdateStatusAsync(Guid id, OrderStatus status)`: Update order status
  - `MapToDto(Order o)`: Convert Order entity to OrderDto

**ProductService**
- **Namespace:** `InventoryApi.Services`
- **File:** `src/InventoryApi/Services/ProductService.cs`
- **Purpose:** Business logic for product management
- **Key Methods:**
  - `GetAllAsync(bool activeOnly = true)`: Retrieve products with optional filtering
  - `GetByIdAsync(Guid id)`: Retrieve specific product
  - `GetBySkuAsync(string sku)`: Retrieve product by SKU identifier
  - `GetByCategoryAsync(Guid categoryId)`: Retrieve products in a category
  - `GetLowStockAsync()`: Retrieve products at or below reorder point
  - `CreateAsync(CreateProductRequest request)`: Create new product
  - `UpdateAsync(Guid id, UpdateProductRequest request)`: Update product
  - `DeleteAsync(Guid id)`: Delete product
  - `AdjustStockAsync(Guid id, StockAdjustmentRequest request)`: Adjust stock with reason
  - `SearchAsync(string query)`: Full-text search products by name, SKU, description, or brand
  - `MapToDto(Product p)`: Convert Product entity to ProductDto

### Controller Classes

**CategoriesController**
- **Namespace:** `InventoryApi.Controllers`
- **File:** `src/InventoryApi/Controllers/CategoriesController.cs`
- **Route:** `/api/v1/categories`
- **Purpose:** HTTP endpoints for category management
- **Endpoints:**
  - `GET /api/v1/categories`: List all categories
  - `GET /api/v1/categories/{id}`: Get category by ID
  - `POST /api/v1/categories`: Create category
  - `PUT /api/v1/categories/{id}`: Update category
  - `DELETE /api/v1/categories/{id}`: Delete category

**OrdersController**
- **Namespace:** `InventoryApi.Controllers`
- **File:** `src/InventoryApi/Controllers/OrdersController.cs`
- **Route:** `/api/v1/orders`
- **Purpose:** HTTP endpoints for order management
- **Endpoints:**
  - `GET /api/v1/orders`: List all orders
  - `GET /api/v1/orders/{id}`: Get order by ID
  - `POST /api/v1/orders`: Create order
  - `PATCH /api/v1/orders/{id}/status`: Update order status

**ProductsController**
- **Namespace:** `InventoryApi.Controllers`
- **File:** `src/InventoryApi/Controllers/ProductsController.cs`
- **Route:** `/api/v1/products`
- **Purpose:** HTTP endpoints for product management
- **Endpoints:**
  - `GET /api/v1/products`: List products
  - `GET /api/v1/products/{id}`: Get product by ID
  - `GET /api/v1/products/sku/{sku}`: Get product by SKU
  - `GET /api/v1/products/search?q={query}`: Search products
  - `GET /api/v1/products/category/{categoryId}`: Get products by category
  - `GET /api/v1/products/low-stock`: Get products below reorder point
  - `POST /api/v1/products`: Create product
  - `PUT /api/v1/products/{id}`: Update product
  - `DELETE /api/v1/products/{id}`: Delete product
  - `POST /api/v1/products/{id}/stock`: Adjust product stock

---

## Technical Terms Unique to This Project

### Data Access Layer

**InventoryDbContext**
- **Definition:** Entity Framework Core database context managing all database operations
- **File:** `src/InventoryApi/Data/InventoryDbContext.cs`
- **Purpose:** Provides DbSet properties for each entity and configures database relationships
- **Key DbSets:**
  - `DbSet<Product> Products`: Products table
  - `DbSet<Category> Categories`: Categories table
  - `DbSet<Order> Orders`: Orders table
  - `DbSet<OrderLine> OrderLines`: Order line items table
- **Database Type:** In-memory (no external database required)
- **Reference:** Injected into all service classes via dependency injection

**DbContext**
- **Definition:** Object that represents a session with the database
- **Implementation:** `InventoryDbContext` in `src/InventoryApi/Data/InventoryDbContext.cs`
- **Purpose:** Manages entity tracking, change detection, and database operations
- **Reference:** Registered in dependency injection container in `Program.cs`

**DbSet**
- **Definition:** Generic collection representing a table in the database
- **Usage:** `DbSet<Product>`, `DbSet<Category>`, `DbSet<Order>`, `DbSet<OrderLine>`
- **Purpose:** Provides LINQ query interface to database tables
- **Reference:** Properties in `InventoryDbContext` class

**In-Memory Database**
- **Definition:** Database provider that stores data in application memory without persistence
- **Provider:** `Microsoft.EntityFrameworkCore.InMemory`
- **Purpose:** Enables zero-configuration development and testing
- **Limitation:** Data is lost when application stops
- **Reference:** Configured in `Program.cs` (line 18)

**Navigation Property**
- **Definition:** Entity Framework property representing a relationship to another entity
- **Examples:**
  - `Product.Category`: Many-to-One relationship
  - `Category.Products`: One-to-Many relationship
  - `Order.Lines`: One-to-Many relationship
  - `OrderLine.Product`: Many-to-One relationship
- **Purpose:** Enables object-oriented access to related entities
- **Reference:** Model classes in `src/InventoryApi/Models/`

**Foreign Key**
- **Definition:** Property that references the primary key of another entity
- **Examples:**
  - `Product.CategoryId`: References `Category.Id`
  - `OrderLine.OrderId`: References `Order.Id`
  - `OrderLine.ProductId`: References `Product.Id`
- **Purpose:** Maintains referential integrity between entities
- **Reference:** Model classes in `src/InventoryApi/Models/`

**Include and ThenInclude**
- **Definition:** EF Core methods for eager loading related entities in a single query
- **Syntax:** `.Include(p => p.Category).ThenInclude(c => c.Products)`
- **Purpose:** Prevents N+1 query problems by loading related data upfront
- **Reference:** `ProductService.GetAllAsync()` in `src/InventoryApi/Services/ProductService.cs` (line 19)

**DeleteBehavior**
- **Definition:** Configuration specifying how foreign key constraints are handled when parent entity is deleted
- **Values:**
  - `Cascade`: Automatically delete dependent entities
  - `Restrict`: Prevent deletion if dependent entities exist
  - `SetNull`: Set foreign key to null (if nullable)
  - `NoAction`: No automatic action
- **Usage:** `OnDelete(DeleteBehavior.Restrict)` for Product-Category relationship
- **Reference:** `InventoryDbContext.OnModelCreating()` in `src/InventoryApi/Data/InventoryDbContext.cs` (line 20)

### Middleware and Infrastructure

**RequestLoggingMiddleware**
- **Definition:** Custom middleware that logs all HTTP requests with correlation IDs and response times
- **File:** `src/InventoryApi/Middleware/RequestLoggingMiddleware.cs`
- **Purpose:** Provides observability and request tracing
- **Functionality:**
  - Generates or retrieves correlation ID from request headers
  - Logs request method, path, and correlation ID
  - Measures request duration using Stopwatch
  - Logs response status code and elapsed time
  - Passes correlation ID through request pipeline
- **Reference:** Registered in `Program.cs` (line 50)

**CorrelationId**
- **Definition:** Unique identifier assigned to each HTTP request for tracing across logs
- **Format:** GUID (Globally Unique Identifier) or 8-character hex string
- **Storage:** Added to HttpContext response headers as `X-Correlation-ID`
- **Purpose:** Enables request tracking through distributed systems
- **Reference:** `RequestLoggingMiddleware.cs` (line 16)

### Data Transfer Objects (DTOs)

**CategoryDto**
- **Definition:** Data transfer object for category data in API requests/responses
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 48)
- **Type:** C# record type
- **Properties:** `Id`, `Name`, `Description`, `Slug`, `IsActive`, `ProductCount`, `CreatedAt`, `UpdatedAt`
- **Purpose:** Decouples API contract from internal entity model
- **Usage:** Request/response serialization in category endpoints

**CreateCategoryRequest**
- **Definition:** Request DTO for creating a new category
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 56)
- **Type:** C# record type
- **Properties:** `Name`, `Description`, `Slug`
- **Validation:** Name is required and max 100 chars; Slug is optional and max 50 chars

**OrderDto**
- **Definition:** Data transfer object for order data in API requests/responses
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 75)
- **Type:** C# record type
- **Properties:** `Id`, `OrderNumber`, `CustomerName`, `CustomerEmail`, `Status`, `TotalAmount`, `ShippingAddress`, `Notes`, `Lines`, `CreatedAt`, `UpdatedAt`
- **Purpose:** Decouples API contract from internal entity model
- **Usage:** Request/response serialization in order endpoints

**OrderLineDto**
- **Definition:** Data transfer object for order line item data
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 88)
- **Type:** C# record type
- **Properties:** `Id`, `ProductId`, `ProductName`, `ProductSKU`, `Quantity`, `UnitPrice`, `LineTotal`
- **Purpose:** Represents line items in order DTOs
- **Usage:** Nested within OrderDto for order details

**ProductDto**
- **Definition:** Data transfer object for product data in API requests/responses
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 11)
- **Type:** C# record type
- **Properties:** `Id`, `Name`, `SKU`, `Description`, `Price`, `Cost`, `StockQuantity`, `ReorderPoint`, `IsActive`, `Brand`, `WeightKg`, `CategoryId`, `CategoryName`, `IsLowStock`, `CreatedAt`, `UpdatedAt`
- **Purpose:** Decouples API contract from internal entity model
- **Usage:** Request/response serialization in product endpoints

**CreateProductRequest**
- **Definition:** Request DTO for creating a new product
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 23)
- **Type:** C# record type
- **Properties:** `Name`, `SKU`, `Description`, `Price`, `Cost`, `StockQuantity`, `ReorderPoint`, `Brand`, `WeightKg`, `CategoryId`
- **Validation:** Name and SKU required; Price and Cost must be >= 0; StockQuantity and ReorderPoint must be >= 0

**UpdateProductRequest**
- **Definition:** Request DTO for updating an existing product
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 35)
- **Type:** C# record type
- **Properties:** `Name`, `Description`, `Price`, `Cost`, `ReorderPoint`, `IsActive`, `Brand`, `WeightKg`, `CategoryId`
- **Validation:** Name required; Price and Cost must be >= 0; ReorderPoint must be >= 0

**StockAdjustmentRequest**
- **Definition:** Request DTO for adjusting product stock quantity
- **File:** `src/InventoryApi/DTOs/ProductDto.cs` (line 44)
- **Type:** C# record type
- **Properties:** `Quantity` (positive or negative integer), `Reason` (optional string)
- **Purpose:** Enables stock adjustments with audit trail
- **Usage:** `POST /api/v1/products/{id}/stock` endpoint

### Data Annotations and Validation

**MaxLength**
- **Definition:** Data annotation attribute specifying maximum string length
- **Syntax:** `[MaxLength(200)]`
- **Purpose:** Enforces string length constraints at model and database level
- **Reference:** `Product.Name` in `src/InventoryApi/Models/Product.cs` (line 11)

**Range**
- **Definition:** Data annotation attribute specifying valid numeric range
- **Syntax:** `[Range(0, double.MaxValue)]`
- **Purpose:** Enforces numeric value constraints
- **Reference:** `Product.Price` in `src/InventoryApi/Models/Product.cs` (line 19)

**Required**
- **Definition:** Data annotation attribute marking a property as mandatory
- **Syntax:** `[Required]`
- **Purpose:** Ensures property has a value before persistence
- **Reference:** Used throughout DTOs and models

### Mapping Patterns

**MapToDto**
- **Definition:** Pattern method converting entity models to data transfer objects
- **Syntax:** `private static ProductDto MapToDto(Product p) => new(...)`
- **Purpose:** Decouples API responses from internal entity structure
- **Reference:** `ProductService.MapToDto()` in `src/InventoryApi/Services/ProductService.cs` (line 175)

### C