# demo-inventory-csharp: Comprehensive Dataflow Documentation

## Executive Summary

**demo-inventory-csharp** is an ASP.NET Core 9 REST API for inventory management with a clean, layered architecture. This document details all data flows, transformations, and integration patterns within the system. The application uses Entity Framework Core with an in-memory database, implements request logging middleware, and provides comprehensive API documentation via Swagger.

**Key Characteristics:**
- **Architecture Pattern:** Layered (Controllers → Services → Data Access)
- **Database:** Entity Framework Core with in-memory storage
- **API Style:** RESTful with JSON request/response
- **Logging:** Console-based with correlation IDs
- **CORS:** Enabled for all origins
- **Testing:** Unit tests with xUnit and FluentAssertions

---

## 1. Request/Response Flows for Key API Endpoints

### 1.1 Products API Endpoints

#### 1.1.1 GET /api/v1/products (Get All Products)

**Request Flow:**
1. HTTP GET request arrives at `ProductsController.GetAll()` in `src/InventoryApi/Controllers/ProductsController.cs` (line 24)
2. Query parameter `activeOnly` (default: true) is parsed
3. Controller calls `ProductService.GetAllAsync(activeOnly)` in `src/InventoryApi/Services/ProductService.cs` (line 19)

**Data Processing:**
- `ProductService.GetAllAsync()` (line 19-27):
  - Queries `_db.Products` with `.Include(p => p.Category)` to eagerly load category relationships
  - Filters by `IsActive` if `activeOnly=true`
  - Orders results by `Name`
  - Maps each `Product` entity to `ProductDto` via `MapToDto()` (line 175)

**Response:**
- Returns HTTP 200 with `IEnumerable<ProductDto>` in JSON format
- Each `ProductDto` (defined in `src/InventoryApi/DTOs/ProductDto.cs`, line 5-21) contains:
  - `Id`, `Name`, `SKU`, `Description`, `Price`, `Cost`
  - `StockQuantity`, `ReorderPoint`, `IsActive`, `Brand`, `WeightKg`
  - `CategoryId`, `CategoryName`, `IsLowStock`, `CreatedAt`, `UpdatedAt`

**Example Response:**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Wireless Headphones Pro",
    "sku": "ELEC-001",
    "price": 149.99,
    "stockQuantity": 85,
    "isLowStock": false,
    "categoryName": "Electronics"
  }
]
```

#### 1.1.2 GET /api/v1/products/{id} (Get Product by ID)

**Request Flow:**
1. HTTP GET request with GUID path parameter arrives at `ProductsController.GetById(Guid id)` (line 33)
2. Controller calls `ProductService.GetByIdAsync(id)` (line 29)

**Data Processing:**
- `ProductService.GetByIdAsync()` (line 29-35):
  - Queries `_db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id)`
  - Returns `null` if not found
  - Maps to `ProductDto` via `MapToDto()`

**Response:**
- HTTP 200 with `ProductDto` if found
- HTTP 404 if product not found

#### 1.1.3 GET /api/v1/products/sku/{sku} (Get Product by SKU)

**Request Flow:**
1. HTTP GET request with SKU path parameter arrives at `ProductsController.GetBySku(string sku)` (line 44)
2. Controller calls `ProductService.GetBySkuAsync(sku)` (line 39)

**Data Processing:**
- `ProductService.GetBySkuAsync()` (line 39-45):
  - Queries `_db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.SKU == sku)`
  - SKU is unique (enforced by database index in `InventoryDbContext.OnModelCreating()`, line 19)

**Response:**
- HTTP 200 with `ProductDto` if found
- HTTP 404 with error message `{"message": "Product with SKU '{sku}' not found."}` if not found

**Error Scenarios:**
- SKU not found: Returns 404 with descriptive message (line 47 in ProductsController)

#### 1.1.4 GET /api/v1/products/search (Search Products)

**Request Flow:**
1. HTTP GET request with query parameter `q` arrives at `ProductsController.Search(string q)` (line 57)
2. Controller calls `ProductService.SearchAsync(q)` (line 51)

**Data Processing:**
- `ProductService.SearchAsync()` (line 47-62):
  - If query is empty or null/whitespace, returns all active products via `GetAllAsync()` (line 49)
  - Otherwise, performs case-insensitive search on:
    - `Name`
    - `SKU`
    - `Description` (if not null)
    - `Brand` (if not null)
  - Only returns active products (`p.IsActive`)
  - Orders by `Name`

**Query Parameter Handling:**
- Empty string `q=""`: Returns all active products (line 49)
- Whitespace-only query: Returns all active products (line 49)
- Non-empty query: Performs multi-field search with case-insensitive matching (line 51-62)

**Response:**
- HTTP 200 with `IEnumerable<ProductDto>`

#### 1.1.5 GET /api/v1/products/category/{categoryId} (Get Products by Category)

**Request Flow:**
1. HTTP GET request with category GUID arrives at `ProductsController.GetByCategory(Guid categoryId)` (line 70)
2. Controller calls `ProductService.GetByCategoryAsync(categoryId)` (line 64)

**Data Processing:**
- `ProductService.GetByCategoryAsync()` (line 64-72):
  - Queries products where `CategoryId == categoryId && IsActive` (line 68)
  - Only returns active products (line 68)
  - Orders by `Name`

**Filtering Logic:**
- Filters by `CategoryId` match
- Filters by `IsActive == true` (inactive products excluded)
- Inactive products in the category are not returned

**Response:**
- HTTP 200 with `IEnumerable<ProductDto>`

#### 1.1.6 GET /api/v1/products/low-stock (Get Low Stock Products)

**Request Flow:**
1. HTTP GET request arrives at `ProductsController.GetLowStock()` (line 81)
2. Controller calls `ProductService.GetLowStockAsync()` (line 75)

**Data Processing:**
- `ProductService.GetLowStockAsync()` (line 75-83):
  - Queries products where `IsActive && StockQuantity <= ReorderPoint` (line 79)
  - Orders by `StockQuantity` ascending (lowest stock first)
  - `IsLowStock` computed property in `Product` model (line 56) returns `StockQuantity <= ReorderPoint`

**Response:**
- HTTP 200 with `IEnumerable<ProductDto>`

**Example Response (Multiple Products):**
```json
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "name": "Design Patterns: GoF",
    "sku": "BOOK-002",
    "stockQuantity": 8,
    "reorderPoint": 10,
    "isLowStock": true,
    "categoryName": "Books"
  },
  {
    "id": "550e8400-e29b-41d4-a716-446655440002",
    "name": "Wireless Headphones Pro",
    "sku": "ELEC-001",
    "stockQuantity": 15,
    "reorderPoint": 15,
    "isLowStock": true,
    "categoryName": "Electronics"
  }
]
```

#### 1.1.7 POST /api/v1/products (Create Product)

**Request Flow:**
1. HTTP POST request with JSON body arrives at `ProductsController.Create(CreateProductRequest request)` (line 93)
2. ModelState validation occurs
3. Controller calls `ProductService.CreateAsync(request)` (line 104)

**Request Body Structure:**
```json
{
  "name": "New Product",
  "sku": "NEW-001",
  "description": "Product description",
  "price": 99.99,
  "cost": 40.00,
  "stockQuantity": 50,
  "reorderPoint": 10,
  "brand": "BrandName",
  "weightKg": 1.5,
  "categoryId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Validation Rules** (from `CreateProductRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 24-34):
- `Name`: Required, MaxLength(200)
- `SKU`: Required, MaxLength(100)
- `Description`: Optional, MaxLength(2000)
- `Price`: Range(0, double.MaxValue)
- `Cost`: Range(0, double.MaxValue)
- `StockQuantity`: Range(0, int.MaxValue)
- `ReorderPoint`: Range(0, int.MaxValue)
- `Brand`: Optional, no length constraint
- `WeightKg`: Optional, decimal precision
- `CategoryId`: Required

**Data Processing:**
- `ProductService.CreateAsync()` (line 85-118):
  1. Validates SKU uniqueness: `_db.Products.AnyAsync(p => p.SKU == request.SKU)` (line 86)
     - Throws `InvalidOperationException` with message: `"A product with SKU '{request.SKU}' already exists."` if duplicate
  2. Validates category exists and is active (line 89-91)
     - Throws `InvalidOperationException` with message: `"Category with ID '{request.CategoryId}' does not exist or is inactive."` if invalid
  3. Creates new `Product` entity with all request fields
  4. Adds to `_db.Products` and calls `SaveChangesAsync()` (line 113)
  5. Logs creation: `_logger.LogInformation("Created product {SKU} - {Name}", ...)` (line 115)
  6. Loads category relationship via explicit loading: `await _db.Entry(product).Reference(p => p.Category).LoadAsync()` (line 116)
  7. Maps to `ProductDto`

**Response:**
- HTTP 201 Created with `ProductDto` and Location header: `Location: /api/v1/products/{id}`
- HTTP 400 Bad Request if validation fails (ModelState errors)
- HTTP 409 Conflict if SKU duplicate or category invalid

**Database Transaction:**
- Single `SaveChangesAsync()` call ensures atomicity

#### 1.1.8 PUT /api/v1/products/{id} (Update Product)

**Request Flow:**
1. HTTP PUT request with GUID and JSON body arrives at `ProductsController.Update(Guid id, UpdateProductRequest request)` (line 127)
2. Controller calls `ProductService.UpdateAsync(id, request)` (line 141)

**Request Body Structure:**
```json
{
  "name": "Updated Name",
  "description": "Updated description",
  "price": 109.99,
  "cost": 45.00,
  "reorderPoint": 15,
  "isActive": true,
  "brand": "NewBrand",
  "weightKg": 2.0,
  "categoryId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Validation Rules** (from `UpdateProductRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 36-45):
