using InventoryApi.Data;
using InventoryApi.Middleware;
using InventoryApi.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Inventory Management API",
        Version = "v1",
        Description = "A demo ASP.NET Core inventory management REST API with product, category, and order management."
    });
});

// EF Core InMemory database
builder.Services.AddDbContext<InventoryDbContext>(options =>
    options.UseInMemoryDatabase("InventoryDb"));

// Register application services
builder.Services.AddScoped<ProductService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<CategoryService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();
    db.Database.EnsureCreated();
    InventoryDbContext.SeedData(db);
}

// Configure middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Inventory API v1"));
}

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseCors();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
