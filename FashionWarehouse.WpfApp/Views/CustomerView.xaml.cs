using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FashionWarehouse.WpfApp.Views;

public partial class CustomerView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public class CustomerModel
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerType { get; set; } = "Đại lý cấp 1";
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public decimal DebtAmount { get; set; }
        public bool IsActive { get; set; } = true;

        public Brush TypeBgColor => CustomerType switch
        {
            "Đại lý cấp 1" => new SolidColorBrush(Color.FromRgb(219, 234, 254)), // Xanh dương nhạt
            "Đại lý cấp 2" => new SolidColorBrush(Color.FromRgb(224, 231, 255)), // Indigo nhạt
            "Khách mua sỉ" => new SolidColorBrush(Color.FromRgb(209, 250, 229)), // Xanh lá nhạt
            _ => new SolidColorBrush(Color.FromRgb(241, 245, 249))              // Xám nhạt
        };

        public Brush TypeFgColor => CustomerType switch
        {
            "Đại lý cấp 1" => new SolidColorBrush(Color.FromRgb(29, 78, 216)),
            "Đại lý cấp 2" => new SolidColorBrush(Color.FromRgb(67, 56, 202)),
            "Khách mua sỉ" => new SolidColorBrush(Color.FromRgb(4, 120, 87)),
            _ => new SolidColorBrush(Color.FromRgb(71, 85, 105))
        };
    }

    private List<CustomerModel> _allCustomers = new();
    private int? _selectedCustomerId = null;

    private static readonly List<CustomerModel> FallbackCustomers = new()
    {
        new() { CustomerId = 1, CustomerCode = "KH001", CustomerName = "Chuỗi Thời Trang May 10", CustomerType = "Đại lý cấp 1", PhoneNumber = "0988123456", Email = "may10@fashion.vn", Address = "Quận Hà Đông, Hà Nội", IsActive = true },
        new() { CustomerId = 2, CustomerCode = "KH002", CustomerName = "Cửa Hàng Thời Trang Eva de Eva", CustomerType = "Đại lý cấp 1", PhoneNumber = "0903987654", Email = "eva@fashion.vn", Address = "Quận 1, TP.HCM", IsActive = true },
        new() { CustomerId = 3, CustomerCode = "KH003", CustomerName = "Shop Quần Áo Nam MenStyle", CustomerType = "Khách mua sỉ", PhoneNumber = "0912345678", Email = "menstyle@gmail.com", Address = "Quận Cầu Giấy, Hà Nội", IsActive = true },
        new() { CustomerId = 4, CustomerCode = "KH004", CustomerName = "Đại Lý Thời Trang Bắc Ninh", CustomerType = "Đại lý cấp 2", PhoneNumber = "0977889911", Email = "bacninh.fashion@gmail.com", Address = "TP. Bắc Ninh, Bắc Ninh", IsActive = true }
    };

    public CustomerView()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = LoadCustomersAsync();
    }

    public async Task LoadCustomersAsync()
    {
        try
        {
            var data = await HttpClient.GetFromJsonAsync<List<CustomerModel>>("api/customers");
            _allCustomers = (data != null && data.Count > 0) ? data : new List<CustomerModel>(FallbackCustomers);
        }
        catch
        {
            _allCustomers = new List<CustomerModel>(FallbackCustomers);
        }

        UpdateKpiAndGrid();
    }

    private void UpdateKpiAndGrid()
    {
        txtKpiTotal.Text = _allCustomers.Count.ToString("N0");
        txtKpiAgencies.Text = _allCustomers.Count(c => c.CustomerType.Contains("Đại lý")).ToString("N0");
        txtKpiRetail.Text = _allCustomers.Count(c => c.CustomerType.Contains("sỉ") || c.CustomerType.Contains("lẻ")).ToString("N0");

        FilterAndDisplay();
    }

    private void FilterAndDisplay()
    {
        var term = txtSearch.Text.Trim().ToLower();
        string filterType = (cboFilterType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả phân loại";

        var filtered = _allCustomers.AsEnumerable();

        if (filterType != "Tất cả phân loại")
        {
            filtered = filtered.Where(c => c.CustomerType == filterType);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            filtered = filtered.Where(c =>
                c.CustomerCode.ToLower().Contains(term) ||
                c.CustomerName.ToLower().Contains(term) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(term)) ||
                (c.Address != null && c.Address.ToLower().Contains(term)));
        }

        var list = filtered.ToList();
        dgCustomers.ItemsSource = null;
        dgCustomers.ItemsSource = list;
        lblCount.Text = $"{list.Count} khách hàng";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterAndDisplay();
    }

    private void CboFilterType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsLoaded) FilterAndDisplay();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadCustomersAsync();
    }

    private void BtnNewCustomer_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        txtCustomerName.Focus();
    }

    private void DgCustomers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgCustomers.SelectedItem is CustomerModel c)
        {
            _selectedCustomerId = c.CustomerId;
            lblFormTitle.Text = $"CẬP NHẬT KHÁCH HÀNG #{c.CustomerCode}";
            btnSaveText.Text = "LƯU THAY ĐỔI";
            txtCustomerCode.Text = c.CustomerCode;
            txtCustomerCode.IsEnabled = false; // Không sửa mã khi update
            txtCustomerName.Text = c.CustomerName;

            foreach (ComboBoxItem item in cboCustomerType.Items)
            {
                if (item.Content?.ToString() == c.CustomerType)
                {
                    cboCustomerType.SelectedItem = item;
                    break;
                }
            }

            txtPhoneNumber.Text = c.PhoneNumber ?? "";
            txtEmail.Text = c.Email ?? "";
            txtAddress.Text = c.Address ?? "";
            btnDelete.Visibility = Visibility.Visible;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = txtCustomerName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowMessage("Vui lòng nhập tên Khách hàng / Đại lý.", false);
            txtCustomerName.Focus();
            return;
        }

        string type = (cboCustomerType.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Đại lý cấp 1";

        try
        {
            if (_selectedCustomerId.HasValue)
            {
                var body = new
                {
                    CustomerName = name,
                    CustomerType = type,
                    PhoneNumber = txtPhoneNumber.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim(),
                    DebtAmount = 0
                };

                var response = await HttpClient.PutAsJsonAsync($"api/customers/{_selectedCustomerId.Value}", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã cập nhật thành công khách hàng '{name}'!", true);
                    ClearForm();
                    await LoadCustomersAsync();
                    return;
                }
                var err = await response.Content.ReadAsStringAsync();
                ShowMessage($"Lỗi cập nhật: {err}", false);
            }
            else
            {
                var body = new
                {
                    CustomerCode = txtCustomerCode.Text.Trim(),
                    CustomerName = name,
                    CustomerType = type,
                    PhoneNumber = txtPhoneNumber.Text.Trim(),
                    Email = txtEmail.Text.Trim(),
                    Address = txtAddress.Text.Trim()
                };

                var response = await HttpClient.PostAsJsonAsync("api/customers", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã thêm mới khách hàng '{name}' vào CSDL thành công!", true);
                    ClearForm();
                    await LoadCustomersAsync();
                    return;
                }
                var err = await response.Content.ReadAsStringAsync();
                ShowMessage($"Lỗi thêm mới: {err}", false);
            }
        }
        catch (Exception ex)
        {
            ShowMessage($"Không thể kết nối đến máy chủ: {ex.Message}", false);
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (!_selectedCustomerId.HasValue) return;

        var res = MessageBox.Show(
            $"Bạn có chắc chắn muốn ngưng giao dịch với khách hàng '{txtCustomerName.Text}'?",
            "Xác nhận ngưng giao dịch",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"api/customers/{_selectedCustomerId.Value}");
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã ngưng giao dịch khách hàng thành công.", true);
                    ClearForm();
                    await LoadCustomersAsync();
                }
                else
                {
                    var err = await response.Content.ReadAsStringAsync();
                    ShowMessage($"Lỗi: {err}", false);
                }
            }
            catch (Exception ex)
            {
                ShowMessage($"Lỗi kết nối: {ex.Message}", false);
            }
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
    }

    private void ClearForm()
    {
        _selectedCustomerId = null;
        lblFormTitle.Text = "THÊM MỚI KHÁCH HÀNG / ĐẠI LÝ";
        btnSaveText.Text = "LƯU KHÁCH HÀNG";
        txtCustomerCode.Clear();
        txtCustomerCode.IsEnabled = true;
        txtCustomerName.Clear();
        cboCustomerType.SelectedIndex = 0;
        txtPhoneNumber.Clear();
        txtEmail.Clear();
        txtAddress.Clear();
        btnDelete.Visibility = Visibility.Collapsed;
        dgCustomers.SelectedItem = null;
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        lblStatusMessage.Text = message;
        lblStatusMessage.Foreground = isSuccess
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
            : new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }
}