- `Name`: Required, MaxLength(200)
- `Description`: Optional, MaxLength(2000)
- `Price`: Range(0, double.MaxValue)
- `Cost`: Range(0, double.MaxValue)
- `ReorderPoint`: Range(0, int.MaxValue)
- `IsActive`: Boolean
- `Brand`: Optional
- `WeightKg`: Optional
- `CategoryId`: Required

**Data Processing:**
- `ProductService.UpdateAsync()` (line 120-145):
  1. Finds product by ID: `_db.Products.FindAsync(id)`
  2. Validates category exists and is active (line 128-130)
  3. Updates all mutable fields (Name, Description, Price, Cost, ReorderPoint, IsActive, Brand, WeightKg, CategoryId)
  4. Sets `UpdatedAt = DateTime.UtcNow` (line 139)
  5. Calls `SaveChangesAsync()` (line 141)
  6. Logs update: `_logger.LogInformation("Updated product {SKU}", product.SKU)` (line 142)
  7. Loads category via explicit loading (line 144)
  8. Maps to `ProductDto`

**Response:**
- HTTP 200 with updated `ProductDto`
- HTTP 404 if product not found
- HTTP 400 if category invalid

**Note:** SKU is immutable (not updated)

#### 1.1.9 DELETE /api/v1/products/{id} (Delete Product)

**Request Flow:**
1. HTTP DELETE request with GUID arrives at `ProductsController.Delete(Guid id)` (line 154)
2. Controller calls `ProductService.DeleteAsync(id)` (line 161)

**Data Processing:**
- `ProductService.DeleteAsync()` (line 147-155):
  1. Finds product by ID: `_db.Products.FindAsync(id)`
  2. Removes from `_db.Products`
  3. Calls `SaveChangesAsync()` (line 152)
  4. Logs deletion: `_logger.LogInformation("Deleted product {Id}", id)` (line 152)
  5. Returns boolean success indicator

**Response:**
- HTTP 204 No Content if successful
- HTTP 404 if product not found

**Cascade Behavior:**
- Product deletion is restricted if it has associated `OrderLine` entries (enforced by `OnDelete(DeleteBehavior.Restrict)` in `InventoryDbContext.OnModelCreating()`, line 33)
- Attempting to delete a product with order lines will result in a database constraint violation

#### 1.1.10 POST /api/v1/products/{id}/stock (Adjust Stock)

**Request Flow:**
1. HTTP POST request with GUID and JSON body arrives at `ProductsController.AdjustStock(Guid id, StockAdjustmentRequest request)` (line 169)
2. Controller calls `ProductService.AdjustStockAsync(id, request)` (line 180)

**Request Body Structure:**
```json
{
  "quantity": 25,
  "reason": "Restock from supplier"
}
```

**Validation Rules** (from `StockAdjustmentRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 47-50):
- `Quantity`: Required, integer (can be positive or negative)
- `Reason`: Optional, MaxLength(500)

**Data Processing:**
- `ProductService.AdjustStockAsync()` (line 157-170):
  1. Finds product with category loaded: `_db.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id)` (line 158)
  2. Calls `product.UpdateStock(request.Quantity)` (line 165)
  3. `Product.UpdateStock()` in `src/InventoryApi/Models/Product.cs` (line 57-62):
     - Validates `StockQuantity + quantity >= 0`
     - Throws `InvalidOperationException` with message: `"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}"` if insufficient stock
     - Updates `StockQuantity += quantity`
     - Sets `UpdatedAt = DateTime.UtcNow`
  4. Calls `SaveChangesAsync()` (line 166)
  5. Logs adjustment: `_logger.LogInformation("Stock adjusted for {SKU}: {Delta} (Reason: {Reason})", product.SKU, request.Quantity, request.Reason)` (line 167)
  6. Maps to `ProductDto`

**Response:**
- HTTP 200 with updated `ProductDto`
- HTTP 404 if product not found
- HTTP 400 if adjustment would result in negative stock

**Behavior with Negative Quantities:**
- Positive quantity: Restock operation (increases stock)
- Negative quantity: Sale/adjustment operation (decreases stock)
- Example: `quantity: -5` deducts 5 units from stock
- Validation prevents stock from going below zero

**Example Flows:**
- Restock: `quantity: 25, reason: "Restock from supplier"` → StockQuantity increases by 25
- Sale adjustment: `quantity: -3, reason: "Manual correction"` → StockQuantity decreases by 3
- Invalid: `quantity: -100` on product with 50 stock → HTTP 400 with error message

---

### 1.2 Categories API Endpoints

#### 1.2.1 GET /api/v1/categories (Get All Categories)

**Request Flow:**
1. HTTP GET request arrives at `CategoriesController.GetAll()` in `src/InventoryApi/Controllers/CategoriesController.cs` (line 23)
2. Controller calls `CategoryService.GetAllAsync()` in `src/InventoryApi/Services/CategoryService.cs` (line 18)

**Data Processing:**
- `CategoryService.GetAllAsync()` (line 18-26):
  - Queries `_db.Categories.Include(c => c.Products)`
  - Orders by `Name`
  - Maps each `Category` to `CategoryDto` via `MapToDto()` (line 89)

**Response:**
- HTTP 200 with `IEnumerable<CategoryDto>`
- Each `CategoryDto` contains: `Id`, `Name`, `Description`, `Slug`, `IsActive`, `ProductCount`, `CreatedAt`, `UpdatedAt`

#### 1.2.2 GET /api/v1/categories/{id} (Get Category by ID)

**Request Flow:**
1. HTTP GET request with GUID arrives at `CategoriesController.GetById(Guid id)` (line 32)
2. Controller calls `CategoryService.GetByIdAsync(id)` (line 28)

**Data Processing:**
- `CategoryService.GetByIdAsync()` (line 28-35):
  - Queries `_db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id)`
  - Maps to `CategoryDto`

**Response:**
- HTTP 200 with `CategoryDto` if found
- HTTP 404 if not found

#### 1.2.3 POST /api/v1/categories (Create Category)

**Request Flow:**
1. HTTP POST request with JSON body arrives at `CategoriesController.Create(CreateCategoryRequest request)` (line 46)
2. Controller calls `CategoryService.CreateAsync(request)` (line 57)

**Request Body Structure:**
```json
{
  "name": "New Category",
  "description": "Category description",
  "slug": "new-category"
}
```

**Validation Rules** (from `CreateCategoryRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 52-56):
- `Name`: Required, MaxLength(100)
- `Description`: Optional, MaxLength(500)
- `Slug`: Optional, MaxLength(50)

**Data Processing:**
- `CategoryService.CreateAsync()` (line 37-56):
  1. Validates category name uniqueness: `_db.Categories.AnyAsync(c => c.Name == request.Name)` (line 38)
     - Throws `InvalidOperationException` with message: `"A category named '{request.Name}' already exists."` if duplicate
  2. Generates slug if not provided (line 40):
     - Algorithm: `request.Name.ToLower().Replace(" ", "-").Replace("&", "and")`
     - Example: "Food & Beverage" → "food-and-beverage"
  3. Creates new `Category` entity
  4. Adds to `_db.Categories` and calls `SaveChangesAsync()` (line 49)
  5. Logs creation: `_logger.LogInformation("Created category {Name}", category.Name)` (line 51)
  6. Maps to `CategoryDto`

**Response:**
- HTTP 201 Created with `CategoryDto`
- HTTP 409 Conflict if name duplicate

#### 1.2.4 PUT /api/v1/categories/{id} (Update Category)

**Request Flow:**
1. HTTP PUT request with GUID and JSON body arrives at `CategoriesController.Update(Guid id, CreateCategoryRequest request)` (line 70)
2. Controller calls `CategoryService.UpdateAsync(id, request)` (line 81)

**Data Processing:**
- `CategoryService.UpdateAsync()` (line 58-72):
  1. Finds category with products loaded: `_db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id)` (line 59)
  2. Updates `Name`, `Description`
  3. Updates `Slug` with preservation logic (line 64):
     - If `request.Slug` is provided, use it
     - If `request.Slug` is null/empty, preserve existing slug: `request.Slug ?? category.Slug`
  4. Sets `UpdatedAt = DateTime.UtcNow` (line 65)
  5. Calls `SaveChangesAsync()` (line 67)
  6. Logs update: `_logger.LogInformation("Updated category {Id}", id)` (line 70)
  7. Maps to `CategoryDto`

**Slug Update Behavior:**
- Slug is preserved if not provided in request
- Slug is updated only if explicitly provided in request body

