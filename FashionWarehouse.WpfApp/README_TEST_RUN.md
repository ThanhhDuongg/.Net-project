# HƯỚNG DẪN TEST CHẠY THỬ GIAO DIỆN WPF

Project giao diện WPF đã được tạo đầy đủ mã nguồn tại: `FashionWarehouse.WpfApp/`.

## 1. Cách 1: Chạy bằng Visual Studio (Khuyên dùng và tiện nhất)
1. Mở file giải pháp `NetProject.slnx` (hoặc mở thư mục `FashionWarehouse.WpfApp`) bằng Visual Studio 2022/2026.
2. Trên thanh công cụ Solution Explorer, chuột phải vào project **`FashionWarehouse.WpfApp`** -> chọn **Set as Startup Project**.
3. Nhấn phím **F5** (hoặc nút **Start** màu xanh) để khởi động.
4. Cửa sổ ứng dụng sẽ xuất hiện:
   - Bạn có thể chuyển qua lại giữa các màn hình qua menu bên trái:
     - **Tổng quan (Dashboard):** Xem các thẻ KPI và bảng cảnh báo hàng hết.
     - **Quản lý Sản phẩm & SKU:** Chọn màu (Trắng/Đen/Navy) và kích cỡ (S/M/L/XL) rồi nhấn nút "Sinh biến thể" để thấy bảng ma trận SKU và mã vạch tự động cập nhật ngay trên giao diện.
     - **Nhập / Xuất Kho:** Gõ mã vạch vào ô hoặc nhấn "+ Thêm dòng" để thấy các dòng hàng và tổng tiền tự tính toán.

## 2. Cách 2: Chạy từ Terminal / CMD / PowerShell thông thường
Mở PowerShell tại máy tính của bạn và gõ:
```powershell
cd C:\Users\duong\source\repos\BTL_CKy\FashionWarehouse.WpfApp
dotnet restore
dotnet run
```
Cửa sổ phần mềm WPF sẽ hiển thị trực tiếp trên màn hình desktop.
