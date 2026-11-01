using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NetProject.DAL;
using NetProject.Data;
using NetProject.Services;
using NetProject.Services.Reporting;

var builder = WebApplication.CreateBuilder(args);

// Lấy ConnectionString DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' chưa được cấu hình.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();
builder.Services.AddSingleton<BarcodeGeneratorService>();

// Cấu hình Audit User
builder.Services.AddScoped<IAuditUserContext, SystemAuditUserContext>();

// Đăng ký DbContext với MySQL
builder.Services.AddDbContext<WarehouseDbContext>(options =>
    options.UseMySQL(connectionString));

// DAL + Unit of Work & Services
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IExcelReportService, ClosedXmlReportService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => new
{
    Application = "Fashion Warehouse Management (.NET 10 - MySQL)",
    Status = "Running",
    CurrentTime = DateTime.Now
});

app.MapPost("/api/test-sku-matrix", (BarcodeGeneratorService service) =>
{
    var colors = new List<(int Id, string Name)> { (1, "WHT"), (2, "BLK") };
    var sizes = new List<(int Id, string Name)> { (1, "S"), (2, "M"), (3, "L") };
    return Results.Ok(service.GenerateMatrix("POLO01", colors, sizes, 120000, 250000));
});

app.Run();