**Response:**
- HTTP 200 with updated `CategoryDto`
- HTTP 404 if not found

#### 1.2.5 DELETE /api/v1/categories/{id} (Delete Category)

**Request Flow:**
1. HTTP DELETE request with GUID arrives at `CategoriesController.Delete(Guid id)` (line 93)
2. Controller calls `CategoryService.DeleteAsync(id)` (line 103)

**Data Processing:**
- `CategoryService.DeleteAsync()` (line 74-87):
  1. Finds category with products loaded: `_db.Categories.Include(c => c.Products).FirstOrDefaultAsync(c => c.Id == id)` (line 75)
  2. Validates no products exist: `if (category.Products.Any())` (line 80)
  3. Throws `InvalidOperationException` with message: `"Cannot delete a category that contains products. Reassign or delete products first."` if products exist
  4. Removes from `_db.Categories` (line 84)
  5. Calls `SaveChangesAsync()` (line 85)
  6. Logs deletion: `_logger.LogInformation("Deleted category {Id}", id)` (line 85)

**Response:**
- HTTP 204 No Content if successful
- HTTP 404 if not found
- HTTP 400 with error message if category contains products

**Error Response Example (Category Contains Products):**
```json
{
  "message": "Cannot delete a category that contains products. Reassign or delete products first."
}
```

---

### 1.3 Orders API Endpoints

#### 1.3.1 GET /api/v1/orders (Get All Orders)

**Request Flow:**
1. HTTP GET request arrives at `OrdersController.GetAll()` in `src/InventoryApi/Controllers/OrdersController.cs` (line 24)
2. Controller calls `OrderService.GetAllAsync()` in `src/InventoryApi/Services/OrderService.cs` (line 19)

**Data Processing:**
- `OrderService.GetAllAsync()` (line 19-28):
  - Queries `_db.Orders.Include(o => o.Lines).ThenInclude(l => l.Product)`
  - Orders by `CreatedAt` descending (newest first)
  - Maps each `Order` to `OrderDto` via `MapToDto()` (line 113)

**Response:**
- HTTP 200 with `IEnumerable<OrderDto>`
- Each `OrderDto` contains: `Id`, `OrderNumber`, `CustomerName`, `CustomerEmail`, `Status`, `TotalAmount`, `ShippingAddress`, `Notes`, `Lines`, `CreatedAt`, `UpdatedAt`

#### 1.3.2 GET /api/v1/orders/{id} (Get Order by ID)

**Request Flow:**
1. HTTP GET request with GUID arrives at `OrdersController.GetById(Guid id)` (line 35)
2. Controller calls `OrderService.GetByIdAsync(id)` (line 30)

**Data Processing:**
- `OrderService.GetByIdAsync()` (line 30-39):
  - Queries `_db.Orders.Include(o => o.Lines).ThenInclude(l => l.Product).FirstOrDefaultAsync(o => o.Id == id)`
  - Maps to `OrderDto`

**Response:**
- HTTP 200 with `OrderDto` if found
- HTTP 404 if not found

#### 1.3.3 POST /api/v1/orders (Create Order)

**Request Flow:**
1. HTTP POST request with JSON body arrives at `OrdersController.Create(CreateOrderRequest request)` (line 48)
2. Controller calls `OrderService.CreateAsync(request)` (line 59)

**Request Body Structure:**
```json
{
  "customerName": "John Doe",
  "customerEmail": "john@example.com",
  "shippingAddress": "123 Main St, City, State 12345",
  "notes": "Handle with care",
  "lines": [
    {
      "productId": "550e8400-e29b-41d4-a716-446655440000",
      "quantity": 2
    },
    {
      "productId": "550e8400-e29b-41d4-a716-446655440001",
      "quantity": 1
    }
  ]
}
```

