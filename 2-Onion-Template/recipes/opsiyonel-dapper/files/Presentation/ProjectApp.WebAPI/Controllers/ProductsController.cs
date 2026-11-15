using ProjectApp.Application.Common.Security;
using ProjectApp.Application.Common.Parameters;
using ProjectApp.Application.Features.Products;
using ProjectApp.Application.Features.Products.Create;
using ProjectApp.Application.Features.Products.Update;
using ProjectApp.Application.Features.Products.UpdateStock;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ProjectApp.WebAPI.Controllers;

// Okuma uçları herkese açık; yazma uçları yalnızca Admin rolüne açıktır.
// Gerçek projede ihtiyaca göre policy tabanlı yetkilendirmeye (AddAuthorization + [Authorize(Policy = "...")]) geçilebilir.
public class ProductsController(IProductService productService) : CustomBaseController
{
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.GetAllListAsync(cancellationToken));

    [HttpGet("paged")]
    public async Task<IActionResult> GetPaged([FromQuery] PagedRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.GetPagedAllListAsync(request, cancellationToken));

    [HttpGet("top-priced")]
    public async Task<IActionResult> GetTopPriced([FromQuery] int count = 5, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.GetTopPricedProductsWithDapperAsync(Math.Clamp(count, 1, 50), cancellationToken));

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.GetByIdAsync(id, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.CreateAsync(request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.UpdateAsync(id, request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPatch("stock")]
    public async Task<IActionResult> UpdateStock([FromBody] UpdateProductStockRequest request, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.UpdateStockAsync(request, cancellationToken));

    [Authorize(Roles = RoleNames.Admin)]
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken = default) =>
        CreateActionResult(await productService.DeleteAsync(id, cancellationToken));
}
