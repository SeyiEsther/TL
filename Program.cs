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
var keyPath = builder.Configuration["DataProtection:KeyPath"];
if (string.IsNullOrWhiteSpace(keyPath))
    keyPath = Path.Combine(builder.Environment.ContentRootPath, "..", "TL-dataprotection-keys");
try
{
    Directory.CreateDirectory(keyPath);
    builder.Services.AddDataProtection()
        .SetApplicationName("TL-Portal")
        .PersistKeysToFileSystem(new DirectoryInfo(keyPath));
}
catch (Exception ex)
{
    // Fall back to default (in-memory/profile) keys rather than failing startup;
    // logged so the cause is visible if antiforgery problems recur.
    Console.Error.WriteLine($"Could not persist Data Protection keys to '{keyPath}': {ex.Message}");
}
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PortalAccessService>();
builder.Services.AddScoped<PersonListService>();
builder.Services.AddScoped<RecordDeleteService>();
builder.Services.AddScoped<HistoryListService>();
builder.Services.AddScoped<DocumentNumberService>();
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
    var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Database");
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

app.UseStaticFiles();
app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
