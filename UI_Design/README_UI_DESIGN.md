# TÀI LIỆU THIẾT KẾ GIAO DIỆN (UI/UX) - HỆ THỐNG QUẢN LÝ KHO QUẦN ÁO

## 1. Phong cách thiết kế (Design System)
- **Framework:** WPF (Windows Presentation Foundation) trên nền tảng .NET 10.
- **UI Kit:** Material Design in XAML Toolkit kết hợp phong cách Tailwind UI Dashboard.
- **Bảng màu chủ đạo:**
  - `Primary Navy/Dark:` `#1E293B` (Sidebar điều hướng, tạo cảm giác chuyên nghiệp cho phần mềm kho).
  - `Accent Blue:` `#2563EB` (Các nút hành động, liên kết chính).
  - `Success Green:` `#10B981` (Thao tác Lưu kho, Nhập kho thành công, Xuất Excel).
  - `Warning Yellow:` `#F59E0B` (Chế độ quét súng Barcode Scanner, cảnh báo).
  - `Danger Red:` `#EF4444` (Xuất kho, Báo động hết hàng Low Stock, Xóa dòng).
  - `Background:` `#F4F6F9` (Màu nền xám dịu mắt khi thủ kho làm việc liên tục nhiều giờ).

## 2. Các màn hình đã thiết kế và lưu tại thư mục `UI_Design/`:
1. `MainWindow.xaml`: Cấu trúc Dashboard khung sườn với Sidebar đa chức năng, thanh Topbar và User Info.
2. `ProductView.xaml` (Phần bạn A):
   - Bên trái: Form tạo mẫu áo cha và tổ hợp màu sắc (CheckBox) $\times$ kích cỡ (CheckBox).
   - Bên phải: Bảng ma trận biến thể tự sinh ra, hiển thị trực quan mã vạch Barcode và nút in tem mác.
3. `ReceiptView.xaml` (Phần bạn B):
   - Phía trên: Khung nhận diện quét súng Barcode tốc độ cao.
   - Phía dưới: Bảng dòng hàng hóa chi tiết và Panel tổng tiền / xác nhận giao dịch ghi kho an toàn.
4. `DashboardView.xaml` (Phần bạn B):
   - 4 Thẻ KPI: Tổng SKU, Tồn kho thực tế, Cảnh báo hết hàng, Tổng giá trị vốn kho.
   - Bảng cảnh báo hàng dưới ngưỡng an toàn và nút xuất file báo cáo Excel (`ClosedXML`).
