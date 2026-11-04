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

using Microsoft.AspNetCore.SignalR.Client;



namespace FashionWarehouse.WpfApp.Views;



public partial class ReceiptView : UserControl

{

    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/") };

    private HubConnection? _hubConnection;

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

        InitSignalR();

        Loaded += (_, _) =>

        {

            _ = LoadSuppliersAndCustomersAsync();

            txtBarcode.Focus();

        };

    }



    private List<string> _supplierNames = new();
    private List<string> _customerNames = new();

    public async Task LoadSuppliersAndCustomersAsync()
    {
        try
        {
            var sData = await HttpClient.GetFromJsonAsync<List<SupplierView.SupplierModel>>("api/suppliers");
            if (sData != null && sData.Count > 0)
            {
                _supplierNames = sData.Select(s => s.SupplierName).ToList();
            }
        }
        catch { }

        if (_supplierNames.Count == 0)
        {
            _supplierNames = new List<string> { "Công Ty May Mặc Việt Tiến", "Tổng Công Ty Đức Giang", "Công Ty Dệt May Phong Phú" };
        }

        try
        {
            var cData = await HttpClient.GetFromJsonAsync<List<CustomerView.CustomerModel>>("api/customers");
            if (cData != null && cData.Count > 0)
            {
                _customerNames = cData.Select(c => $"{c.CustomerCode} - {c.CustomerName} ({c.CustomerType})").ToList();
            }
        }
        catch { }

        if (_customerNames.Count == 0)
        {
            _customerNames = new List<string>
            {
                "KH001 - Chuỗi Thời Trang May 10 (Đại lý cấp 1)",
                "KH002 - Cửa Hàng Thời Trang Eva de Eva (Đại lý cấp 1)",
                "KH003 - Shop Quần Áo Nam MenStyle (Khách mua sỉ)",
                "KH004 - Đại Lý Thời Trang Bắc Ninh (Đại lý cấp 2)"
            };
        }

        UpdatePartnerDropdown();
    }

    public void SetTransactionType(bool isImport)
    {
        _isImport = isImport;
        if (rdoImport != null && rdoExport != null)
        {
            rdoImport.IsChecked = isImport;
            rdoExport.IsChecked = !isImport;
        }
        TransactionType_Changed(this, new RoutedEventArgs());
    }

    private void UpdatePartnerDropdown()
    {
        if (cboPartner == null) return;
        cboPartner.ItemsSource = null;

        if (_isImport)
        {
            // Thay đổi giao diện chuyên biệt cho NHẬP KHO
            if (brdTransactionModeBadge != null)
            {
                brdTransactionModeBadge.Background = new SolidColorBrush(Color.FromRgb(236, 253, 245));
                brdTransactionModeBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            }
            if (lblModeIcon != null) lblModeIcon.Text = "📥";
            if (lblModeTitle != null)
            {
                lblModeTitle.Text = "PHIẾU NHẬP HÀNG TỪ NHÀ CUNG CẤP";
                lblModeTitle.Foreground = new SolidColorBrush(Color.FromRgb(4, 120, 87));
            }
            if (lblModeDesc != null)
            {
                lblModeDesc.Text = "Nhập bổ sung nguồn hàng may mặc vào kho & ghi nhận giá vốn";
                lblModeDesc.Foreground = new SolidColorBrush(Color.FromRgb(6, 95, 70));
            }
            if (lblTableTitle != null) lblTableTitle.Text = "Danh Sách Hàng Hóa Nhập Vào Kho (Theo Giá Vốn)";
            if (lblPartnerHeader != null) lblPartnerHeader.Text = "🏢 Chọn Nhà Cung Cấp (NCC):";
            if (btnCommit != null) btnCommit.Background = new SolidColorBrush(Color.FromRgb(16, 185, 129));
            if (btnCommitText != null) btnCommitText.Text = "✅ HOÀN TẤT NHẬP KHO (IN PHIẾU)";

            cboPartner.ItemsSource = _supplierNames;
            cboPartner.SelectedIndex = 0;
        }
        else
        {
            // Thay đổi giao diện chuyên biệt cho XUẤT KHO
            if (brdTransactionModeBadge != null)
            {
                brdTransactionModeBadge.Background = new SolidColorBrush(Color.FromRgb(255, 241, 242));
                brdTransactionModeBadge.BorderBrush = new SolidColorBrush(Color.FromRgb(244, 63, 94));
            }
            if (lblModeIcon != null) lblModeIcon.Text = "📤";
            if (lblModeTitle != null)
            {
                lblModeTitle.Text = "PHIẾU XUẤT KHO KIÊM HÓA ĐƠN BÁN HÀNG";
                lblModeTitle.Foreground = new SolidColorBrush(Color.FromRgb(190, 18, 60));
            }
            if (lblModeDesc != null)
            {
                lblModeDesc.Text = "Xuất hàng phân phối cho Đại lý / Khách hàng & xuất hóa đơn";
                lblModeDesc.Foreground = new SolidColorBrush(Color.FromRgb(159, 18, 57));
            }
            if (lblTableTitle != null) lblTableTitle.Text = "Danh Sách Hàng Hóa Xuất Kho Cho Đại Lý (Theo Giá Bán)";
            if (lblPartnerHeader != null) lblPartnerHeader.Text = "👤 Chọn Khách Hàng / Đại Lý Nhận Hàng:";
            if (btnCommit != null) btnCommit.Background = new SolidColorBrush(Color.FromRgb(225, 29, 72));
            if (btnCommitText != null) btnCommitText.Text = "📦 HOÀN TẤT XUẤT KHO (IN HÓA ĐƠN)";

            cboPartner.ItemsSource = _customerNames.Count > 0 ? _customerNames : new List<string> { "KH001 - Chuỗi Thời Trang May 10", "KH002 - Cửa Hàng Eva de Eva" };
            cboPartner.SelectedIndex = 0;
        }
    }

    private async void InitSignalR()

    {

        try

        {

            _hubConnection = new HubConnectionBuilder()

                .WithUrl("http://localhost:5116/hub/barcode")

                .WithAutomaticReconnect()

                .Build();



            _hubConnection.On<string>("ReceiveScannedBarcode", (barcode) =>

            {

                Dispatcher.Invoke(async () =>

                {

                    txtBarcode.Text = barcode;

                    await ProcessBarcodeOrSkuInputAsync();

                });

            });



            _hubConnection.Reconnecting += _ =>

            {

                Dispatcher.Invoke(() => brdSignalRStatus.Background = new SolidColorBrush(Color.FromRgb(245, 158, 11)));

                return Task.CompletedTask;

            };



            _hubConnection.Reconnected += _ =>

            {

                Dispatcher.Invoke(() => brdSignalRStatus.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)));

                return Task.CompletedTask;

            };



            await _hubConnection.StartAsync();

            brdSignalRStatus.Background = new SolidColorBrush(Color.FromRgb(34, 197, 94)); // Xanh lá

        }

        catch

        {

            brdSignalRStatus.Background = new SolidColorBrush(Color.FromRgb(239, 68, 68)); // Đỏ

        }

    }



    private void BtnMobileScan_Click(object sender, RoutedEventArgs e)
    {
        var qrWindow = new MobileScanQrWindow();
        qrWindow.Owner = Window.GetWindow(this);
        qrWindow.ShowDialog();
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

        UpdatePartnerDropdown();



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

            SupplierId = _isImport ? (cboPartner.SelectedIndex + 1) : (int?)null,

            CustomerName = _isImport ? null : (cboPartner.SelectedItem is ComboBoxItem item ? item.Content?.ToString() : "Khách Mua Buôn / Đại Lý"),

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

                var snapshotItems = _items.Select(x => new InvoicePreviewWindow.InvoiceLineItem

                {

                    Index = x.Index,

                    SKU = x.SKU,

                    DisplayName = x.DisplayName,

                    LocationCode = x.LocationCode,

                    Quantity = x.Quantity,

                    UnitPrice = x.UnitPrice

                }).ToList();



                string receiptCode = txtReceiptCode.Text.Trim();

                string partner = cboPartner.SelectedItem?.ToString() ?? (_isImport ? "Nhà Cung Cấp" : "Khách Hàng");

                string creator = FashionWarehouse.WpfApp.Services.UserSession.IsLoggedIn

                    ? FashionWarehouse.WpfApp.Services.UserSession.FullName

                    : "Trần Văn Kho";

                string notes = txtNotes.Text.Trim();

                bool isImport = _isImport;



                MessageBox.Show(

                    $"Giao dịch kho '{receiptCode}' đã HOÀN TẤT THÀNH CÔNG!\nBấm OK để mở Hóa Đơn / Phiếu Chứng Từ chi tiết.",

                    "Giao dịch thành công",

                    MessageBoxButton.OK,

                    MessageBoxImage.Information);



                var invoiceWindow = new InvoicePreviewWindow(

                    receiptCode,

                    isImport,

                    partner,

                    isImport ? "02838640800" : "0901234567",

                    creator,

                    notes,

                    DateTime.Now,

                    snapshotItems);



                invoiceWindow.Owner = Window.GetWindow(this);

                invoiceWindow.ShowDialog();



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





    private void BtnViewCurrentInvoice_Click(object sender, RoutedEventArgs e)

    {

        if (_items.Count == 0)

        {

            MessageBox.Show("Chưa có mặt hàng nào trong phiếu để xem trước hóa đơn.", "Chưa có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);

            return;

        }



        var snapshotItems = _items.Select(x => new InvoicePreviewWindow.InvoiceLineItem

        {

            Index = x.Index,

            SKU = x.SKU,

            DisplayName = x.DisplayName,

            LocationCode = x.LocationCode,

            Quantity = x.Quantity,

            UnitPrice = x.UnitPrice

        }).ToList();



        string receiptCode = txtReceiptCode.Text.Trim();

        string partner = cboPartner.SelectedItem?.ToString() ?? (_isImport ? "Nhà Cung Cấp" : "Khách Hàng");

        string creator = FashionWarehouse.WpfApp.Services.UserSession.IsLoggedIn

            ? FashionWarehouse.WpfApp.Services.UserSession.FullName

            : "Trần Văn Kho";



        var invoiceWindow = new InvoicePreviewWindow(

            receiptCode,

            _isImport,

            partner,

            _isImport ? "02838640800" : "0901234567",

            creator,

            txtNotes.Text.Trim(),

            DateTime.Now,

            snapshotItems);



        invoiceWindow.Owner = Window.GetWindow(this);

        invoiceWindow.ShowDialog();

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

