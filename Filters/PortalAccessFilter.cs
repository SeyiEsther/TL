using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace TL.Filters;

public class PortalAccessFilter : IAsyncPageFilter
{
    public async Task OnPageHandlerExecutionAsync(
        PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        var page = context.RouteData.Values["page"]?.ToString();
        var access = context.HttpContext.RequestServices.GetRequiredService<TL.Services.PortalAccessService>();
        await access.EnsureReadyAsync();

        if (!access.CanAccessPage(page))
        {
            // Short-circuit: setting Result and then calling next() throws in .NET 8,
            // so return here to redirect unauthorised users cleanly to Home.
            context.Result = new RedirectToPageResult("/Index");
            return;
        }

        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) =>
        Task.CompletedTask;
}
