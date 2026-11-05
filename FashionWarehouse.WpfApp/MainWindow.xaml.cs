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

    private readonly SupplierView _supplierView = new();
    private readonly CustomerView _customerView = new();
    private readonly WarehouseLocationView _warehouseLocationView = new();

    private readonly ReceiptHistoryView _receiptHistoryView = new();



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

    private void BtnSuppliers_Click(object sender, RoutedEventArgs e) => ShowSuppliers();
    private void BtnCustomers_Click(object sender, RoutedEventArgs e) => ShowCustomers();

    private void BtnTransactionGroup_Click(object sender, RoutedEventArgs e)

    {

        bool isVisible = pnlTransactionSubmenu.Visibility == Visibility.Visible;

        pnlTransactionSubmenu.Visibility = isVisible ? Visibility.Collapsed : Visibility.Visible;

        txtTransChevron.Text = isVisible ? "▶" : "▼";

    }

    private void BtnImport_Click(object sender, RoutedEventArgs e) => ShowImport();

    private void BtnExport_Click(object sender, RoutedEventArgs e) => ShowExport();

    private void BtnReceiptHistory_Click(object sender, RoutedEventArgs e) => ShowReceiptHistory();

    private void BtnReceipts_Click(object sender, RoutedEventArgs e) => ShowImport();

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



    private void ShowCustomers()
    {
        _ = _customerView.LoadCustomersAsync();
        MainContent.Content = _customerView;
        txtPageTitle.Text = "QUẢN LÝ KHÁCH HÀNG & ĐẠI LÝ";
        txtPageSubTitle.Text = "Danh sách đại lý phân phối, cửa hàng đối tác và thông tin giao nhận hàng";
        SetActiveButton(btnCustomers);
    }

    private void ShowSuppliers()

    {

        MainContent.Content = _supplierView;

        txtPageTitle.Text = "QUẢN LÝ NHÀ CUNG CẤP";

        txtPageSubTitle.Text = "Danh sách đối tác may mặc, thông tin liên hệ và lịch sử giao hàng";

        SetActiveButton(btnSuppliers);

    }



    public void ShowImport()

    {

        _receiptView.SetTransactionType(true);

        MainContent.Content = _receiptView;

        txtPageTitle.Text = "NHẬP KHO HÀNG HÓA";

        txtPageSubTitle.Text = "Quét Barcode nhập hàng từ Nhà Cung Cấp, tăng tồn kho và in chứng từ";

        SetActiveButton(btnImport);

    }



    public void ShowExport()

    {

        _receiptView.SetTransactionType(false);

        MainContent.Content = _receiptView;

        txtPageTitle.Text = "XUẤT KHO HÀNG HÓA";

        txtPageSubTitle.Text = "Quét Barcode xuất hàng cho Khách/Đại lý, kiểm tra tồn và xuất hóa đơn";

        SetActiveButton(btnExport);

    }



    private void ShowReceiptHistory()

    {

        _ = _receiptHistoryView.LoadReceiptsAsync();

        MainContent.Content = _receiptHistoryView;

        txtPageTitle.Text = "HÓA ĐƠN & LỊCH SỬ GIAO DỊCH";

        txtPageSubTitle.Text = "Tra cứu chứng từ nhập/xuất kho và xem lại/in hóa đơn";

        SetActiveButton(btnReceiptHistory);

    }



    private void ShowWarehouseLocations()
    {
        _ = _warehouseLocationView.LoadLocationsAsync();
        MainContent.Content = _warehouseLocationView;
        txtPageTitle.Text = "QUẢN LÝ KHO & VỊ TRÍ KỆ";
        txtPageSubTitle.Text = "Danh mục kho bãi, vị trí dãy kệ lưu trữ và điều chuyển hàng nội bộ (Transfer)";
        SetActiveButton(btnLocations);
    }

    private void BtnLocations_Click(object sender, RoutedEventArgs e) => ShowWarehouseLocations();

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

        var allButtons = new[]

        {

            btnDashboard, btnProducts, btnCategories, btnSuppliers, btnCustomers,

            btnImport, btnExport, btnReceiptHistory, btnLocations,

            btnInventory, btnReports, btnEmployees

        };



        foreach (var button in allButtons)

        {

            if (button == null) continue;

            bool isCurrent = button == activeButton;

            button.Background = isCurrent

                ? new SolidColorBrush(Color.FromRgb(37, 99, 235))

                : Brushes.Transparent;

            button.Foreground = isCurrent

                ? Brushes.White

                : new SolidColorBrush(Color.FromRgb(203, 213, 225));

        }

    }

}

