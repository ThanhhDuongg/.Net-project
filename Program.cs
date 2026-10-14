using System;
using NetProject.DTOs;
using NetProject.Services;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký Services
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<BarcodeGeneratorService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Endpoint kiểm tra hoạt động hệ thống
app.MapGet("/", () => new
{
    Application = "Fashion Warehouse Management (.NET 10 - MySQL)",
    Status = "Running",
    CurrentTime = DateTime.Now
});

// Endpoint test nhanh thuật toán sinh SKU Matrix và Barcode
app.MapPost("/api/test-sku-matrix", (BarcodeGeneratorService service) =>
{
    var colors = new List<(int Id, string Name)> { (1, "WHT"), (2, "BLK") };
    var sizes = new List<(int Id, string Name)> { (1, "S"), (2, "M"), (3, "L") };
    
    var matrix = service.GenerateMatrix("POLO01", colors, sizes, 120000, 250000);
    return Results.Ok(matrix);
});

app.Run();