**Validation Rules** (from `CreateOrderRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 58-65):
- `CustomerName`: Required, MaxLength(200)
- `CustomerEmail`: Optional, MaxLength(200)
- `ShippingAddress`: Optional, MaxLength(500)
- `Notes`: Optional, MaxLength(1000)
- `Lines`: Required, must be non-empty list

**Line Item Validation Rules** (from `CreateOrderLineRequest` in `src/InventoryApi/DTOs/ProductDto.cs`, line 67-71):
- `ProductId`: Required
- `Quantity`: Range(1, int.MaxValue) - must be at least 1

**Data Processing:**
- `OrderService.CreateAsync()` (line 41-101):
  1. Validates order has at least one line item (line 43)
     - Throws `InvalidOperationException` with message: `"Order must have at least one line item."` if empty
  2. Extracts all product IDs from lines (line 46)
  3. Fetches all products in single query: `_db.Products.Where(p => productIds.Contains(p.Id) && p.IsActive).ToListAsync()` (line 48-50)
  4. Validates all products exist and are active (line 52-53)
     - Throws `InvalidOperationException` with message: `"One or more products not found or inactive."` if mismatch
  5. **Stock Validation Loop** (line 56-60):
     - For each line item, checks: `product.StockQuantity >= lineReq.Quantity`
     - Throws `InvalidOperationException` with detailed message: `"Insufficient stock for '{product.Name}' (SKU: {product.SKU}). Available: {product.StockQuantity}, Requested: {lineReq.Quantity}"` if insufficient
  6. **Order Creation** (line 62-63):
     - Generates unique order number: `$"ORD-{Interlocked.Increment(ref _orderCounter):D6}"` (line 62)
     - Format: ORD-001001, ORD-001002, etc. (6-digit zero-padded counter)
     - Creates `Order` entity with status `OrderStatus.Confirmed`
  7. **Stock Deduction Loop** (line 65-75):
     - For each line item:
       - Calls `product.UpdateStock(-lineReq.Quantity)` to deduct stock
       - Creates `OrderLine` with product snapshot (Name, SKU, UnitPrice)
  8. **Total Calculation** (line 77):
     - Calls `order.RecalculateTotal()` which sums `LineTotal` for all lines
  9. Adds order to `_db.Orders` and calls `SaveChangesAsync()` (line 79)
  10. Logs order creation: `_logger.LogInformation("Order {OrderNumber} created for customer {Customer}, total {Total:C}", ...)` (line 82-84)
  11. Maps to `OrderDto`

**Response:**
- HTTP 201 Created with `OrderDto` and Location header: `Location: /api/v1/orders/{id}`
- HTTP 400 Bad Request if:
  - No line items
  - Product not found or inactive
  - Insufficient stock

**Error Response Examples:**

*Insufficient Stock:*
```json
{
  "message": "Insufficient stock for 'Wireless Headphones Pro' (SKU: ELEC-001). Available: 5, Requested: 10"
}
```

*Product Not Found:*
```json
{
  "message": "One or more products not found or inactive."
}
```

**Critical Data Transformation:**
- `OrderLine.LineTotal` computed property: `Quantity * UnitPrice` (line 84 in `Order.cs`)
- `Order.TotalAmount` = sum of all `OrderLine.LineTotal` values
- Product snapshot captured at order time (Name, SKU, UnitPrice) - not linked to current product state

**Stock Deduction Atomicity:**
- All stock deductions occur within single `SaveChangesAsync()` call (line 79)
- If any deduction fails, entire transaction rolls back
- Prevents partial order creation with partial stock deductions

#### 1.3.4 PATCH /api/v1/orders/{id}/status (Update Order Status)

**Request Flow:**
1. HTTP PATCH request with GUID and JSON body arrives at `OrdersController.UpdateStatus(Guid id, UpdateOrderStatusRequest request)` (line 67)
2. Controller validates status enum with case-insensitive parsing (line 69-70)
3. Controller calls `OrderService.UpdateStatusAsync(id, status)` (line 72)

**Request Body Structure:**
```json
{
  "status": "Shipped"
}
```

**Validation Rules** (from `UpdateOrderStatusRequest` in `src/InventoryApi/Controllers/OrdersController.cs`, line 81):
- `Status`: Required string

**Valid Status Values:**
- `Pending`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `Refunded`
- Defined in `OrderStatus` enum in `src/InventoryApi/Models/Order.cs` (line 7-14)

**Status Parsing:**
- Case-insensitive parsing: `Enum.TryParse<OrderStatus>(request.Status, ignoreCase: true, out var status)` (line 69)
- Accepts: "shipped", "Shipped", "SHIPPED", etc.

**Data Processing:**
- `OrderService.UpdateStatusAsync()` (line 103-112):
  1. Finds order with lines loaded: `_db.Orders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == id)` (line 104)
  2. Updates `Status` field (line 107)
  3. Sets `UpdatedAt = DateTime.UtcNow` (line 108)
  4. Calls `SaveChangesAsync()` (line 109)
  5. Logs status change: `_logger.LogInformation("Order {OrderNumber} status updated to {Status}", order.OrderNumber, status)` (line 110)
  6. Maps to `OrderDto`

**Response:**
- HTTP 200 with updated `OrderDto`
- HTTP 404 if order not found
- HTTP 400 if invalid status with message: `"Invalid status '{status}'. Valid values: Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Refunded"`

**Error Response Example (Invalid Status):**
```json
{
  "message": "Invalid status 'InvalidStatus'. Valid values: Pending, Confirmed, Processing, Shipped, Delivered, Cancelled, Refunded"
}
```

---

## 2. Data Transformations and Processing Pipelines

### 2.1 Entity-to-DTO Mapping Pipeline

All API responses use Data Transfer Objects (DTOs) to decouple API contracts from domain models.

#### 2.1.1 Product Entity → ProductDto

**Mapping Function:** `ProductService.MapToDto()` in `src/InventoryApi/Services/ProductService.cs` (line 175-182)

```csharp
private static ProductDto MapToDto(Product p) => new(
    p.Id, p.Name, p.SKU, p.Description, p.Price, p.Cost,
    p.StockQuantity, p.ReorderPoint, p.IsActive, p.Brand,
    p.WeightKg, p.CategoryId, p.Category?.Name, p.IsLowStock,
    p.CreatedAt, p.UpdatedAt
);
```

**Transformations:**
- `Category` object → `CategoryName` (string) via null-coalescing: `p.Category?.Name`
  - Returns null if Category is not loaded
  - Returns category name string if Category is loaded
- `IsLowStock` computed property evaluated: `StockQuantity <= ReorderPoint`
- All timestamps preserved as-is (UTC DateTime)

#### 2.1.2 Category Entity → CategoryDto

**Mapping Function:** `CategoryService.MapToDto()` in `src/InventoryApi/Services/CategoryService.cs` (line 89-93)

```csharp
private static CategoryDto MapToDto(Category c) => new(
    c.Id, c.Name, c.Description, c.Slug, c.IsActive,
    c.Products.Count, c.CreatedAt, c.UpdatedAt
);
```

**Transformations:**
- `Products` collection → `ProductCount` (integer) via `.Count` property
- All other fields preserved as-is

#### 2.1.3 Order Entity → OrderDto

**Mapping Function:** `OrderService.MapToDto()` in `src/InventoryApi/Services/OrderService.cs` (line 113-119)

```csharp
private static OrderDto MapToDto(Order o) => new(
    o.Id, o.OrderNumber, o.CustomerName, o.CustomerEmail,
    o.Status.ToString(), o.TotalAmount, o.ShippingAddress, o.Notes,
    o.Lines.Select(l => new OrderLineDto(l.Id, l.ProductId, l.ProductName, l.ProductSKU, l.Quantity, l.UnitPrice, l.LineTotal)).ToList(),
    o.CreatedAt, o.UpdatedAt
);
```

**Transformations:**
- `OrderStatus` enum → string representation via `.ToString()` (e.g., `OrderStatus.Confirmed` → `"Confirmed"`)
- `OrderLine` collection → `OrderLineDto` collection via `.Select()`
- Each `OrderLineDto` includes computed `LineTotal`

#### 2.1.4 OrderLineDto Schema

**Definition:** `OrderLineDto` in `src/InventoryApi/DTOs/ProductDto.cs` (line 73-80)

```csharp
public record OrderLineDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSKU,
    int Quantity,
    decimal UnitPrice,
    decimal LineTotal
);
```

**Fields:**
- `Id`: Unique identifier for the order line
- `ProductId`: Reference to the product (for traceability)
- `ProductName`: Product name snapshot (captured at order time)
- `ProductSKU`: Product SKU snapshot (captured at order time)
- `Quantity`: Number of units ordered
- `UnitPrice`: Price per unit at order time
- `LineTotal`: Computed property = `Quantity * UnitPrice`

**Computed Properties:**
- `LineTotal`: Calculated as `Quantity * UnitPrice` (line 84 in `Order.cs`)
  - Example: 2 units × $149.99 = $299.98

### 2.2 Request-to-Entity Transformation Pipeline

#### 2.2.1 CreateProductRequest → Product

**Location:** `ProductService.CreateAsync()` in `src/InventoryApi/Services/ProductService.cs` (line 97-109)

**Transformation Steps:**
1. Validate SKU uniqueness
2. Validate category exists and is active
3. Create new `Product` entity with:
   - Auto-generated `Id` (GUID) via `Guid.NewGuid()` (line 8 in Product.cs)
   - All request fields mapped directly
   - `IsActive` defaults to `true` (line 33 in Product.cs)
   - `CreatedAt` and `UpdatedAt` set to `DateTime.UtcNow` (line 48-49 in Product.cs)

#### 2.2.2 UpdateProductRequest → Product

**Location:** `ProductService.UpdateAsync()` in `src/InventoryApi/Services/ProductService.cs` (line 120-145)

**Transformation Steps:**
1. Fetch existing product
2. Update mutable fields (all except SKU and Id)
3. Set `UpdatedAt = DateTime.UtcNow` (line 139)
4. Preserve `CreatedAt` (not modified)

#### 2.2.3 CreateOrderRequest → Order + OrderLine[]

**Location:** `OrderService.CreateAsync()` in `src/InventoryApi/Services/OrderService.cs` (line 41-101)

**Transformation Steps:**
1. Validate line items exist
2. Fetch all products
3. Validate stock availability
4. Create `Order` entity:
   - Auto-generated `Id` (GUID)
   - Auto-generated `OrderNumber` (ORD-XXXXXX format)
   - Status set to `OrderStatus.Confirmed`
   - `CreatedAt` and `UpdatedAt` set to `DateTime.UtcNow`
5. Create `OrderLine` entities for each line:
   - Auto-generated `Id` (GUID)
   - Product snapshot (Name, SKU, UnitPrice)
   - Quantity from request
6. Deduct stock from products
7. Calculate total amount

### 2.3 Search and Filter Pipelines

#### 2.3.1 Product Search Pipeline

**Location:** `ProductService.SearchAsync()` in `src/InventoryApi/Services/ProductService.cs` (line 47-62)

**Pipeline Steps:**
1. Check if query is empty/null/whitespace (line 48-49)
   - If empty, return all active products via `GetAllAsync()`
2. Normalize query to lowercase (line 51)
3. Filter by `IsActive == true` (line 53)
4. Apply multi-field search with null-safe operators (line 54-58):
   - `Name.ToLower().Contains(lower)` - always checked
   - `SKU.ToLower().Contains(lower)` - always checked
   - `(p.Description != null && p.Description.ToLower().Contains(lower))` - null-safe check
   - `(p.Brand != null && p.Brand.ToLower().Contains(lower))` - null-safe check
5. Order by `Name` (line 59)
6. Map to `ProductDto[]` (line 60)

**Database Query:**
```csharp
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
```

#### 2.3.2 Low Stock Filter Pipeline

**Location:** `ProductService.GetLowStockAsync()` in `src/InventoryApi/Services/ProductService.cs` (line 75-83)

**Pipeline Steps:**
1. Filter by `IsActive == true` (line 79)
2. Filter by `StockQuantity <= ReorderPoint` (line 79)
3. Order by `StockQuantity` ascending (lowest stock first) (line 80)
4. Map to `ProductDto[]` (line 81)

---

## 3. Event Flows and Messaging Patterns

### 3.1 Logging Events

The application uses structured logging with correlation IDs for request tracing.

#### 3.1.1 Request Logging Middleware

**Location:** `RequestLoggingMiddleware` in `src/InventoryApi/Middleware/RequestLoggingMiddleware.cs` (line 1-56)

**Event Flow:**

1. **Request Arrival** (line 16-24):
   - Extract correlation ID from `X-Correlation-ID` header if present
   - Generate new correlation ID if not provided: `Guid.NewGuid().ToString("N")[..8]` (8-character hex string)
   - Add correlation ID to response headers: `context.Response.Headers["X-Correlation-ID"] = correlationId`
   - Start stopwatch: `var sw = Stopwatch.StartNew()`
   - Log: `"[{CorrelationId}] {Method} {Path}{Query} started"`

2. **Request Processing** (line 26-31):
   - Call next middleware in pipeline: `await _next(context)`
   - Catch any exceptions in finally block (line 32)

3. **Response Logging** (line 33-47):
   - Stop stopwatch: `sw.Stop()`
   - Determine log level based on status code (line 35-39):
     - `>= 500`: `LogLevel.Error`
     - `>= 400`: `LogLevel.Warning`
     - Otherwise: `LogLevel.Information`
   - Log: `"[{CorrelationId}] {Method} {Path} responded {StatusCode} in {ElapsedMs}ms"`

**Example Log Output:**
```
[a1b2c3d4] GET /api/v1/products started
[a1b2c3d4] GET /api/v1/products responded 200 in 45ms
[a1b2c3d4] POST /api/v1/products responded 201 in 120ms
[a1b2c3d4] GET /api/v1/products/invalid-id responded 400 in 15ms
[a1b2c3d4] GET /api/v1/products/500 responded 500 in 200ms
```

**Correlation ID Propagation:**
- Client can provide `X-Correlation-ID` header in request
- If not provided, middleware generates 8-character ID
- ID is added to response headers for client tracking
- All logs within request scope include correlation ID

#### 3.1.2 Service-Level Logging

**Product Service Logging:**
- `ProductService.CreateAsync()` (line 115): `"Created product {SKU} - {Name}"` with parameters: `product.SKU, product.Name`
- `ProductService.UpdateAsync()` (line 142): `"Updated product {SKU}"` with parameter: `product.SKU`
- `ProductService.DeleteAsync()` (line 152): `"Deleted product {Id}"` with parameter: `id`
- `ProductService.AdjustStockAsync()` (line 167): `"Stock adjusted for {SKU}: {Delta} (Reason: {Reason})"` with parameters: `product.SKU, request.Quantity, request.Reason`

**Order Service Logging:**
- `OrderService.CreateAsync()` (line 82-84): `"Order {OrderNumber} created for customer {Customer}, total {Total:C}"` with parameters: `order.OrderNumber, order.CustomerName, order.TotalAmount`
  - Format specifier `:C` formats amount as currency
- `OrderService.UpdateStatusAsync()` (line 110): `"Order {OrderNumber} status updated to {Status}"` with parameters: `order.OrderNumber, status`

**Category Service Logging:**
- `CategoryService.CreateAsync()` (line 51): `"Created category {Name}"` with parameter: `category.Name`
- `CategoryService.UpdateAsync()` (line 70): `"Updated category {Id}"` with parameter: `id`
- `CategoryService.DeleteAsync()` (line 85): `"Deleted category {Id}"` with parameter: `id`

**Controller-Level Logging:**
- `ProductsController.Create()` (line 109): `"Failed to create product: {Message}"` on exception
- `OrdersController.Create()` (line 59): `"Failed to create order: {Message}"` on exception
- `CategoriesController.Create()` (line 57): `"Failed to create category: {Message}"` on exception

### 3.2 Error Events

#### 3.2.1 Validation Errors

**Product Creation Errors:**
- SKU duplicate: `InvalidOperationException` with message: `"A product with SKU '{request.SKU}' already exists."` (line 86-87)
- Category not found: `InvalidOperationException` with message: `"Category with ID '{request.CategoryId}' does not exist or is inactive."` (line 89-91)

**Order Creation Errors:**
- No line items: `InvalidOperationException` with message: `"Order must have at least one line item."` (line 43)
- Product not found: `InvalidOperationException` with message: `"One or more products not found or inactive."` (line 52-53)
- Insufficient stock: `InvalidOperationException` with message: `"Insufficient stock for '{product.Name}' (SKU: {product.SKU}). Available: {product.StockQuantity}, Requested: {lineReq.Quantity}"` (line 58-59)

**Category Deletion Errors:**
- Category contains products: `InvalidOperationException` with message: `"Cannot delete a category that contains products. Reassign or delete products first."` (line 80)

**Stock Adjustment Errors:**
- Insufficient stock: `InvalidOperationException` with message: `"Insufficient stock for product {SKU}. Available: {StockQuantity}, Requested: {-quantity}"` (line 59 in Product.cs)

#### 3.2.2 Error Response Handling

**Location:** Controllers catch `InvalidOperationException` and return appropriate HTTP responses

**Example:** `ProductsController.Create()` (line 103-109)
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

**Error Response Format:**
```json
{
  "message": "Error message from exception"
}
```

**ModelState Validation Error Response:**
```json
{
  "errors": {
    "Name": ["The Name field is required."],
    "Price": ["The field Price must be between 0 and 9223372036854775807."]
  }
}
```

---

## 4. Integration Points with External Services

### 4.1 Database Integration

**Provider:** Entity Framework Core with in-memory database

**Location:** `InventoryDbContext` in `src/InventoryApi/Data/InventoryDbContext.cs`

**Configuration:** `Program.cs` (line 18-19)
```csharp
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));
```

**DbSet Definitions:**
- `DbSet<Product>` (line 9)
- `DbSet<Category>` (line 10)
- `DbSet<Order>` (line 11)
- `DbSet<OrderLine>` (line 12)

### 4.2 Dependency Injection Integration

**Service Registration:** `Program.cs` (line 24-26)
```csharp
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();
```

**Lifetime:** Scoped (one instance per HTTP request)

**Injection Points:**
- Controllers receive services via constructor injection
- Services receive `InventoryDbContext` via constructor injection
- Services receive `ILogger<T>` via constructor injection

### 4.3 CORS Integration

**Configuration:** `Program.cs` (line 28-35)
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

**Middleware:** `Program.cs` (line 52)
```csharp
app.UseCors();
```

**Effect:** All origins can make requests with any HTTP method and headers

### 4.4 Swagger/OpenAPI Integration

**Configuration:** `Program.cs` (line 12-17)
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

**Middleware:** `Program.cs` (line 49-50)
```csharp
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
```

**Endpoint:** Available at `/swagger/index.html` in development

---

## 5. Database Read/Write Patterns and Transactions

### 5.1 Read Patterns

#### 5.1.1 Single Entity Read

**Pattern:** `FindAsync()` or `FirstOrDefaultAsync()`

**Example:** `ProductService.GetByIdAsync()` (line 29-35)
```csharp
var product = await _db.Products
    .Include(p => p.Category)
    .FirstOrDefaultAsync(p => p.Id == id);
