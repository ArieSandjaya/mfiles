using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MFilesClone.Server.Services;
using MFilesClone.Shared;

namespace MFilesClone.Server.Controllers;

[Authorize]
[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly CategoryService _categoryService;

    public CategoriesController(CategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(categories.Select(c => new CategoryDto { Id = c.Id, Name = c.Name, ParentCategoryId = c.ParentCategoryId }).ToList());
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request)
    {
        var category = await _categoryService.CreateAsync(request.Name);
        var dto = new CategoryDto { Id = category.Id, Name = category.Name, ParentCategoryId = category.ParentCategoryId };
        return CreatedAtAction(nameof(GetAll), dto);
    }
}
