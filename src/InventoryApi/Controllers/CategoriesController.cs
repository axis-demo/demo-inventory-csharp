using InventoryApi.DTOs;
using InventoryApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace InventoryApi.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(CategoryService categoryService, ILogger<CategoriesController> logger)
    {
        _categoryService = categoryService;
        _logger = logger;
    }

    /// <summary>Get all categories</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories);
    }

    /// <summary>Get a category by ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null)
            return NotFound(new { message = $"Category {id} not found." });

        return Ok(category);
    }

    /// <summary>Create a new category</summary>
    [HttpPost]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var category = await _categoryService.CreateAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Failed to create category: {Message}", ex.Message);
            return Conflict(new { message = ex.Message });
        }
    }

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
}