```

**Characteristics:**
- Eager loads related entities with `.Include()`
- Returns null if not found
- No tracking for read-only scenarios

#### 5.1.2 Collection Read with Filtering

**Pattern:** `Where()` + `ToListAsync()`

**Example:** `ProductService.SearchAsync()` (line 51-62)
```csharp
var products = await _db.Products
    .Include(p => p.Category)
    .Where(p => p.IsActive && (...))
    .OrderBy(p => p.Name)
    .ToListAsync();
```

**Characteristics:**
- Filters applied at database level
- Ordering applied at database level
- All results materialized to memory

#### 5.1.3 Existence Check

**Pattern:** `AnyAsync()`

**Example:** `ProductService.CreateAsync()` (line 86)
```csharp
if (await _db.Products.AnyAsync(p => p.SKU == request.SKU))
    throw new InvalidOperationException(...);
```

**Characteristics:**
- Efficient database query (returns boolean)
- No data materialization

### 5.2 Write Patterns

#### 5.2.1 Create Pattern

**Location:** `ProductService.CreateAsync()` (line 85-118)

**Steps:**
1. Validate business rules (SKU uniqueness, category exists)
2. Create entity instance
3. Add to DbSet: `_db.Products.Add(product)`
4. Persist: `await _db.SaveChangesAsync()`
5. Load relationships: `await _db.Entry(product).Reference(p => p.Category).LoadAsync()`
6. Map to DTO

**Transaction Scope:** Single `SaveChangesAsync()` call

#### 5.2.2 Update Pattern

**Location:** `ProductService.UpdateAsync()` (line 120-145)

**Steps:**
1. Fetch entity: `_db.Products.FindAsync(id)`
2. Validate business rules
3. Update properties
4. Set `UpdatedAt = DateTime.UtcNow`
5. Persist: `await _db.SaveChangesAsync()`
6. Load relationships
7. Map to DTO

**Transaction Scope:** Single `SaveChangesAsync()` call

**Change Tracking:** EF Core automatically detects property changes via `FindAsync()` which returns tracked entities

#### 5.2.3 Delete Pattern

**Location:** `ProductService.DeleteAsync()` (line 147-155)

**Steps:**
1. Fetch entity: `_db.Products.FindAsync(id)`
2. Remove from DbSet: `_db.Products.Remove(product)`
3. Persist: `await _db.SaveChangesAsync()`

**Transaction Scope:** Single `SaveChangesAsync()` call

**Cascade Behavior:** Restricted by foreign key constraint (line 33 in `InventoryDbContext`)

#### 5.2.4 Complex Write Pattern (Order Creation)

**Location:** `OrderService.CreateAsync()` (line 41-101)

**Steps:**
1. Validate order has line items
2. Fetch all products in single query
3. Validate all products exist and are active
4. Validate stock availability for all items
5. Create Order entity
6. For each line item:
   - Deduct stock: `product.UpdateStock(-quantity)`
   - Create OrderLine entity
7. Calculate total: `order.RecalculateTotal()`
8. Add order to DbSet
9. Persist all changes: `await _db.SaveChangesAsync()`

**Transaction Scope:** Single `SaveChangesAsync()` call ensures atomicity
- All stock deductions succeed or all fail
- Order and all lines created together

**Critical Invariant:** Stock deductions only occur after all validations pass

### 5.3 Relationship Loading Patterns

#### 5.3.1 Eager Loading

**Pattern:** `.Include()` in query

**Example:** `ProductService.GetAllAsync()` (line 20)
```csharp
var query = _db.Products.Include(p => p.Category).AsQueryable();
```

**Effect:** Category loaded in single query with products

#### 5.3.2 Explicit Loading

**Pattern:** `.Reference().LoadAsync()` or `.Collection().LoadAsync()`

**Example:** `ProductService.CreateAsync()` (line 116)
```csharp
await _db.Entry(product).Reference(p => p.Category).LoadAsync();
```

**Effect:** Category loaded separately after entity creation

**Use Case:** After creating a new entity, load relationships for response mapping

#### 5.3.3 Nested Eager Loading

**Pattern:** `.Include().ThenInclude()`

**Example:** `OrderService.GetAllAsync()` (line 20-23)
```csharp
var orders = await _db.Orders
    .Include(o => o.Lines)
    .ThenInclude(l => l.Product)
    .OrderByDescending(o => o.CreatedAt)
    .ToListAsync();
