using Microsoft.EntityFrameworkCore;
using TL.Data;
using TL.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<ShiftCompletionService>();
builder.Services.AddScoped<HodEffectivenessService>();
builder.Services.AddControllers().AddJsonOptions(o =>
    o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles);
builder.Services.AddRazorPages();

var app = builder.Build();

app.UseStaticFiles();
app.MapControllers();
app.MapRazorPages();

app.Run();
