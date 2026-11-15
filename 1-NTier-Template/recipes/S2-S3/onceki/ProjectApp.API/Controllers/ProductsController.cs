using ProjectApp.Repositories.Identity;
using ProjectApp.Services.Common.Pagination;
using ProjectApp.Services.Features.Products;
using ProjectApp.Services.Features.Products.Create;
using ProjectApp.Services.Features.Products.Update;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.API.Controllers;

// Okuma uçları herkese açık; yazma uçları yalnızca Admin rolüne açıktır.
// Gerçek projede ihtiyaca göre policy tabanlı yetkilendirmeye (AddAuthorization + [Authorize(Policy = "...")]) geçilebilir.
public class ProductsController(IProductService productService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken) =>
        CreateActionResult(await productService.GetAllListAsync(cancellationToken));

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await productService.GetPagedAllListAsync(request, cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await productService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await productService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateProductRequest request, CancellationToken cancellationToken) =>
        CreateActionResult(await productService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken) =>
        CreateActionResult(await productService.DeleteAsync(id, cancellationToken));
}
