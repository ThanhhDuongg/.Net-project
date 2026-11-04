using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using NetProject.DAL;
using NetProject.Data;
using NetProject.Hubs;
using NetProject.Services;
using NetProject.Services.Reporting;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:5116");

// Lấy ConnectionString DefaultConnection
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' chưa được cấu hình.");

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR();
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
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthorization();
app.MapControllers();
app.MapHub<BarcodeScannerHub>("/hub/barcode");

app.MapGet("/scan", (IWebHostEnvironment env) =>
{
    var htmlPath = Path.Combine(env.ContentRootPath, "wwwroot", "scan.html");
    return Results.File(htmlPath, "text/html");
});

app.MapGet("/api/system/scanner-info", () =>
{
    string localIp = "10.51.247.73";
    try
    {
        using var socket = new System.Net.Sockets.Socket(System.Net.Sockets.AddressFamily.InterNetwork, System.Net.Sockets.SocketType.Dgram, 0);
        socket.Connect("8.8.8.8", 65530);
        if (socket.LocalEndPoint is System.Net.IPEndPoint ep)
            localIp = ep.Address.ToString();
    }
    catch { }

    string? cloudflareUrl = null;
    try
    {
        if (File.Exists("tunnel_url.txt"))
            cloudflareUrl = File.ReadAllText("tunnel_url.txt").Trim();
    }
    catch { }

    return Results.Ok(new
    {
        localIp,
        localScanUrl = $"http://{localIp}:5116/scan",
        cloudflareScanUrl = string.IsNullOrWhiteSpace(cloudflareUrl) ? $"http://{localIp}:5116/scan" : cloudflareUrl
    });
});

app.MapGet("/", () => new
{
    Application = "Fashion Warehouse Management (.NET 10 - MySQL)",
    Status = "Running",
    CurrentTime = DateTime.UtcNow
});

// Đảm bảo bảng Customers tồn tại và nạp dữ liệu mẫu
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<WarehouseDbContext>();
        db.Database.ExecuteSqlRaw(@"
            CREATE TABLE IF NOT EXISTS Customers (
                CustomerId INT AUTO_INCREMENT PRIMARY KEY,
                CustomerCode VARCHAR(30) NOT NULL UNIQUE,
                CustomerName VARCHAR(150) NOT NULL,
                CustomerType VARCHAR(50) NOT NULL DEFAULT 'Đại lý cấp 1',
                PhoneNumber VARCHAR(20) NULL,
                Email VARCHAR(100) NULL,
                Address VARCHAR(255) NULL,
                DebtAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                IsActive TINYINT(1) NOT NULL DEFAULT 1,
                CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP
            ) ENGINE=InnoDB;

            INSERT IGNORE INTO Customers (CustomerCode, CustomerName, CustomerType, PhoneNumber, Email, Address) VALUES
            ('KH001', 'Chuỗi Thời Trang May 10', 'Đại lý cấp 1', '0988123456', 'may10@fashion.vn', 'Quận Hà Đông, Hà Nội'),
            ('KH002', 'Cửa Hàng Thời Trang Eva de Eva', 'Đại lý cấp 1', '0903987654', 'eva@fashion.vn', 'Quận 1, TP.HCM'),
            ('KH003', 'Shop Quần Áo Nam MenStyle', 'Khách mua sỉ', '0912345678', 'menstyle@gmail.com', 'Quận Cầu Giấy, Hà Nội'),
            ('KH004', 'Đại Lý Thời Trang Bắc Ninh', 'Đại lý cấp 2', '0977889911', 'bacninh.fashion@gmail.com', 'TP. Bắc Ninh, Bắc Ninh');
        ");
    }
    catch { }
}

app.Run();
