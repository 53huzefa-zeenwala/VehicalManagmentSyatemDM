using Microsoft.AspNetCore.Mvc;

namespace VehicleManagementDM.Api.Common.Errors;

/// <summary>MVC result that writes (and logs) an <see cref="ApiError"/>; used where MVC expects an <see cref="IActionResult"/>.</summary>
public sealed class ApiErrorResult(ApiError error) : IActionResult
{
    public ApiError Error { get; } = error;

    public Task ExecuteResultAsync(ActionContext context) =>
        context.HttpContext.RequestServices.GetRequiredService<ApiErrorWriter>().WriteAsync(context.HttpContext, Error);
}
