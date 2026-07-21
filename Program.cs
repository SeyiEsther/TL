using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<PortalAccessService>();
builder.Services.AddScoped<PersonListService>();
builder.Services.AddScoped<RecordDeleteService>();
builder.Services.AddScoped<HistoryListService>();
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
builder.Services.AddControllers().AddJsonOptions(o =>
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
}

app.UseStaticFiles();
app.MapControllers();
app.MapRazorPages();

app.Run();

public partial class Program { }
