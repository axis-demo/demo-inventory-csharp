using InventoryApi.DTOs;
using InventoryApi.Models;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly OrderService _orderService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(OrderService orderService, ILogger<OrdersController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    /// <summary>Get all orders</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<OrderDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var orders = await _orderService.GetAllAsync();
        return Ok(orders);
    }

    /// <summary>Get an order by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
            return NotFound(new { message = $"Order {id} not found." });

        return Ok(order);
    }

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

    /// <summary>Update order status</summary>
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
}

public record UpdateOrderStatusRequest([System.ComponentModel.DataAnnotations.Required] string Status);
