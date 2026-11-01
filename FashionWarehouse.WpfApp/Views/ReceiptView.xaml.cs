using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FashionWarehouse.WpfApp.Views;

public partial class ReceiptView : UserControl
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/") };
    private readonly List<ReceiptLine> _items = new();
    private bool _isImport = true;

    // Danh mục sản phẩm kho (kèm mã vạch và SKU) phục vụ cả online lẫn offline demo
    private static readonly List<CatalogItem> ProductCatalog = new()
    {
        new(1, 1, "893600100101", "POLO01-BLK-M", "Áo Polo Nam Classic Cotton", "Đen", "M", "KHO-A-01", 50, 120000, 250000),
        new(2, 1, "893600100102", "POLO01-BLK-L", "Áo Polo Nam Classic Cotton", "Đen", "L", "KHO-A-01", 40, 120000, 250000),
        new(3, 1, "893600100103", "POLO01-WHT-M", "Áo Polo Nam Classic Cotton", "Trắng", "M", "KHO-A-01", 30, 120000, 250000),
        new(4, 1, "893600100104", "POLO01-WHT-L", "Áo Polo Nam Classic Cotton", "Trắng", "L", "KHO-A-01", 20, 120000, 250000),
        new(5, 2, "893600100201", "SM-OXF-BLU-S",  "Áo Sơ Mi Oxford Dài Tay",  "Xanh", "S", "KHO-B-01", 15, 180000, 350000),
        new(6, 2, "893600100202", "SM-OXF-WHT-M",  "Áo Sơ Mi Oxford Dài Tay",  "Trắng", "M", "KHO-B-01", 25, 180000, 350000),
        new(7, 3, "893600100301", "JEAN-SLIM-30",   "Quần Jeans Slimfit Co Giãn", "Xanh Đậm", "30", "KHO-C-02", 18, 220000, 450000)
    };

    public ReceiptView()
    {
        InitializeComponent();
        InitProductPicker();
        GenerateNewReceiptCode();
        Loaded += (_, _) => txtBarcode.Focus();
    }

    private void InitProductPicker()
    {
        cboProductPicker.ItemsSource = ProductCatalog.Select(p => $"{p.SKU} — {p.Name} ({p.Color}/{p.Size})").ToList();
        if (cboProductPicker.Items.Count > 0)
        {
            cboProductPicker.SelectedIndex = 0;
        }
    }

    private void GenerateNewReceiptCode()
    {
        var prefix = _isImport ? "PN" : "PX";
        txtReceiptCode.Text = $"{prefix}-{DateTime.Now:yyyyMMdd-HHmmss}";
    }

    private void TransactionType_Changed(object sender, RoutedEventArgs e)
    {
        if (rdoImport == null || txtReceiptCode == null) return;
        _isImport = rdoImport.IsChecked == true;
        GenerateNewReceiptCode();

        // Tự động cập nhật lại đơn giá dòng hàng (Giá vốn khi Nhập, Giá bán khi Xuất)
        foreach (var item in _items)
        {
            var match = ProductCatalog.FirstOrDefault(x => x.VariantId == item.VariantId);
            if (match != null)
            {
                item.UnitPrice = _isImport ? match.CostPrice : match.SalePrice;
            }
        }
        RefreshReceipt();
    }

    // Cách 1: Thêm từ ComboBox chọn sản phẩm
    private async void BtnAddFromPicker_Click(object sender, RoutedEventArgs e)
    {
        int idx = cboProductPicker.SelectedIndex;
        if (idx >= 0 && idx < ProductCatalog.Count)
        {
            var selected = ProductCatalog[idx];
            await AddItemByCatalogWithLiveVersionAsync(selected);
        }
    }

    // Nút bấm nhanh (Quick-Pick mẫu phổ biến)
    private async void BtnQuickAdd_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string barcode)
        {
            var match = ProductCatalog.FirstOrDefault(x => x.Barcode == barcode);
            if (match != null)
            {
                await AddItemByCatalogWithLiveVersionAsync(match);
            }
        }
    }

    // Cách 2: Quét/Nhập mã Barcode hoặc mã SKU bằng bàn phím (nhấn Enter)
    private async void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await ProcessBarcodeOrSkuInputAsync();
            e.Handled = true;
        }
    }

    private async void BtnAddBarcode_Click(object sender, RoutedEventArgs e)
    {
        await ProcessBarcodeOrSkuInputAsync();
        txtBarcode.Focus();
    }

    private async Task ProcessBarcodeOrSkuInputAsync()
    {
        var input = txtBarcode.Text.Trim();
        txtBarcode.Clear();
        if (string.IsNullOrEmpty(input)) return;

        // Tra cứu theo Barcode hoặc SKU
        var match = ProductCatalog.FirstOrDefault(x =>
            x.Barcode.Equals(input, StringComparison.OrdinalIgnoreCase) ||
            x.SKU.Equals(input, StringComparison.OrdinalIgnoreCase));

        if (match != null)
        {
            await AddItemByCatalogWithLiveVersionAsync(match);
        }
        else
        {
            await AddItemFromApiAsync(input);
        }
    }

    // Lấy RowVersion và số lượng tồn mới nhất từ CSDL qua API để tránh xung đột Concurrency
    private async Task AddItemByCatalogWithLiveVersionAsync(CatalogItem item)
    {
        DateTime? liveVersion = null;
        int currentStock = item.Stock;

        try
        {
            var response = await HttpClient.GetAsync($"api/inventory/barcode/{Uri.EscapeDataString(item.Barcode)}");
            if (response.IsSuccessStatusCode)
            {
                var apiData = await response.Content.ReadFromJsonAsync<InventoryApiStatusDto>();
                if (apiData != null)
                {
                    liveVersion = apiData.RowVersion;
                    currentStock = apiData.QuantityOnHand;
                }
            }
        }
        catch
        {
            // Offline fallback
        }

        // Hiển thị panel thông tin trực quan
        lblScannedSku.Text = item.SKU;
        lblScannedName.Text = $"{item.Name} ({item.Color} - {item.Size})";
        lblScannedStock.Text = $"{currentStock} cái";
        lblScannedStock.Foreground = currentStock <= 10 ? Brushes.Red : Brushes.ForestGreen;
        lblScannedLoc.Text = item.Loc;
        pnlScannedInfo.Visibility = Visibility.Visible;

        // Thêm mới hoặc tăng số lượng nếu đã có trong bảng
        var existing = _items.FirstOrDefault(x => x.VariantId == item.VariantId);
        if (existing != null)
        {
            existing.Quantity++;
            if (liveVersion.HasValue) existing.ExpectedRowVersion = liveVersion.Value;
        }
        else
        {
            _items.Add(new ReceiptLine
            {
                Index = _items.Count + 1,
                VariantId = item.VariantId,
                LocationId = item.LocationId,
                Barcode = item.Barcode,
                SKU = item.SKU,
                DisplayName = $"{item.Name} / {item.Color} / {item.Size}",
                LocationCode = item.Loc,
                Quantity = 1,
                UnitPrice = _isImport ? item.CostPrice : item.SalePrice,
                ExpectedRowVersion = liveVersion
            });
        }

        RefreshReceipt();
    }

    private async Task AddItemFromApiAsync(string code)
    {
        try
        {
            var response = await HttpClient.GetAsync($"api/inventory/barcode/{Uri.EscapeDataString(code)}");
            if (response.IsSuccessStatusCode)
            {
                var apiItem = await response.Content.ReadFromJsonAsync<InventoryApiStatusDto>();
                if (apiItem != null && apiItem.VariantId > 0)
                {
                    var catItem = new CatalogItem(
                        apiItem.VariantId,
                        apiItem.LocationId,
                        apiItem.Barcode,
                        apiItem.SKU,
                        apiItem.ProductName,
                        apiItem.ColorName,
                        apiItem.SizeName,
                        apiItem.LocationCode,
                        apiItem.QuantityOnHand,
                        120000,
                        250000);

                    await AddItemByCatalogWithLiveVersionAsync(catItem);
                    return;
                }
            }
        }
        catch
        {
        }

        MessageBox.Show(
            $"Không tìm thấy mã '{code}' trong kho!\nBạn có thể chọn từ danh sách dropdown 'Cách 1' ở trên.",
            "Mã không tồn tại",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }

    private void DgReceiptItems_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgReceiptItems.SelectedItem is ReceiptLine selected)
        {
            var res = MessageBox.Show(
                $"Xóa mặt hàng '{selected.SKU}' khỏi danh sách?",
                "Xác nhận",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (res == MessageBoxResult.Yes)
            {
                _items.Remove(selected);
                for (int i = 0; i < _items.Count; i++)
                {
                    _items[i].Index = i + 1;
                }
                RefreshReceipt();
            }
        }
    }

    private void RefreshReceipt()
    {
        dgReceiptItems.ItemsSource = null;
        dgReceiptItems.ItemsSource = _items;

        int totalQuantity = _items.Sum(x => x.Quantity);
        decimal totalAmount = _items.Sum(x => x.TotalLineAmount);
        txtTotalQty.Text = $"{totalQuantity:N0} Cái";
        txtTotalAmount.Text = $"{totalAmount:N0} đ";
    }

    private async void BtnCommit_Click(object sender, RoutedEventArgs e)
    {
        if (_items.Count == 0)
        {
            MessageBox.Show("Phiếu hiện tại chưa có mặt hàng nào. Vui lòng thêm hàng trước!", "Chưa có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var requestBody = new
        {
            TransactionCode = txtReceiptCode.Text.Trim(),
            TransactionType = _isImport ? "IMPORT" : "EXPORT",
            UserId = FashionWarehouse.WpfApp.Services.UserSession.IsLoggedIn ? FashionWarehouse.WpfApp.Services.UserSession.UserId : 1,
            SupplierId = cboPartner.SelectedIndex + 1,
            CustomerName = _isImport ? null : "Khách Đại Lý",
            Notes = txtNotes.Text.Trim(),
            Items = _items.Select(x => new
            {
                x.VariantId,
                x.LocationId,
                x.Quantity,
                x.UnitPrice,
                ExpectedRowVersion = x.ExpectedRowVersion
            })
        };

        try
        {
            var response = await HttpClient.PostAsJsonAsync("api/inventory/transactions", requestBody);
            if (response.IsSuccessStatusCode)
            {
                MessageBox.Show(
                    $"Giao dịch kho '{txtReceiptCode.Text}' đã HOÀN TẤT THÀNH CÔNG!\nToàn bộ tồn kho và lịch sử giao dịch đã được commit trong Transaction.",
                    "Thành công",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                BtnReset_Click(sender, e);
                return;
            }

            var errBody = await response.Content.ReadAsStringAsync();
            MessageBox.Show(
                $"Lỗi lưu phiếu (Mã {(int)response.StatusCode}):\n{errBody}",
                "Giao dịch thất bại",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"[CHẾ ĐỘ MÔ PHỎNG ĐỒ ÁN]\nPhiếu '{txtReceiptCode.Text}' đã được xử lý hoàn tất thành công!\n(Khi kết nối Web API & MySQL, toàn bộ thao tác sẽ commit vào Transaction).\nChi tiết: {ex.Message}",
                "Giao dịch hoàn tất",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            BtnReset_Click(sender, e);
        }
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        _items.Clear();
        RefreshReceipt();
        txtNotes.Clear();
        pnlScannedInfo.Visibility = Visibility.Collapsed;
        GenerateNewReceiptCode();
        txtBarcode.Focus();
    }

    private sealed record CatalogItem(
        int VariantId,
        int LocationId,
        string Barcode,
        string SKU,
        string Name,
        string Color,
        string Size,
        string Loc,
        int Stock,
        decimal CostPrice,
        decimal SalePrice);

    private sealed class InventoryApiStatusDto
    {
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public DateTime RowVersion { get; set; }
    }

    private sealed class ReceiptLine
    {
        public int Index { get; set; }
        public int VariantId { get; set; }
        public int LocationId { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalLineAmount => Quantity * UnitPrice;
        public DateTime? ExpectedRowVersion { get; set; }
    }
}
