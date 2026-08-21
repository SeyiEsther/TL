using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// Persist Data Protection keys outside the app folder so an app-pool recycle
// or a redeploy (which overwrites the binaries and restarts the app) can't
// regenerate the keys. Without this, every restart invalidates in-flight
// antiforgery tokens, so a form open before the restart fails to POST with a
// 400 — e.g. a HoD audit that silently won't save. The default path is a
// sibling of the content root (so publish never wipes it); override with the
// DataProtection:KeyPath setting if the app account can't write there.
// Try several candidate folders and pick the first one we can actually WRITE to
// (create + write + delete a probe file). A folder that exists but isn't writable
// by the app-pool account was silently failing before — which is exactly what
// makes a deploy/recycle invalidate open forms.
static string? ResolveWritableKeyDir(params string?[] candidates)
{
    foreach (var c in candidates)
    {
        if (string.IsNullOrWhiteSpace(c)) continue;
        try
        {
            Directory.CreateDirectory(c);
            var probe = Path.Combine(c, ".writetest");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return c;
        }
        catch { /* try the next candidate */ }
    }
    return null;
}

var keyDir = ResolveWritableKeyDir(
    builder.Configuration["DataProtection:KeyPath"],
    Path.Combine(builder.Environment.ContentRootPath, "..", "TL-dataprotection-keys"),
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "TL-Portal", "dataprotection-keys"));

var keysPersisted = keyDir != null;
if (keysPersisted)
{
    builder.Services.AddDataProtection()
        .SetApplicationName("TL-Portal")
        .PersistKeysToFileSystem(new DirectoryInfo(keyDir!));
}
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PortalAccessService>();
builder.Services.AddScoped<PersonListService>();
builder.Services.AddScoped<RecordDeleteService>();
builder.Services.AddScoped<HistoryListService>();
builder.Services.AddScoped<DocumentNumberService>();
builder.Services.AddScoped<ActionService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<ShiftCompletionService>();
builder.Services.AddScoped<ShiftResumeService>();
builder.Services.AddScoped<HodEffectivenessService>();
builder.Services.AddScoped<TlShiftComplianceService>();
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.ValueCountLimit = 20_000;
    options.ValueLengthLimit = 1024 * 1024;
    options.MultipartBodyLengthLimit = 32 * 1024 * 1024;
});
builder.Services.AddControllers(options =>
{
    options.Filters.Add<TL.Filters.ApiPortalAccessFilter>();
}).AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.MaxAge = TimeSpan.FromHours(12);
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
});
builder.Services.AddRazorPages(options =>
    options.Conventions.AddFolderApplicationModelConvention("/", model =>
        model.Filters.Add(new TL.Filters.PortalAccessFilter())));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

    // Make the Data Protection state obvious in the logs — this is what decides
    // whether a deploy/recycle silently logs everyone out of open forms.
    if (keysPersisted)
        logger.LogInformation("Data Protection keys persisted to {Path} — antiforgery tokens survive restarts and deploys.", keyDir);
    else
        logger.LogError("Data Protection keys are NOT being persisted (no writable folder found). " +
            "Every app restart/deploy will invalidate open forms — users get HTTP 400 on save. " +
            "Set DataProtection:KeyPath in appsettings to a folder the app-pool account can write to.");

    if (db.Database.IsRelational())
    {
        try
        {
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed — run dotnet ef database update on the server.");
            throw;
        }
    }

    var people = scope.ServiceProvider.GetRequiredService<PersonListService>();
    await people.EnsureLoadedAsync();

    var docs = scope.ServiceProvider.GetRequiredService<DocumentNumberService>();
    await docs.EnsureSeededAsync();
}

// Turn the raw "HTTP 400" that an expired antiforgery token produces into a
// friendly page. An antiforgery 400 has an empty body, so StatusCodePages fires;
// genuine JSON 400s (which carry a body) are left untouched. Only page POSTs that
// expect HTML get the friendly screen.
app.UseStatusCodePages(async ctx =>
{
    var res = ctx.HttpContext.Response;
    var req = ctx.HttpContext.Request;
    var wantsHtml = req.Headers.Accept.Any(h => h != null && h.Contains("text/html", StringComparison.OrdinalIgnoreCase));
    if (res.StatusCode == 400
        && HttpMethods.IsPost(req.Method)
        && wantsHtml
        && !req.Path.StartsWithSegments("/api"))
    {
        res.ContentType = "text/html; charset=utf-8";
        await res.WriteAsync(StartupHtml.SessionRefreshedHtml);
    }
});

app.UseStaticFiles();
app.MapControllers();
app.MapRazorPages();

app.Run();

// Friendly "your session refreshed" screen shown instead of a raw 400.
static class StartupHtml
{
    public const string SessionRefreshedHtml = """
<!doctype html><html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Session refreshed</title>
<style>
  body{font-family:'Segoe UI',system-ui,sans-serif;background:#eef0f3;margin:0;
       display:flex;min-height:100vh;align-items:center;justify-content:center;color:#1a1a1a}
  .card{background:#fff;max-width:460px;margin:20px;padding:28px 30px;border-radius:14px;
        box-shadow:0 6px 24px rgba(0,0,0,.12);border-top:5px solid #CC1F2C}
  h1{font-size:20px;margin:0 0 8px}
  p{font-size:14px;line-height:1.55;color:#374151;margin:0 0 12px}
  .btn{display:inline-block;background:#CC1F2C;color:#fff;border:0;border-radius:9px;
       padding:10px 18px;font-size:14px;font-weight:700;cursor:pointer;text-decoration:none}
  .muted{font-size:12px;color:#6b7280;margin-top:14px}
</style></head><body>
  <div class="card">
    <h1>Your session refreshed</h1>
    <p>The app updated while this form was open, so this save was declined for security.
       <strong>Nothing was lost.</strong></p>
    <p>Reload the page and submit again. Anything you typed is saved in this browser
       and should reappear when the page loads.</p>
    <button class="btn" onclick="history.back()">Go back &amp; retry</button>
    <div class="muted">If it keeps happening, close the tab, reopen the form from the menu, and try once more.</div>
  </div>
</body></html>
""";
}

public partial class Program { }
