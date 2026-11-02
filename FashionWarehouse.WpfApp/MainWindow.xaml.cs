using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FashionWarehouse.WpfApp.Services;
using FashionWarehouse.WpfApp.Views;

namespace FashionWarehouse.WpfApp;

public partial class MainWindow : Window
{
    private readonly DashboardView _dashboardView = new();
    private readonly ProductView _productView = new();
    private readonly CategoryView _categoryView = new();
    private readonly ReceiptView _receiptView = new();
    private readonly InventoryView _inventoryView = new();
    private readonly ReportsView _reportsView = new();
    private readonly EmployeeView _employeeView = new();

    public MainWindow()
    {
        InitializeComponent();
        LoadUserInfo();
        ShowDashboard();
    }

    private void LoadUserInfo()
    {
        if (UserSession.IsLoggedIn)
        {
            txtUserFullName.Text = UserSession.FullName;
            txtUserRole.Text = $"{UserSession.RoleName} ({UserSession.Username})";
        }
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "Bạn có chắc chắn muốn đăng xuất khỏi hệ thống?",
            "Xác nhận đăng xuất",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Yes)
        {
            UserSession.Logout();
            var loginWindow = new LoginWindow();
            loginWindow.Show();
            this.Close();
        }
    }

    private void BtnDashboard_Click(object sender, RoutedEventArgs e) => ShowDashboard();
    private void BtnProducts_Click(object sender, RoutedEventArgs e) => ShowProducts();
    private void BtnCategories_Click(object sender, RoutedEventArgs e) => ShowCategories();
    private void BtnReceipts_Click(object sender, RoutedEventArgs e) => ShowReceipts();
    private void BtnInventory_Click(object sender, RoutedEventArgs e) => ShowInventory();
    private void BtnReports_Click(object sender, RoutedEventArgs e) => ShowReports();
    private void BtnEmployees_Click(object sender, RoutedEventArgs e) => ShowEmployees();

    private void ShowDashboard()
    {
        MainContent.Content = _dashboardView;
        txtPageTitle.Text = "BẢNG ĐIỀU KHIỂN TRUNG TÂM";
        txtPageSubTitle.Text = "Tổng quan tồn kho và biến động nhập / xuất từ cơ sở dữ liệu";
        SetActiveButton(btnDashboard);
    }

    private void ShowProducts()
    {
        MainContent.Content = _productView;
        txtPageTitle.Text = "QUẢN LÝ SẢN PHẨM & BIẾN THỂ SKU";
        txtPageSubTitle.Text = "Tạo ma trận màu sắc × kích cỡ và mã vạch Barcode";
        SetActiveButton(btnProducts);
    }

    private void ShowCategories()
    {
        MainContent.Content = _categoryView;
        txtPageTitle.Text = "QUẢN LÝ DANH MỤC SẢN PHẨM";
        txtPageSubTitle.Text = "Thêm, sửa, xóa danh mục hàng hóa thời trang";
        SetActiveButton(btnCategories);
    }

    private void ShowReceipts()
    {
        MainContent.Content = _receiptView;
        txtPageTitle.Text = "NHẬP / XUẤT KHO BẰNG MÃ VẠCH";
        txtPageSubTitle.Text = "Quét Barcode, kiểm tra tồn kho và hoàn tất phiếu giao dịch";
        SetActiveButton(btnReceipts);
    }

    private void ShowInventory()
    {
        MainContent.Content = _inventoryView;
        txtPageTitle.Text = "TỒN KHO & VỊ TRÍ KỆ";
        txtPageSubTitle.Text = "Tra cứu tồn từng SKU, vị trí lưu trữ và cảnh báo dưới ngưỡng an toàn";
        SetActiveButton(btnInventory);
    }

    private void ShowReports()
    {
        MainContent.Content = _reportsView;
        txtPageTitle.Text = "BÁO CÁO & THỐNG KÊ";
        txtPageSubTitle.Text = "Tổng hợp nhập / xuất theo khoảng ngày và xuất sổ báo cáo Excel";
        SetActiveButton(btnReports);
    }

    private void ShowEmployees()
    {
        MainContent.Content = _employeeView;
        txtPageTitle.Text = "QUẢN LÝ NHÂN VIÊN & PHÂN QUYỀN";
        txtPageSubTitle.Text = "Danh sách tài khoản, phân bổ vai trò quản trị viên / thủ kho và thiết lập bảo mật";
        SetActiveButton(btnEmployees);
    }

    private void SetActiveButton(Button activeButton)
    {
        foreach (var button in new[] { btnDashboard, btnProducts, btnCategories, btnReceipts, btnInventory, btnReports, btnEmployees })
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
