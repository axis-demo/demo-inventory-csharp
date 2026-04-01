# demo-inventory-csharp

A demo ASP.NET Core 9 inventory management REST API with full CRUD for products, categories, and orders.

## Features

- **Products**: Full CRUD, SKU-based lookup, full-text search, category filter, low-stock alerts, stock adjustments
- **Categories**: Full CRUD with product count, slug generation
- **Orders**: Order placement with automatic stock deduction, status lifecycle management
- **Swagger UI**: Interactive API docs at `/swagger`
- **EF Core InMemory**: Zero-config persistence, seeded with demo data on startup
- **Request Logging Middleware**: Correlation IDs, method/path/status/duration on every request

## Quick Start

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)

### Run the API

```bash
cd src/InventoryApi
dotnet run
```

The API will start at `https://localhost:5001` / `http://localhost:5000`.
Swagger UI: `http://localhost:5000/swagger`

### Run Tests

```bash
dotnet test
```

## API Reference

### Products `GET /api/v1/products`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/products` | List all active products |
| GET | `/api/v1/products?activeOnly=false` | List all products including inactive |
| GET | `/api/v1/products/{id}` | Get product by ID |
| GET | `/api/v1/products/sku/{sku}` | Get product by SKU |
| GET | `/api/v1/products/search?q={query}` | Search by name, SKU, description, brand |
| GET | `/api/v1/products/category/{categoryId}` | Get products by category |
| GET | `/api/v1/products/low-stock` | Get products at or below reorder point |
| POST | `/api/v1/products` | Create a new product |
| PUT | `/api/v1/products/{id}` | Update a product |
| DELETE | `/api/v1/products/{id}` | Delete a product |
| POST | `/api/v1/products/{id}/stock` | Adjust stock quantity |

### Categories `GET /api/v1/categories`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/categories` | List all categories |
| GET | `/api/v1/categories/{id}` | Get category by ID |
| POST | `/api/v1/categories` | Create a category |
| PUT | `/api/v1/categories/{id}` | Update a category |
| DELETE | `/api/v1/categories/{id}` | Delete a category (must be empty) |

### Orders `GET /api/v1/orders`

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/v1/orders` | List all orders |
| GET | `/api/v1/orders/{id}` | Get order by ID |
| POST | `/api/v1/orders` | Place a new order |
| PATCH | `/api/v1/orders/{id}/status` | Update order status |

#### Order Status Values
`Pending`, `Confirmed`, `Processing`, `Shipped`, `Delivered`, `Cancelled`, `Refunded`

## Example Requests

### Create a Product

```bash
curl -X POST http://localhost:5000/api/v1/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Example Widget",
    "sku": "WDGT-9999",
    "description": "A sample product",
    "price": 29.99,
    "cost": 12.00,
    "stockQuantity": 100,
    "reorderPoint": 20,
    "categoryId": "<category-id-from-GET-categories>"
  }'
```

### Place an Order

```bash
curl -X POST http://localhost:5000/api/v1/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerName": "Jane Smith",
    "customerEmail": "jane@example.com",
    "lines": [
      { "productId": "<product-id>", "quantity": 2 }
    ]
  }'
```

### Adjust Stock

```bash
curl -X POST http://localhost:5000/api/v1/products/{id}/stock \
  -H "Content-Type: application/json" \
  -d '{ "quantity": 50, "reason": "Restock from supplier" }'
```

## Project Structure

```
src/
  InventoryApi/
    Controllers/    # API controllers (Products, Categories, Orders)
    Data/           # EF Core DbContext + seed data
    DTOs/           # Request/response records
    Middleware/     # RequestLoggingMiddleware
    Models/         # Domain entities (Product, Category, Order, OrderLine)
    Services/       # Business logic (ProductService, OrderService, CategoryService)
tests/
  InventoryApi.Tests/
    ProductServiceTests.cs  # xUnit tests with FluentAssertions
data/
  products-catalog.json     # 20,000 sample products (padding / demo data)
  transactions-history.json # 10,000 transaction records
  suppliers.json            # 500 supplier records
```

## License

MIT
