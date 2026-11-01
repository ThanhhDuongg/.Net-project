# Module Giao dịch Kho & Báo cáo

## Thành phần đã triển khai

- **DAL / Database**
  - Entity `Inventory` có `RowVersion` để kiểm soát đồng thời lạc quan.
  - Entity `InventoryTransaction` là nhật ký biến động tồn kho bất biến.
  - `WarehouseDbContext` thiết lập quan hệ, unique index, concurrency token và audit shadow properties: `CreatedAt`, `CreatedBy`, `UpdatedAt`, `UpdatedBy`.
  - `IUnitOfWork` và `EfUnitOfWork` điều phối toàn bộ transaction; BLL không gọi trực tiếp `DbContext`.
  - Migration SQL: `Database/migration_inventory_transactions.sql`.

- **BLL**
  - `InventoryService.ProcessAsync` gom các dòng trùng Variant/Vị trí, kiểm tra số lượng, kiểm tra `ExpectedRowVersion`, tạo `Receipt` + `ReceiptDetail` + `InventoryTransaction` và commit trong **một database transaction**.
  - Khi thiếu tồn, xung đột phiên bản hoặc `SaveChanges` lỗi: `RollbackAsync` được gọi và trạng thái aggregate in-memory được phục hồi về số lượng trước thay đổi.
  - API trả HTTP `409 Conflict` khi có xung đột optimistic concurrency hoặc số lượng không đủ.

- **Barcode, dashboard và báo cáo**
  - `GET /api/inventory/barcode/{barcode}` trả cả `LocationId` và `RowVersion`; màn hình WPF gửi token này lại khi lưu, tránh ghi đè dữ liệu đã cũ.
  - Màn hình Receipt quét barcode bằng Enter/nút thêm, không đưa mã không hợp lệ vào phiếu.
  - Dashboard gọi `GET /api/dashboard/summary`, `GET /api/dashboard/low-stock` để hiển thị KPI, biểu đồ nhập/xuất, cảnh báo tồn thấp.
  - `GET /api/reports/inventory.xlsx` dùng ClosedXML xuất `.xlsx` theo batch 5.000 dòng, có tiêu đề, định dạng tiền, filter và hàng tổng.

- **Unit test**
  - `tests/BTL_CKy.UnitTests/InventoryServiceTests.cs` dùng **Moq** để kiểm tra thiếu tồn, lỗi lưu, conflict concurrency, nhập/xuất thành công và xác nhận lỗi không làm trừ tồn sai.

## Cài đặt cơ sở dữ liệu

1. Chạy `Database/schema_mysql.sql`.
2. Chạy `Database/migration_inventory_transactions.sql`.
3. Cập nhật chuỗi `ConnectionStrings:MySqlConnection` trong `appsettings.json` bằng mật khẩu MySQL thực tế.

## Chạy kiểm thử

```powershell
dotnet build .\NetProject.slnx --no-restore
dotnet test .\tests\BTL_CKy.UnitTests\BTL_CKy.UnitTests.csproj --no-build
```

## Chạy API và WPF

Mở hai terminal:

```powershell
# Terminal 1 - API
dotnet run --project .\NetProject.csproj

# Terminal 2 - WPF
dotnet run --project .\FashionWarehouse.WpfApp\FashionWarehouse.WpfApp.csproj
```

WPF mặc định gọi API tại `http://localhost:5116/` (theo `Properties/launchSettings.json`).