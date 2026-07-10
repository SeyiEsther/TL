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
builder.Services.AddScoped<PersonListService>();
builder.Services.AddScoped<RecordDeleteService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<ShiftCompletionService>();
builder.Services.AddScoped<ShiftResumeService>();
builder.Services.AddScoped<HodEffectivenessService>();
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddRazorPages();

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
