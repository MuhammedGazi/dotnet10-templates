using FluentValidation;
using ProjectApp.Application.Common.Wrappers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ProjectApp.WebAPI.Filters;

/// <summary>
/// Action argümanlarını kayıtlı <see cref="IValidator{T}"/> sınıflarıyla doğrular
/// (FluentValidation.AspNetCore olmadan) ve hataları <see cref="ServiceResult"/> formatında döner.
/// </summary>
public class FluentValidationFilter(IServiceProvider serviceProvider) : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var errors = new List<string>();

        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
            {
                continue;
            }

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (serviceProvider.GetService(validatorType) is not IValidator validator)
            {
                continue;
            }

            var result = await validator.ValidateAsync(
                new ValidationContext<object>(argument),
                context.HttpContext.RequestAborted);

            errors.AddRange(result.Errors.Select(e => e.ErrorMessage));
        }

        // Model bağlama hataları (bozuk JSON, yanlış tip, eksik gövde) iç tip adlarını içerebildiği için
        // istemciye genel bir mesaj döner; ayrıntı yalnızca sunucu tarafında kalır.
        if (!context.ModelState.IsValid)
        {
            errors.Add("İstek gövdesi veya parametreleri geçersiz. Alan adlarını ve veri tiplerini kontrol edin.");
        }

        if (errors.Count > 0)
        {
            context.Result = new BadRequestObjectResult(ServiceResult.Fail(errors.Distinct().ToList()));
            return;
        }

        await next();
    }
}