```

**Effect:** Orders, OrderLines, and Products all loaded in optimized queries

### 5.4 Database Constraints and Indexes

**Location:** `InventoryDbContext.OnModelCreating()` (line 15-35)

#### 5.4.1 Unique Constraints

**Product SKU:**
```csharp
entity.HasIndex(p => p.SKU).IsUnique();
```
- Enforces SKU uniqueness at database level
- Prevents duplicate products
- Located at line 19

**Order Number:**
```csharp
entity.HasIndex(o => o.OrderNumber).IsUnique();
```
- Enforces order number uniqueness
- Prevents duplicate orders
- Located at line 27

#### 5.4.2 Foreign Key Constraints

**Product → Category:**
```csharp
entity.HasOne(p => p.Category)
      .WithMany(c => c.Products)
      .HasForeignKey(p => p.CategoryId)
      .OnDelete(DeleteBehavior.Restrict);
```
- Restricts product deletion if category has products
- Prevents orphaned products
- Located at line 20-24

**OrderLine → Order:**
```csharp
entity.HasMany(o => o.Lines)
      .WithOne(l => l.Order)
      .HasForeignKey(l => l.OrderId)
      .OnDelete(DeleteBehavior.Cascade);
```
- Cascades order deletion to order lines
- Prevents orphaned order lines
- Located at line 28-31

**OrderLine → Product:**
```csharp
entity.HasOne(l => l.Product)
      .WithMany(p => p.OrderLines)
      .HasForeignKey(l => l.ProductId)
      .OnDelete(DeleteBehavior.Restrict);
```
- Restricts product deletion if product has order lines
- Prevents orphaned order lines
- Located at line 33-36

---

## 6. State Management (Frontend and Backend)

### 6.1 Backend State Management

#### 6.1.1 Entity State in EF Core

**Change Tracking:** EF Core automatically tracks entity state

**States:**
- **Detached:** Entity not tracked
- **Added:** New entity, will be inserted
- **Modified:** Existing entity with changes, will be updated
- **Unchanged:** Existing entity with no changes
- **Deleted:** Entity marked for deletion

**Example:** `ProductService.UpdateAsync()` (line 120-145)
```csharp
var product = await _db.Products.FindAsync(id);  // State: Unchanged
product.Name = request.Name;                      // State: Modified
await _db.SaveChangesAsync();                     // Persists changes
```

#### 6.1.2 Order Counter State

**Location:** `OrderService` (line 14)
```csharp
private static int _orderCounter = 1000;
```

**Usage:** `OrderService.CreateAsync()` (line 62)
```csharp
var orderNumber = $"ORD-{Interlocked.Increment(ref _orderCounter):D6}";
```

**Characteristics:**
- Static field shared across all service instances
- Thread-safe increment using `Interlocked.Increment()`
- Generates sequential order numbers (ORD-001001, ORD-001002, etc.)
- State persists for application lifetime
- Format: ORD-XXXXXX (6-digit zero-padded counter)

**Limitation:** Resets on application restart (in-memory database)

#### 6.1.3 Database Context Lifetime

**Scope:** Per HTTP request (Scoped lifetime)

**Location:** `Program.cs` (line 24)
```csharp
builder.Services.AddScoped<ProductService>();
```

**Effect:**
- New `InventoryDbContext` instance created per request
- All changes tracked within request scope
- Context disposed after request completes
- Prevents state leakage between requests

### 6.2 Frontend State Management

**Note:** This is a backend-only API. No frontend state management is implemented.

**API Contract:** Clients must manage their own state based on API responses

**Recommended Client Patterns:**
- Store API responses in client-side state management (Redux, Vuex, etc.)
- Implement optimistic updates with rollback on error
- Use correlation IDs from response headers for request tracking

---

## 7. Caching Strategies and Data Invalidation

### 7.1 Current Caching Strategy

**Status:** No explicit caching implemented

**Rationale:** In-memory database provides fast access; caching would add complexity without significant benefit for demo application

### 7.2 Potential Caching Opportunities

#### 7.2.1 Category Caching

**Opportunity:** Categories change infrequently

**Implementation Pattern:**
```csharp
private static readonly MemoryCache _categoryCache = new MemoryCache(new MemoryCacheOptions());

public async Task<IEnumerable<CategoryDto>> GetAllAsync()
{
    if (_categoryCache.TryGetValue("all_categories", out var cached))
        return (IEnumerable<CategoryDto>)cached;
    
    var categories = await _db.Categories.Include(c => c.Products).OrderBy(c => c.Name).ToListAsync();
    var dtos = categories.Select(MapToDto).ToList();
    _categoryCache.Set("all_categories", dtos, TimeSpan.FromMinutes(30));
    return dtos;
}
```

**Invalidation Triggers:**
- `CreateAsync()`: Clear cache
- `UpdateAsync()`: Clear cache
- `DeleteAsync()`: Clear cache

#### 7.2.2 Product Search Caching

**Opportunity:** Search results are deterministic

**Implementation Pattern:**
```csharp
private static readonly MemoryCache _searchCache = new MemoryCache(new MemoryCacheOptions());

public async Task<IEnumerable<ProductDto>> SearchAsync(string query)
{
    var cacheKey = $"search_{query.ToLower()}";
    if (_searchCache.TryGetValue(cacheKey, out var cached))
        return (IEnumerable<ProductDto>)cached;
    
    // ... existing search logic ...
    
    _searchCache.Set(cacheKey, results, TimeSpan.FromMinutes(15));
    return results;
}
```

**Invalidation Triggers:**
- `CreateAsync()`: Clear all search caches
- `UpdateAsync()`: Clear all search caches
- `DeleteAsync()`: Clear all search caches
- `AdjustStockAsync()`: Clear low-stock cache only

#### 7.2.3 Low Stock Cache

**Opportunity:** Low stock list changes infrequently

**Implementation Pattern:**
```csharp
private static readonly MemoryCache _lowStockCache = new MemoryCache(new MemoryCacheOptions());

public async Task<IEnumerable<ProductDto>> GetLowStockAsync()
{
    if (_lowStockCache.TryGetValue("low_stock", out var cached))
        return (IEnumerable<ProductDto>)cached;
    
    // ... existing logic ...
    
    _lowStockCache.Set("low_stock", products, TimeSpan.FromMinutes(5));
    return products;
}
```

**Invalidation Triggers:**
- `AdjustStockAsync()`: Clear cache
- `CreateAsync()`: Clear cache if new product is low stock
- `UpdateAsync()`: Clear cache if reorder point changed

### 7.3 Cache Invalidation Strategy

**Pattern:** Explicit invalidation on write operations

**Benefits:**
- Ensures data consistency
- Simple to understand and debug
- No background cache refresh complexity

**Drawbacks:**
- Requires manual invalidation in each write method
- Risk of forgetting to invalidate

**Alternative:** Time-based expiration (TTL)
- Simpler implementation
- Eventual consistency model
- Trade-off: Stale data for short period

---

## 8. File and Blob Storage Flows

### 8.1 Current Implementation

**Status:** No file or blob storage implemented

**Rationale:** Demo application focuses on inventory data management; no file uploads required

### 8.2 Potential File Storage Scenarios

#### 8.2.1 Product Images

**Use Case:** Store product images for catalog

**Implementation Pattern:**
```csharp
public class Product
{
    // ... existing properties ...
    public string? ImageUrl { get; set; }
    public byte[]? ImageData { get; set; }
}

public record CreateProductRequest
{
    // ... existing fields ...
    public IFormFile? Image { get; set; }
}

