using ProjectApp.Application.Common.Security;
using ProjectApp.Application.Features.Categories;
using ProjectApp.Application.Features.Categories.Create;
using ProjectApp.Application.Features.Categories.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.WebAPI.Controllers;

// Okuma uçları herkese açık; yazma uçları yalnızca Admin rolüne açıktır.
// Gerçek projede ihtiyaca göre policy tabanlı yetkilendirmeye (AddAuthorization + [Authorize(Policy = "...")]) geçilebilir.
public class CategoriesController(ICategoryService categoryService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.GetAllListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/products")]
    public async Task<IActionResult> GetCategoryWithProducts(Guid id, CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.GetCategoryWithProductsAsync(id, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default) =>
        CreateActionResult(await categoryService.DeleteAsync(id, cancellationToken));
}
