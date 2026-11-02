using Microsoft.AspNetCore.Mvc;
using NetProject.DTOs;
using NetProject.Services;

namespace NetProject.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProductListItemDto>>> GetAll(CancellationToken ct)
    {
        var products = await _productService.GetAllProductsAsync(ct);
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductDetailDto>> Create(
        CreateProductMatrixDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _productService.CreateProductWithVariantsAsync(dto, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}

[ApiController]
[Route("api/categories")]
public sealed class CategoryController : ControllerBase
{
    private readonly IProductService _productService;

    public CategoryController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll(CancellationToken ct)
    {
        return Ok(await _productService.GetCategoriesAsync(ct));
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create(
        CreateCategoryDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _productService.CreateCategoryAsync(dto.CategoryName, dto.Description, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> Update(
        int id, CreateCategoryDto dto, CancellationToken ct)
    {
        try
        {
            var result = await _productService.UpdateCategoryAsync(id, dto.CategoryName, dto.Description, ct);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        try
        {
            await _productService.DeleteCategoryAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }
}