public async Task<ProductDto> CreateAsync(CreateProductRequest request)
{
    // ... existing validation ...
    
    string? imageUrl = null;
    if (request.Image != null)
    {
        imageUrl = await _blobService.UploadAsync(request.Image);
    }
    
    var product = new Product { /* ... */, ImageUrl = imageUrl };
    // ... rest of creation ...
}
```

**Storage Options:**
- Azure Blob Storage
- AWS S3
- Local file system
- Database BLOB column

#### 8.2.2 Order Documents

**Use Case:** Store order receipts, invoices, shipping labels

**Implementation Pattern:**
```csharp
public class OrderDocument
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public string DocumentType { get; set; } // "Receipt", "Invoice", "ShippingLabel"
    public string FileName { get; set; }
    public string BlobUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
{
    // ... existing order creation ...
    
    var order = new Order { /* ... */ };
    _db.Orders.Add(order);
    await _db.SaveChangesAsync();
    
    // Generate and store receipt
    var receiptBlob = await _documentService.GenerateReceiptAsync(order);
    var document = new OrderDocument
    {
        OrderId = order.Id,
        DocumentType = "Receipt",
        FileName = $"receipt-{order.OrderNumber}.pdf",
        BlobUrl = receiptBlob.Uri.ToString()
    };
    _db.OrderDocuments.Add(document);
    await _db.SaveChangesAsync();
    
    return MapToDto(order);
}
```

#### 8.2.3 Bulk Import Files

**Use Case:** Import products from CSV/Excel files

**Implementation Pattern:**
```csharp
[HttpPost("import")]
public async Task<IActionResult> ImportProducts(IFormFile file)
{
    if (file.ContentType != "text/csv")
        return BadRequest("Only CSV files are supported");
    
    using var stream = file.OpenReadStream();
    using var reader = new StreamReader(stream);
    
    var products = new List<Product>();
    string? line;
    while ((line = await reader.ReadLineAsync()) != null)
    {
        var parts = line.Split(',');
        var product = new Product
        {
            Name = parts[0],
            SKU = parts[1],
            Price = decimal.Parse(parts[2]),
            // ... map other fields ...
        };
        products.Add(product);
    }
    
    _db.Products.AddRange(products);
    await _db.SaveChangesAsync();
    
    return Ok(new { importedCount = products.Count });
}
```

### 8.3 Data Flow for File Operations

**Typical Flow:**
1. Client uploads file via multipart/form-data
2. Controller receives `IFormFile`
3. Service validates file (type, size, content)
4. Service uploads to blob storage or saves locally
5. Service stores reference in database
6. Service returns URL or reference to client
7. Client can download file using returned URL

**Considerations:**
- Virus scanning for uploaded files
- File size limits
- Concurrent upload handling
- Storage quota management
- Cleanup of orphaned files

---

## 9. Data Flow Diagrams

### 9.1 Product Creation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ POST /api/v1/products
                           │ CreateProductRequest
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ ProductsController.Create()                                     │
│ - Validate ModelState                                           │
│ - Call ProductService.CreateAsync()                             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ ProductService.CreateAsync()                                    │
│ - Validate SKU uniqueness (AnyAsync)                            │
│ - Validate category exists (AnyAsync)                           │
│ - Create Product entity                                         │
│ - Add to DbSet                                                  │
│ - SaveChangesAsync()                                            │
│ - Load category relationship                                    │
│ - MapToDto()                                                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ InventoryDbContext                                              │
│ - Track entity changes                                          │
│ - Validate constraints                                          │
│ - Generate INSERT SQL                                           │
│ - Execute in in-memory database                                 │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ In-Memory Database                                              │
│ - Store Product record                                          │
│ - Update indexes (SKU unique index)                             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ ProductService (continued)                                      │
│ - Return ProductDto                                             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ ProductsController                                              │
│ - Return 201 Created                                            │
│ - Location header: /api/v1/products/{id}                        │
│ - Body: ProductDto                                              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
│ - Receive 201 with ProductDto                                   │
│ - Update local state                                            │
└─────────────────────────────────────────────────────────────────┘
```

### 9.2 Order Creation Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ POST /api/v1/orders
                           │ CreateOrderRequest (with line items)
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ OrdersController.Create()                                       │
│ - Validate ModelState                                           │
│ - Call OrderService.CreateAsync()                               │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ OrderService.CreateAsync()                                      │
│ 1. Validate line items exist                                    │
│ 2. Fetch all products (single query)                            │
│ 3. Validate all products exist and active                       │
│ 4. Validate stock availability for each line                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ OrderService (continued)                                        │
│ 5. Create Order entity                                          │
│ 6. For each line item:                                          │
│    - Deduct stock: product.UpdateStock(-quantity)              │
│    - Create OrderLine entity                                    │
│ 7. Calculate total: order.RecalculateTotal()                    │
│ 8. Add order to DbSet                                           │
│ 9. SaveChangesAsync() [ATOMIC]                                  │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ InventoryDbContext                                              │
│ - Track Order, OrderLines, Product changes                      │
│ - Generate INSERT for Order                                     │
│ - Generate INSERT for OrderLines                                │
│ - Generate UPDATE for Products (stock deduction)                │
│ - Execute all in transaction                                    │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ In-Memory Database                                              │
│ - Insert Order record                                           │
│ - Insert OrderLine records                                      │
│ - Update Product stock quantities                               │
│ - Update indexes (OrderNumber unique index)                     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ OrderService (continued)                                        │
│ - MapToDto()                                                    │
│ - Return OrderDto                                               │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ OrdersController                                                │
│ - Return 201 Created                                            │
│ - Location header: /api/v1/orders/{id}                          │
│ - Body: OrderDto (with calculated totals)                       │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
│ - Receive 201 with OrderDto                                     │
│ - Verify stock deductions in response                           │
│ - Update local inventory state                                  │
└─────────────────────────────────────────────────────────────────┘
```

### 9.3 Request Logging Flow

```
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
│ (Optional: X-Correlation-ID header)                             │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           │ HTTP Request
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ RequestLoggingMiddleware.InvokeAsync()                          │
│ - Extract/generate correlation ID                               │
│ - Add to response headers                                       │
│ - Start stopwatch                                               │
│ - Log: "[{CorrelationId}] {Method} {Path} started"              │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Next Middleware (CORS, Authorization, etc.)                     │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Controller & Service Processing                                 │
│ (Business logic execution)                                      │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ RequestLoggingMiddleware (finally block)                        │
│ - Stop stopwatch                                                │
│ - Determine log level (status code)                             │
│ - Log: "[{CorrelationId}] {Method} {Path} responded            │
│         {StatusCode} in {ElapsedMs}ms"                          │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Console Logger                                                  │
│ - Output structured log entry                                   │
└──────────────────────────┬──────────────────────────────────────┘
                           │
                           ▼
┌─────────────────────────────────────────────────────────────────┐
│ Client                                                          │
│ - Receive HTTP Response                                         │
│ - Receive X-Correlation-ID header                               │
└─────────────────────────────────────────────────────────────────┘
```

---

## 10. Data Consistency and Integrity

### 10.1 Transactional Guarantees

**ACID Properties Implemented:**

**Atomicity:**
- Single `SaveChangesAsync()` call ensures all-or-nothing semantics
- Example: Order creation with stock deductions (line 79 in `OrderService`)
- If any operation fails, entire transaction rolls back

**Consistency:**
- Database constraints enforced:
  - Unique indexes (SKU, OrderNumber)
  - Foreign key constraints with appropriate delete behaviors
  - Data validation in models (MaxLength, Range attributes)

**Isolation:**
- In-memory database provides implicit isolation
- Each request has scoped DbContext instance

**Durability:**
- In-memory database: Data persists for application lifetime
- On application restart, data is lost (seeded fresh)

### 10.2 Business Rule Enforcement

#### 10.2.1 Product Constraints

**SKU Uniqueness:**
- Enforced at database level (unique index)
- Validated before insert in `CreateAsync()` (line 86)

**Category Requirement:**
- Product must reference active category
- Validated in `CreateAsync()` (line 89-91)
- Validated in `UpdateAsync()` (line 128-130)

**Stock Non-Negativity:**
- Enforced in `Product.UpdateStock()` (line 57-62)
- Throws exception if adjustment would result in negative stock

#### 10.2.2 Order Constraints

**Line Items Required:**
- Order must have at least one line item
- Validated in `CreateAsync()` (line 43)

**Stock Availability:**
- All products must have sufficient stock
- Validated before any stock deductions (line 56-60)

**Stock Deduction Atomicity:**
- All stock deductions occur in single transaction
- Prevents partial order creation with partial stock deductions

#### 10.2.3 Category Constraints

**Name Uniqueness:**
- Enforced in `CreateAsync()` (line 38)

**No Orphaned Products:**
- Category deletion prevented if products exist
- Enforced in `DeleteAsync()` (line 80)

### 10.3 Data Validation

**Request Validation:**
- ModelState validation in controllers
- Data annotations on DTO records:
  - `[Required]` for mandatory fields
  - `[MaxLength]` for string fields
  - `[Range]` for numeric fields

**Business Logic Validation:**
- Service layer validates business rules
- Examples:
  - SKU uniqueness (ProductService)
  - Category existence (ProductService)
  - Stock availability (OrderService)
  - No orphaned products (CategoryService)

---

## 11. Performance Considerations

### 11.1 Query Optimization

**Eager Loading:**
- `.Include()` used to load related entities in single query
- Prevents N+1 query problem
- Example: `ProductService.GetAllAsync()` (line 20)

**Nested Eager Loading:**
- `.ThenInclude()` used for multi-level relationships
- Example: `OrderService.GetAllAsync()` (line 20-23)

**Filtered Queries:**
- Filtering applied at database level (LINQ to Entities)
- Only required data materialized to memory
- Example: `ProductService.SearchAsync()` (line 51-62)

**Existence Checks:**
- `AnyAsync()` used instead of `ToListAsync()` for boolean checks
- Efficient database query without data materialization
- Example: `ProductService.CreateAsync()` (line 86)

### 11.2 Potential Bottlenecks

**Search Performance:**
- Multi-field search with `Contains()` may be slow on large datasets
- Mitigation: Add full-text search index or implement caching

**Order Creation:**
- Multiple product fetches and stock updates
- Mitigation: Already optimized with single product query and atomic transaction

**Category Deletion:**
- Loads all products to check for orphans
- Mitigation: Use `AnyAsync()` instead of `Include().Any()`

### 11.3 Scalability Considerations

**In-Memory Database Limitations:**
- All data stored in process memory
- No persistence across restarts
- Single-process only (no distributed caching)
- Not suitable for production

**Recommended Improvements for Production:**
- Replace in-memory database with SQL Server/PostgreSQL
- Implement distributed caching (Redis)
- Add database connection pooling
- Implement pagination for large result sets
- Add query result caching

---

## 12. Seed Data Initialization

### 12.1 Seed Data Strategy

**Location:** `InventoryDbContext.SeedData()` in `src/InventoryApi/Data/InventoryDbContext.cs` (line 45-73)

**Initialization Timing:** `Program.cs` (line 43-48)
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
    InventoryDbContext.SeedData(db);
}
```

