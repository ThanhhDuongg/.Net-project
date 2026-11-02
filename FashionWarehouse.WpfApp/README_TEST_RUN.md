# HƯỚNG DẪN TEST CHẠY THỬ GIAO DIỆN WPF

Project giao diện WPF đã được thiết kế hoàn thiện tại: `FashionWarehouse.WpfApp/`.

## 1. Cách 1: Khởi chạy bằng PowerShell / Terminal (Tiện nhất)
Mở 2 cửa sổ PowerShell tại thư mục gốc của dự án:

**Cửa sổ 1: Khởi động Backend Web API**
```powershell
dotnet run --project NetProject.csproj
```

**Cửa sổ 2: Khởi động Ứng dụng Desktop WPF**
```powershell
dotnet run --project FashionWarehouse.WpfApp\FashionWarehouse.WpfApp.csproj
```

---

## 2. Cách 2: Chạy bằng Visual Studio
1. Mở file giải pháp `NetProject.slnx` bằng Visual Studio 2022/2026.
2. Trên Solution Explorer, chuột phải vào project **`FashionWarehouse.WpfApp`** -> chọn **Set as Startup Project**.
3. Nhấn phím **F5** (hoặc nút **Start** màu xanh) để khởi động.
4. Cửa sổ ứng dụng sẽ xuất hiện với đầy đủ 7 phân hệ hoàn chỉnh:
   - **Tổng quan (Dashboard):** Xem các thẻ KPI tài chính, tồn kho và biểu đồ luân chuyển hàng hóa.
   - **Quản lý Sản phẩm & SKU:** Chọn màu sắc $\times$ kích cỡ để sinh ma trận biến thể SKU và in mã vạch Barcode.
   - **Quản lý Danh mục:** Thêm, sửa danh mục quần áo thời trang.
   - **Nhập / Xuất Kho:** Quét súng Barcode, kiểm tra tồn kho và xác nhận phiếu giao dịch.
   - **Tồn kho & Vị trí kệ:** Tra cứu tồn kho thời gian thực theo từng vị trí kệ A/B/C/D và xuất file kiểm kê Excel.
   - **Báo cáo & Thống kê:** Tra cứu sổ thẻ kho chi tiết, dự báo nhập hàng tự động và xuất sổ báo cáo Excel.
   - **Nhân viên & Phân quyền:** Quản lý tài khoản, đặt lại mật khẩu, phân bổ vai trò Admin/Thủ kho và xem bảng ma trận phân quyền RBAC.
