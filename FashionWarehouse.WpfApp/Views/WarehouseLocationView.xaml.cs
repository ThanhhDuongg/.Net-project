using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace FashionWarehouse.WpfApp.Views;

public partial class WarehouseLocationView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(5)
    };

    public class WarehouseLocationModel
    {
        public int LocationId { get; set; }
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SkuCount { get; set; }
        public int TotalQuantity { get; set; }
    }

    public class LocationInventoryItemModel
    {
        public int VariantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
        public decimal CostPrice { get; set; }
    }

    private List<WarehouseLocationModel> _allLocations = new();
    private int? _selectedLocationId = null;
    private WarehouseLocationModel? _currentViewingLocation = null;
    private List<LocationInventoryItemModel> _fromLocItems = new();

    private static readonly List<WarehouseLocationModel> FallbackLocations = new()
    {
        new() { LocationId = 1, LocationCode = "KHO-A-01", LocationName = "Kệ A - Tầng 1", Description = "Khu vực chuyên áo thun & polo", SkuCount = 4, TotalQuantity = 120 },
        new() { LocationId = 2, LocationCode = "KHO-B-01", LocationName = "Kệ B - Tầng 1", Description = "Khu vực chuyên áo sơ mi công sở", SkuCount = 2, TotalQuantity = 60 },
        new() { LocationId = 3, LocationCode = "KHO-C-02", LocationName = "Kệ C - Tầng 2", Description = "Khu vực chuyên quần jeans", SkuCount = 0, TotalQuantity = 0 }
    };

    public WarehouseLocationView()
    {
        InitializeComponent();
        Loaded += (_, _) => _ = LoadLocationsAsync();
    }

    public async Task LoadLocationsAsync()
    {
        try
        {
            var res = await HttpClient.GetFromJsonAsync<List<WarehouseLocationModel>>("api/locations");
            _allLocations = res ?? FallbackLocations;
        }
        catch
        {
            _allLocations = FallbackLocations;
        }

        UpdateKpis();
        ApplyFilter();
    }

    private void UpdateKpis()
    {
        txtKpiTotal.Text = _allLocations.Count.ToString();
        txtKpiSkus.Text = _allLocations.Sum(l => l.SkuCount).ToString();
        txtKpiTotalQty.Text = _allLocations.Sum(l => l.TotalQuantity).ToString("N0");
        txtKpiEmpty.Text = _allLocations.Count(l => l.TotalQuantity == 0).ToString();
    }

    private void ApplyFilter()
    {
        var term = txtSearch?.Text?.Trim().ToLower() ?? string.Empty;
        var filtered = _allLocations.AsEnumerable();

        if (!string.IsNullOrEmpty(term))
        {
            filtered = filtered.Where(l =>
                l.LocationCode.ToLower().Contains(term) ||
                l.LocationName.ToLower().Contains(term) ||
                (l.Description != null && l.Description.ToLower().Contains(term)));
        }

        dgLocations.ItemsSource = filtered.ToList();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadLocationsAsync();
        ShowStatus("Đã làm mới danh sách kho & vị trí kệ!");
    }

    private void DgLocations_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgLocations.SelectedItem is WarehouseLocationModel loc)
        {
            PopulateEditForm(loc);
        }
    }

    private void CloseSidePanel()
    {
        _selectedLocationId = null;
        if (pnlEditForm != null) pnlEditForm.Visibility = Visibility.Collapsed;
        if (pnlInventoryList != null) pnlInventoryList.Visibility = Visibility.Collapsed;
        if (brdSidePanel != null) brdSidePanel.Visibility = Visibility.Collapsed;
        if (dgLocations != null) dgLocations.SelectedItem = null;
    }

    private void BtnNewLocation_Click(object sender, RoutedEventArgs e)
    {
        _selectedLocationId = null;
        txtLocationCode.Text = string.Empty;
        txtLocationCode.IsEnabled = true;
        txtLocationName.Text = string.Empty;
        txtDescription.Text = string.Empty;
        lblFormTitle.Text = "THÊM VỊ TRÍ KHO MỚI";
        lblSaveBtnText.Text = "💾 Tạo Vị Trí Mới";
        pnlLocationStats.Visibility = Visibility.Collapsed;

        if (brdSidePanel != null) brdSidePanel.Visibility = Visibility.Visible;
        if (pnlEditForm != null) pnlEditForm.Visibility = Visibility.Visible;
        if (pnlInventoryList != null) pnlInventoryList.Visibility = Visibility.Collapsed;
        if (dgLocations != null) dgLocations.SelectedItem = null;
        txtLocationName.Focus();
    }

    private void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WarehouseLocationModel loc)
        {
            PopulateEditForm(loc);
        }
    }

    private void PopulateEditForm(WarehouseLocationModel loc)
    {
        _selectedLocationId = loc.LocationId;
        txtLocationCode.Text = loc.LocationCode;
        txtLocationCode.IsEnabled = false; // Mã kho không nên sửa để bảo toàn khóa ngoại
        txtLocationName.Text = loc.LocationName;
        txtDescription.Text = loc.Description ?? string.Empty;
        lblFormTitle.Text = $"CẬP NHẬT [{loc.LocationCode}]";
        lblSaveBtnText.Text = "💾 Cập Nhật";

        txtStatSkus.Text = $"{loc.SkuCount} mã hàng";
        txtStatTotalQty.Text = $"{loc.TotalQuantity:N0} chiếc";
        pnlLocationStats.Visibility = Visibility.Visible;

        if (brdSidePanel != null) brdSidePanel.Visibility = Visibility.Visible;
        if (pnlEditForm != null) pnlEditForm.Visibility = Visibility.Visible;
        if (pnlInventoryList != null) pnlInventoryList.Visibility = Visibility.Collapsed;
    }

    private void BtnCancelEdit_Click(object sender, RoutedEventArgs e)
    {
        CloseSidePanel();
    }

    private async void BtnSaveLocation_Click(object sender, RoutedEventArgs e)
    {
        var name = txtLocationName.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            MessageBox.Show("Vui lòng nhập Tên kho / Vị trí kệ!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
            txtLocationName.Focus();
            return;
        }

        try
        {
            if (_selectedLocationId == null)
            {
                // Create
                var req = new
                {
                    LocationCode = string.IsNullOrWhiteSpace(txtLocationCode.Text) ? null : txtLocationCode.Text.Trim(),
                    LocationName = name,
                    Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim()
                };

                var res = await HttpClient.PostAsJsonAsync("api/locations", req);
                if (res.IsSuccessStatusCode)
                {
                    ShowStatus($"Tạo vị trí kho '{name}' thành công!");
                    await LoadLocationsAsync();
                    CloseSidePanel();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi tạo kho: {err}", "Thông báo lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                // Update
                var req = new
                {
                    LocationName = name,
                    Description = string.IsNullOrWhiteSpace(txtDescription.Text) ? null : txtDescription.Text.Trim()
                };

                var res = await HttpClient.PutAsJsonAsync($"api/locations/{_selectedLocationId.Value}", req);
                if (res.IsSuccessStatusCode)
                {
                    ShowStatus($"Cập nhật vị trí kho '{name}' thành công!");
                    await LoadLocationsAsync();
                    CloseSidePanel();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi cập nhật: {err}", "Thông báo lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối máy chủ: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WarehouseLocationModel loc)
        {
            if (loc.TotalQuantity > 0)
            {
                MessageBox.Show($"Vị trí kho '{loc.LocationCode}' đang còn tồn trữ {loc.TotalQuantity} sản phẩm.\n\nBạn phải điều chuyển hết hàng sang vị trí khác trước khi có thể xóa!",
                    "Không thể xóa", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Bạn có chắc chắn muốn xóa vị trí kho '{loc.LocationCode} - {loc.LocationName}' không?",
                "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (confirm != MessageBoxResult.Yes) return;

            try
            {
                var res = await HttpClient.DeleteAsync($"api/locations/{loc.LocationId}");
                if (res.IsSuccessStatusCode)
                {
                    ShowStatus($"Đã xóa thành công vị trí kho '{loc.LocationCode}'!");
                    await LoadLocationsAsync();
                    CloseSidePanel();
                }
                else
                {
                    var err = await res.Content.ReadAsStringAsync();
                    MessageBox.Show($"Không thể xóa: {err}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // ==========================================
    // XEM HÀNG TRÊN KỆ
    // ==========================================
    private async void BtnViewInventory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is WarehouseLocationModel loc)
        {
            _currentViewingLocation = loc;
            lblInventoryTitle.Text = $"HÀNG HÓA TẠI KỆ {loc.LocationCode}";
            lblInventorySubTitle.Text = $"{loc.LocationName} • Đang tồn {loc.TotalQuantity:N0} chiếc ({loc.SkuCount} SKU)";

            try
            {
                var items = await HttpClient.GetFromJsonAsync<List<LocationInventoryItemModel>>($"api/locations/{loc.LocationId}/inventory");
                dgLocationItems.ItemsSource = items ?? new List<LocationInventoryItemModel>();
            }
            catch
            {
                dgLocationItems.ItemsSource = new List<LocationInventoryItemModel>();
            }

            if (brdSidePanel != null) brdSidePanel.Visibility = Visibility.Visible;
            if (pnlEditForm != null) pnlEditForm.Visibility = Visibility.Collapsed;
            if (pnlInventoryList != null) pnlInventoryList.Visibility = Visibility.Visible;
        }
    }

    private void BtnCloseInventory_Click(object sender, RoutedEventArgs e)
    {
        CloseSidePanel();
    }

    private void BtnTransferCurrentLoc_Click(object sender, RoutedEventArgs e)
    {
        if (_currentViewingLocation != null)
        {
            OpenTransferDialog(_currentViewingLocation.LocationId);
        }
    }

    // ==========================================
    // ĐIỀU CHUYỂN HÀNG HÓA NỘI BỘ (TRANSFER)
    // ==========================================
    private void BtnOpenTransferModal_Click(object sender, RoutedEventArgs e)
    {
        OpenTransferDialog(null);
    }

    private void OpenTransferDialog(int? defaultFromLocId)
    {
        cboFromLocation.ItemsSource = _allLocations.Where(l => l.TotalQuantity > 0).ToList();
        cboFromLocation.DisplayMemberPath = "LocationCode";
        cboFromLocation.SelectedValuePath = "LocationId";

        cboToLocation.ItemsSource = _allLocations.ToList();
        cboToLocation.DisplayMemberPath = "LocationCode";
        cboToLocation.SelectedValuePath = "LocationId";

        if (defaultFromLocId.HasValue)
        {
            cboFromLocation.SelectedValue = defaultFromLocId.Value;
        }
        else if (cboFromLocation.Items.Count > 0)
        {
            cboFromLocation.SelectedIndex = 0;
        }

        txtTransferQty.Text = "1";
        txtTransferNotes.Text = string.Empty;
        dlgTransfer.Visibility = Visibility.Visible;
    }

    private void BtnCloseTransferDialog_Click(object sender, RoutedEventArgs e)
    {
        dlgTransfer.Visibility = Visibility.Collapsed;
    }

    private async void CboFromLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboFromLocation.SelectedValue is int fromId)
        {
            try
            {
                _fromLocItems = await HttpClient.GetFromJsonAsync<List<LocationInventoryItemModel>>($"api/locations/{fromId}/inventory") ?? new();
                cboTransferVariant.ItemsSource = _fromLocItems;
                cboTransferVariant.DisplayMemberPath = "SKU";
                cboTransferVariant.SelectedValuePath = "VariantId";

                if (_fromLocItems.Count > 0)
                {
                    cboTransferVariant.SelectedIndex = 0;
                }
                else
                {
                    lblStockAvailable.Text = "Vị trí này hiện không có hàng tồn để chuyển!";
                }
            }
            catch
            {
                _fromLocItems.Clear();
                cboTransferVariant.ItemsSource = null;
            }
        }
    }

    private void CboTransferVariant_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (cboTransferVariant.SelectedItem is LocationInventoryItemModel item)
        {
            lblStockAvailable.Text = $"Sản phẩm: {item.ProductName} ({item.ColorName}/{item.SizeName}) • Tồn khả dụng: {item.QuantityOnHand} chiếc";
        }
    }

    private async void BtnExecuteTransfer_Click(object sender, RoutedEventArgs e)
    {
        if (cboFromLocation.SelectedValue is not int fromLocId)
        {
            MessageBox.Show("Vui lòng chọn vị trí kho nguồn!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboToLocation.SelectedValue is not int toLocId)
        {
            MessageBox.Show("Vui lòng chọn vị trí kho đích tiếp nhận!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (fromLocId == toLocId)
        {
            MessageBox.Show("Kho đích phải khác kho nguồn!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (cboTransferVariant.SelectedItem is not LocationInventoryItemModel item)
        {
            MessageBox.Show("Vui lòng chọn mặt hàng cần chuyển!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (!int.TryParse(txtTransferQty.Text.Trim(), out int qty) || qty <= 0)
        {
            MessageBox.Show("Số lượng chuyển phải là số nguyên dương!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (qty > item.QuantityOnHand)
        {
            MessageBox.Show($"Số lượng chuyển ({qty}) vượt quá số lượng tồn khả dụng ({item.QuantityOnHand})!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        try
        {
            var req = new
            {
                VariantId = item.VariantId,
                FromLocationId = fromLocId,
                ToLocationId = toLocId,
                Quantity = qty,
                Notes = txtTransferNotes.Text.Trim(),
                UserId = 1
            };

            var res = await HttpClient.PostAsJsonAsync("api/locations/transfer", req);
            if (res.IsSuccessStatusCode)
            {
                MessageBox.Show($"Điều chuyển thành công {qty} sản phẩm sang vị trí mới!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                dlgTransfer.Visibility = Visibility.Collapsed;
                await LoadLocationsAsync();

                if (_currentViewingLocation != null && _currentViewingLocation.LocationId == fromLocId)
                {
                    BtnViewInventory_Click(new Button { Tag = _currentViewingLocation }, new RoutedEventArgs());
                }
            }
            else
            {
                var err = await res.Content.ReadAsStringAsync();
                MessageBox.Show($"Lỗi điều chuyển: {err}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ShowStatus(string msg)
    {
        lblStatusMessage.Text = $"✓ {msg}";
        _ = Task.Delay(4000).ContinueWith(_ => Dispatcher.Invoke(() =>
        {
            if (lblStatusMessage.Text.Contains(msg)) lblStatusMessage.Text = string.Empty;
        }));
    }
}
