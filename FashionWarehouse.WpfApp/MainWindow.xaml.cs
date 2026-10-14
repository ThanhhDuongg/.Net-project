using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FashionWarehouse.WpfApp.Views;

namespace FashionWarehouse.WpfApp;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboardView = new();
    private readonly ProductView _productView = new();
    private readonly ReceiptView _receiptView = new();

    public MainWindow()
    {
        InitializeComponent();
        ShowDashboard();
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void BtnProducts_Click(object sender, RoutedEventArgs e) => ShowProducts();
    private void BtnReceipts_Click(object sender, RoutedEventArgs e) => ShowReceipts();

    private void BtnComingSoon_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Màn hình này đã có bản thiết kế tại thư mục UI_Design. Nó sẽ được triển khai ở bước tiếp theo.",
            "Chức năng đang phát triển",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private void ShowDashboard()
    {
        MainContent.Content = _dashboardView;
        txtPageTitle.Text = "BẢNG ĐIỀU KHIỂN TRUNG TÂM";
        txtPageSubTitle.Text = "Dữ liệu minh họa giao diện quản lý kho thời trang";
        SetActiveButton(btnDashboard);
    }

    private void ShowProducts()
    {
        MainContent.Content = _productView;
        txtPageTitle.Text = "QUẢN LÝ SẢN PHẨM & BIẾN THỂ SKU";
        txtPageSubTitle.Text = "Tạo ma trận màu sắc × kích cỡ và mã vạch Barcode";
        SetActiveButton(btnProducts);
    }

    private void ShowReceipts()
    {
        MainContent.Content = _receiptView;
        txtPageTitle.Text = "NHẬP / XUẤT KHO BẰNG MÃ VẠCH";
        txtPageSubTitle.Text = "Quét Barcode, kiểm tra tồn kho và hoàn tất phiếu giao dịch";
        SetActiveButton(btnReceipts);
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var button in new[] { btnDashboard, btnProducts, btnReceipts })
        {
            button.Background = button == activeButton
                ? new SolidColorBrush(Color.FromRgb(37, 99, 235))
                : Brushes.Transparent;
            button.Foreground = button == activeButton
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(203, 213, 225));
        }
    }
}
