using Microsoft.EntityFrameworkCore;
using StockRoom.Data;
using StockRoom.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("Default") ?? "Data Source=stockroom.db"));

builder.Services.AddScoped<StockService>();
builder.Services.AddScoped<ProductCsvService>();

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(
        new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "StockRoom API",
        Version = "v1",
        Description = "Small-business inventory: products, an append-only stock ledger, low-stock and " +
                      "valuation reports, barcode lookup, and CSV import/export keyed by SKU.",
    });
});

var app = builder.Build();

// Demo-friendly persistence: schema + a small electronics shop seeded on first run.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseSwagger();
app.UseSwaggerUI(o => o.SwaggerEndpoint("/swagger/v1/swagger.json", "StockRoom API v1"));

app.UseDefaultFiles(); // serves wwwroot/index.html — the stock dashboard
app.UseStaticFiles();

app.MapControllers();
app.MapGet("/api/health", () => Results.Ok(new { status = "ok", utc = DateTime.UtcNow }));

app.Run();
