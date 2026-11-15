using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Features.Categories;
using ProjectApp.Services.Features.Categories.Create;
using ProjectApp.Services.Features.Categories.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.API.Controllers;

// Okuma uçları herkese açık; yazma uçları yalnızca Admin rolüne açıktır.
// Gerçek projede ihtiyaca göre policy tabanlı yetkilendirmeye (AddAuthorization + [Authorize(Policy = "...")]) geçilebilir.
public class CategoriesController(ICategoryService categoryService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.GetAllListAsync(cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.GetByIdAsync(id, cancellationToken));

    [HttpGet("{id:guid}/products")]
    public async Task<IActionResult> GetCategoryWithProducts(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.GetCategoryWithProductsAsync(id, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateCategoryRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await categoryService.DeleteAsync(id, cancellationToken));
}
