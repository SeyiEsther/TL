using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using TL.Services;

namespace TL.Filters;

/// <summary>
/// Applies the same portal role checks used by Razor Pages to API controllers.
/// </summary>
public class ApiPortalAccessFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var access = context.HttpContext.RequestServices.GetRequiredService<PortalAccessService>();
        await access.EnsureReadyAsync();

        var path = context.HttpContext.Request.Path.Value ?? "";
        if (!access.CanAccessApi(path))
        {
            context.Result = new JsonResult(new { error = "Access denied." })
            {
                StatusCode = StatusCodes.Status403Forbidden,
            };
            return;
        }

        await next();
    }
}
