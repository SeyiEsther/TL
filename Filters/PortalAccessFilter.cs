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
            // POST to a protected page (e.g. Audit Save) must not silently send the user
            // Home and wipe in-progress answers. Return 403 so the browser can keep the form.
            if (HttpMethods.IsPost(context.HttpContext.Request.Method)
                && IsProtectedWritePage(page))
            {
                context.Result = new ContentResult
                {
                    StatusCode = StatusCodes.Status403Forbidden,
                    ContentType = "application/json",
                    Content = """{"error":"Access denied or session expired — reopen the page from the menu and try again."}""",
                };
                return;
            }

            context.Result = new RedirectToPageResult("/Index");
            return;
        }

        await next();
    }

    static bool IsProtectedWritePage(string? pagePath) => pagePath is
        "/Audit" or "/AuditStart" or "/SeniorAudit" or "/SeniorStart" or "/Form"
        or "/Admin" or "/AdminTeamLeaders" or "/AdminHodNames" or "/AdminSeniorNames" or "/AdminFullAccess"
        or "/History" or "/ShiftHistory" or "/HodHistory" or "/SeniorHistory";

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) =>
        Task.CompletedTask;
}
