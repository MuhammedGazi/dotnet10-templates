using System.Net;
using ProjectApp.Repositories;
using ProjectApp.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectApp.API.Filters;

/// <summary>
/// [Hazır örnek] Action'daki <c>id</c> parametresine ait kayıt yoksa action'a girmeden 404 döner.
/// Kullanım (DI'a kayıtlıdır): <c>[ServiceFilter(typeof(NotFoundFilter&lt;Product, Guid&gt;))]</c>
/// Not: Servisler zaten 404 döndüğü için bu filtre bir uca eklenirse aynı kayıt için veritabanına
/// iki kez gidilir; servisin kaydı okumadığı uçlarda (ör. yalnızca id alan işlemler) tercih edin.
/// </summary>
public class NotFoundFilter<T, TId>(IGenericRepository<T, TId> genericRepository)
    : IAsyncActionFilter where T : class where TId : struct
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.ActionArguments.TryGetValue("id", out var idAsObject) || idAsObject is not TId id)
        {
            await next();
            return;
        }

        if (await genericRepository.AnyAsync(id, context.HttpContext.RequestAborted))
        {
            await next();
            return;
        }

        var result = ServiceResult.Fail("Kayıt bulunamadı.", HttpStatusCode.NotFound);
        context.Result = new NotFoundObjectResult(result);
    }
}
