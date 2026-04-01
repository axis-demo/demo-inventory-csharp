using InventoryApi.DTOs;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly ProductService _productService;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(ProductService productService, ILogger<ProductsController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>Get all active products</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] bool activeOnly = true)
    {
        var products = await _productService.GetAllAsync(activeOnly);
        return Ok(products);
    }

    /// <summary>Get a product by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            return NotFound(new { message = $"Product {id} not found." });

        return Ok(product);
    }

    /// <summary>Get a product by SKU</summary>
    [HttpGet("sku/{sku}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySku(string sku)
    {
        var product = await _productService.GetBySkuAsync(sku);
        if (product is null)
            return NotFound(new { message = $"Product with SKU '{sku}' not found." });

        return Ok(product);
    }

    /// <summary>Search products by name, SKU, description or brand</summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search([FromQuery] string q = "")
    {
        var results = await _productService.SearchAsync(q);
        return Ok(results);
    }

    /// <summary>Get products by category</summary>
    [HttpGet("category/{categoryId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetByCategory(Guid categoryId)
    {
        var products = await _productService.GetByCategoryAsync(categoryId);
        return Ok(products);
    }

    /// <summary>Get products with stock at or below reorder point</summary>
    [HttpGet("low-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLowStock()
    {
        var products = await _productService.GetLowStockAsync();
        return Ok(products);
    }

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

    /// <summary>Update a product</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var product = await _productService.UpdateAsync(id, request);
            if (product is null)
                return NotFound(new { message = $"Product {id} not found." });

            return Ok(product);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Delete a product</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _productService.DeleteAsync(id);
        if (!deleted)
            return NotFound(new { message = $"Product {id} not found." });

        return NoContent();
    }

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
}