**Seed Data Schema:**

**Categories (5 total):**
1. Electronics - "electronic devices and accessories"
2. Clothing - "apparel and fashion items"
3. Food & Beverage - "food and drink products"
4. Books - "books, eBooks, and publications"
5. Sports - "sports and outdoor equipment"

**Products (10 total):**
- Electronics: Wireless Headphones Pro (ELEC-001), USB-C Cable (ELEC-002), Mechanical Keyboard (ELEC-003)
- Clothing: Classic White T-Shirt (CLTH-001), Running Shoes X500 (CLTH-002)
- Food & Beverage: Organic Green Tea (FOOD-001), Protein Powder (FOOD-002)
- Books: Clean Code (BOOK-001), Design Patterns GoF (BOOK-002)
- Sports: Yoga Mat Premium (SPRT-001)

**Seed Condition:** Only seeds if no categories exist (line 46)
```csharp
if (db.Categories.Any()) return;
```

---

## 13. Default Values and Field Initialization

### 13.1 Product Model Defaults

**Location:** `src/InventoryApi/Models/Product.cs`

**Default Values:**
- `Id`: Auto-generated GUID via `Guid.NewGuid()` (line 8)
- `IsActive`: `true` (line 33)
- `ReorderPoint`: `10` (line 31)
- `CreatedAt`: `DateTime.UtcNow` (line 48)
- `UpdatedAt`: `DateTime.UtcNow` (line 49)

### 13.2 Category Model Defaults

**Location:** `src/InventoryApi/Models/Category.cs`

**Default Values:**
- `Id`: Auto-generated GUID via `Guid.NewGuid()` (line 8)
- `IsActive`: `true` (line 16)
- `CreatedAt`: `DateTime.UtcNow` (line 17)
- `UpdatedAt`: `DateTime.UtcNow` (line 18)

### 13.3 Order Model Defaults

**Location:** `src/InventoryApi/Models/Order.cs`

**Default Values:**
- `Id`: Auto-generated GUID via `Guid.NewGuid()` (line 18)
- `Status`: `OrderStatus.Pending` (line 31)
- `CreatedAt`: `DateTime.UtcNow` (line 37)
- `UpdatedAt`: `DateTime.UtcNow` (line 38)

### 13.4 Category.IsActive Field Behavior

**Location:** `src/InventoryApi/Models/Category.cs` (line 16)

**Default Value:** `true`

**Impact on Product Visibility:**
- Products filtered by active category in `ProductService.GetByCategoryAsync()` (line 68)
- Products filtered by active category in `ProductService.CreateAsync()` validation (line 89-91)
- Products filtered by active category in `ProductService.UpdateAsync()` validation (line 128-130)
- Inactive categories prevent product creation/update with that category
- Inactive categories still return their products if directly queried

---

## 14. OrderStatus Enum and State Transitions

### 14.1 OrderStatus Enum Definition

**Location:** `src/InventoryApi/Models/Order.cs` (line 7-14)

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

**Valid Values:**
- `Pending` (0): Initial state
- `Confirmed` (1): Order confirmed, stock deducted
- `Processing` (2): Order being prepared
- `Shipped` (3): Order shipped to customer
- `Delivered` (4): Order delivered
- `Cancelled` (5): Order cancelled
- `Refunded` (6): Order refunded

### 14.2 Status Transitions

**Initial Status:** `OrderStatus.Confirmed` (set in `OrderService.CreateAsync()`, line 63)

**Valid Transitions:** No restrictions enforced
- Any status can transition to any other status
- No validation of logical state transitions
- Example: Can transition from `Delivered` to `Pending`

**Recommended Validation (Not Implemented):**
```csharp
private static bool IsValidTransition(OrderStatus from, OrderStatus to)
{
    return (from, to) switch
    {
        (OrderStatus.Pending, OrderStatus.Confirmed) => true,
        (OrderStatus.Confirmed, OrderStatus.Processing) => true,
        (OrderStatus.Processing, OrderStatus.Shipped) => true,
        (OrderStatus.Shipped, OrderStatus.Delivered) => true,
        (_, OrderStatus.Cancelled) => true,
        (_, OrderStatus.Refunded) => true,
        _ => false
    };
}
```

---

## 15. Summary of Data Flows

### 15.1 Key Data Flow Patterns

| Pattern | Location | Characteristics |
|---------|----------|-----------------|
| **Single Entity Read** | `GetByIdAsync()` | Eager load relationships, return null if not found |
| **Collection Read** | `GetAllAsync()` | Filter and order at DB level, materialize all results |
| **Search** | `SearchAsync()` | Multi-field case-insensitive search, filter active only |
| **Create** | `CreateAsync()` | Validate business rules, create entity, single SaveChanges |
| **Update** | `UpdateAsync()` | Fetch, validate, update properties, single SaveChanges |
| **Delete** | `DeleteAsync()` | Fetch, remove, single SaveChanges |
| **Complex Write** | `OrderService.CreateAsync()` | Multiple validations, multiple entity creates, atomic SaveChanges |

### 15.2 Data Transformation Pipeline

```
Request JSON
    ↓
DTO Record (CreateProductRequest, etc.)
    ↓
ModelState Validation
    ↓
Service Layer Processing
    ↓
Entity Creation/Update
    ↓
Database Persistence
    ↓
Entity-to-DTO Mapping
    ↓
Response JSON
```

### 15.3 Critical Data Flows

1. **Product Management:** CRUD operations with category validation and stock tracking
2. **Order Processing:** Multi-step validation, atomic stock deduction, total calculation
3. **Category Management:** CRUD with orphan prevention
4. **Request Logging:** Correlation ID tracking, performance monitoring
5. **Error Handling:** Business rule validation with appropriate HTTP responses

---

## 16. Testing Data Flows

### 16.1 Unit Test Coverage

**Location:** `tests/InventoryApi.Tests/ProductServiceTests.cs`

**Test Patterns:**

#### 16.1.1 Read Operation Tests

**GetAllAsync_ReturnsActiveProducts** (line 33-47):
- Arrange: Create active and inactive products
- Act: Call `GetAllAsync(activeOnly: true)`
- Assert: Only active product returned

**GetByIdAsync_ExistingProduct_ReturnsDto** (line 58-68):
- Arrange: Create product
- Act: Call `GetByIdAsync()`
- Assert: Correct DTO returned with all fields

#### 16.1.2 Write Operation Tests

**CreateAsync_ValidRequest_CreatesProduct** (line 79-92):
- Arrange: Prepare valid request
- Act: Call `CreateAsync()`
- Assert: Product created with correct fields

**CreateAsync_DuplicateSKU_ThrowsInvalidOperationException** (line 94-104):
- Arrange: Create product with SKU
- Act: Try to create another with same SKU
- Assert: Exception thrown with correct message

#### 16.1.3 Business Rule Tests

**AdjustStockAsync_ExceedsAvailableStock_ThrowsInvalidOperationException** (line 162-172):
- Arrange: Create product with low stock
- Act: Try to deduct more than available
- Assert: Exception thrown

**GetLowStockAsync_ReturnsOnlyLowStockProducts** (line 196-211):
- Arrange: Create low and high stock products
- Act: Call `GetLowStockAsync()`
- Assert: Only low stock products returned

### 16.2 Test Database Setup

**Location:** `ProductServiceTests` constructor (line 18-28)

```csharp
var options = new DbContextOptionsBuilder<InventoryDbContext>()
    .UseInMemoryDatabase(Guid.NewGuid().ToString())
    .Options;

_db = new InventoryDbContext(options);
_db.Database.EnsureCreated();
```

**Characteristics:**
- Unique database per test (GUID name)
- Isolated test execution
- No test data pollution

---

## 17. Conclusion

**demo-inventory-csharp** demonstrates a well-structured, layered architecture with clear data flow patterns:

1. **Request/Response:** Standardized JSON API with DTOs
2. **Data Processing:** Service layer with business logic validation
3. **Persistence:** EF Core with in-memory database and proper constraints
4. **Transactions:** Atomic operations with proper error handling
5. **Logging:** Structured logging with correlation IDs
6. **Testing:** Comprehensive unit tests with isolated test databases

**Key Strengths:**
- Clean separation of concerns (Controllers → Services → Data Access)
- Proper validation at multiple layers
- Atomic transactions for complex operations
- Comprehensive error handling
- Structured logging for observability

**Production Considerations:**
- Replace in-memory database with persistent storage
- Implement caching for frequently accessed data
- Add pagination for large result sets
- Implement authentication and authorization
- Add rate limiting and request throttling
- Monitor performance metrics

---

**Document Generated:** 2024
**Repository:** demo-inventory-csharp
**Framework:** ASP.NET Core 9
**Database:** Entity Framework Core (In-Memory)