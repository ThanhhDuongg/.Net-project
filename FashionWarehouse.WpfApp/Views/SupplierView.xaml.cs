using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FashionWarehouse.WpfApp.Views;

public partial class SupplierView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public class SupplierModel
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public bool IsActive { get; set; } = true;
        public int TotalReceipts { get; set; }
    }

    private List<SupplierModel> _allSuppliers = new();
    private int? _selectedSupplierId = null;

    private static readonly List<SupplierModel> FallbackSuppliers = new()
    {
        new() { SupplierId = 1, SupplierName = "Công Ty May Mặc Việt Tiến", ContactName = "Nguyễn Văn Hùng", PhoneNumber = "02838640800", Email = "viettien@supplier.vn", Address = "Quận Tân Bình, TP.HCM", IsActive = true, TotalReceipts = 18 },
        new() { SupplierId = 2, SupplierName = "Tổng Công Ty Đức Giang", ContactName = "Lê Thị Lan", PhoneNumber = "02438271544", Email = "ducgiang@supplier.vn", Address = "Long Biên, Hà Nội", IsActive = true, TotalReceipts = 12 },
        new() { SupplierId = 3, SupplierName = "Công Ty Dệt May Phong Phú", ContactName = "Trần Thanh Phong", PhoneNumber = "02838963533", Email = "phongphu@supplier.vn", Address = "TP. Thủ Đức, TP.HCM", IsActive = true, TotalReceipts = 7 },
        new() { SupplierId = 4, SupplierName = "Tập Đoàn Dệt May Việt Nam (Vinatex)", ContactName = "Phạm Đình Đoàn", PhoneNumber = "02438257008", Email = "vinatex@supplier.vn", Address = "Hoàn Kiếm, Hà Nội", IsActive = true, TotalReceipts = 9 }
    };

    public SupplierView()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = LoadSuppliersAsync();
    }

    private async System.Threading.Tasks.Task LoadSuppliersAsync()
    {
        try
        {
            var data = await HttpClient.GetFromJsonAsync<List<SupplierModel>>("api/suppliers");
            _allSuppliers = (data != null && data.Count > 0) ? data : new List<SupplierModel>(FallbackSuppliers);
        }
        catch
        {
            _allSuppliers = new List<SupplierModel>(FallbackSuppliers);
        }

        UpdateKpiAndGrid();
    }

    private void UpdateKpiAndGrid()
    {
        txtKpiTotal.Text = _allSuppliers.Count.ToString("N0");
        txtKpiActive.Text = _allSuppliers.Count(s => s.IsActive).ToString("N0");
        txtKpiReceipts.Text = _allSuppliers.Sum(s => s.TotalReceipts).ToString("N0");

        FilterAndDisplay();
    }

    private void FilterAndDisplay()
    {
        var term = txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _allSuppliers
            : _allSuppliers.Where(s =>
                s.SupplierName.ToLower().Contains(term) ||
                (s.ContactName != null && s.ContactName.ToLower().Contains(term)) ||
                (s.PhoneNumber != null && s.PhoneNumber.Contains(term)) ||
                (s.Address != null && s.Address.ToLower().Contains(term))).ToList();

        dgSuppliers.ItemsSource = null;
        dgSuppliers.ItemsSource = filtered;
        lblCount.Text = $"{filtered.Count} đối tác";
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterAndDisplay();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadSuppliersAsync();
    }

    private void BtnNewSupplier_Click(object sender, RoutedEventArgs e)
    {
        ClearForm();
        txtSupplierName.Focus();
    }

    private void DgSuppliers_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgSuppliers.SelectedItem is SupplierModel s)
        {
            _selectedSupplierId = s.SupplierId;
            lblFormTitle.Text = $"CẬP NHẬT NHÀ CUNG CẤP #{s.SupplierId}";
            btnSaveText.Text = "LƯU THAY ĐỔI";
            txtSupplierName.Text = s.SupplierName;
            txtContactName.Text = s.ContactName ?? "";
            txtPhoneNumber.Text = s.PhoneNumber ?? "";
            txtEmail.Text = s.Email ?? "";
            txtAddress.Text = s.Address ?? "";
            btnDelete.Visibility = Visibility.Visible;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string name = txtSupplierName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            ShowMessage("Vui lòng nhập tên Nhà cung cấp.", false);
            txtSupplierName.Focus();
            return;
        }

        var body = new
        {
            SupplierName = name,
            ContactName = txtContactName.Text.Trim(),
            PhoneNumber = txtPhoneNumber.Text.Trim(),
            Email = txtEmail.Text.Trim(),
            Address = txtAddress.Text.Trim()
        };

        try
        {
            if (_selectedSupplierId.HasValue)
            {
                var response = await HttpClient.PutAsJsonAsync($"api/suppliers/{_selectedSupplierId.Value}", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã cập nhật thành công nhà cung cấp '{name}'!", true);
                    ClearForm();
                    await LoadSuppliersAsync();
                    return;
                }
                var err = await response.Content.ReadAsStringAsync();
                ShowMessage($"Lỗi cập nhật: {err}", false);
            }
            else
            {
                var response = await HttpClient.PostAsJsonAsync("api/suppliers", body);
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã thêm mới nhà cung cấp '{name}' vào CSDL thành công!", true);
                    ClearForm();
                    await LoadSuppliersAsync();
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
        if (!_selectedSupplierId.HasValue) return;

        var res = MessageBox.Show(
            $"Bạn có chắc chắn muốn ngưng hoạt động nhà cung cấp '{txtSupplierName.Text}'?",
            "Xác nhận ngưng hoạt động",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (res == MessageBoxResult.Yes)
        {
            try
            {
                var response = await HttpClient.DeleteAsync($"api/suppliers/{_selectedSupplierId.Value}");
                if (response.IsSuccessStatusCode)
                {
                    ShowMessage($"✓ Đã ngưng hoạt động nhà cung cấp thành công.", true);
                    ClearForm();
                    await LoadSuppliersAsync();
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
        _selectedSupplierId = null;
        lblFormTitle.Text = "THÊM MỚI NHÀ CUNG CẤP";
        btnSaveText.Text = "LƯU NHÀ CUNG CẤP";
        txtSupplierName.Clear();
        txtContactName.Clear();
        txtPhoneNumber.Clear();
        txtEmail.Clear();
        txtAddress.Clear();
        btnDelete.Visibility = Visibility.Collapsed;
        dgSuppliers.SelectedItem = null;
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        lblStatusMessage.Text = message;
        lblStatusMessage.Foreground = isSuccess
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129))
            : new SolidColorBrush(Color.FromRgb(220, 38, 38));
    }
}

