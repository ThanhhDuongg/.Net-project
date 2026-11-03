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

public partial class ReceiptHistoryView : UserControl
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(6)
    };

    public class ReceiptInvoiceRow
    {
        public int ReceiptId { get; set; }
        public string ReceiptCode { get; set; } = string.Empty;
        public string ReceiptType { get; set; } = string.Empty;
        public DateTime ReceiptDate { get; set; }
        public string PartnerName { get; set; } = string.Empty;
        public string? PartnerPhone { get; set; }
        public string? PartnerAddress { get; set; }
        public int TotalQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string CreatorFullName { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<InvoiceItemModel> Items { get; set; } = new();

        public bool IsImport => string.Equals(ReceiptType, "IMPORT", StringComparison.OrdinalIgnoreCase);
        public string TypeDisplay => IsImport ? "📥 NHẬP KHO" : "📤 XUẤT KHO";
        public Brush TypeBgColor => IsImport
            ? new SolidColorBrush(Color.FromRgb(209, 250, 229))  // Xanh nhạt
            : new SolidColorBrush(Color.FromRgb(254, 226, 226)); // Đỏ nhạt
        public Brush TypeFgColor => IsImport
            ? new SolidColorBrush(Color.FromRgb(5, 150, 105))    // Xanh đậm
            : new SolidColorBrush(Color.FromRgb(220, 38, 38));   // Đỏ đậm
    }

    public class InvoiceItemModel
    {
        public int Index { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalLineAmount { get; set; }
    }

    private List<ReceiptInvoiceRow> _receipts = new();

    public ReceiptHistoryView()
    {
        InitializeComponent();
        dpFrom.SelectedDate = DateTime.Today.AddDays(-30);
        dpTo.SelectedDate = DateTime.Today;
        Loaded += (_, _) => _ = LoadReceiptsAsync();
    }

    public async Task LoadReceiptsAsync()
    {
        lblStatus.Text = "Đang tải dữ liệu...";
        try
        {
            string url = "api/receipts";
            var queryParams = new List<string>();

            if (cboFilterType.SelectedIndex == 1) queryParams.Add("type=IMPORT");
            else if (cboFilterType.SelectedIndex == 2) queryParams.Add("type=EXPORT");

            if (dpFrom.SelectedDate.HasValue)
                queryParams.Add($"from={dpFrom.SelectedDate.Value:yyyy-MM-dd}");
            if (dpTo.SelectedDate.HasValue)
                queryParams.Add($"to={dpTo.SelectedDate.Value:yyyy-MM-dd}");

            if (queryParams.Count > 0)
                url += "?" + string.Join("&", queryParams);

            var data = await HttpClient.GetFromJsonAsync<List<ReceiptInvoiceRow>>(url);
            _receipts = data ?? new List<ReceiptInvoiceRow>();

            FilterAndDisplay();
            lblStatus.Text = $"✓ Đã tải {_receipts.Count} chứng từ.";
        }
        catch (Exception ex)
        {
            lblStatus.Text = "Lỗi kết nối máy chủ";
            MessageBox.Show($"Không thể tải lịch sử chứng từ từ Web API:\n{ex.Message}", "Lỗi tải dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void FilterAndDisplay()
    {
        var term = txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrWhiteSpace(term)
            ? _receipts
            : _receipts.Where(r =>
                r.ReceiptCode.ToLower().Contains(term) ||
                r.PartnerName.ToLower().Contains(term) ||
                r.CreatorFullName.ToLower().Contains(term) ||
                (r.Notes != null && r.Notes.ToLower().Contains(term))).ToList();

        dgReceipts.ItemsSource = null;
        dgReceipts.ItemsSource = filtered;
        lblReceiptCount.Text = $"{filtered.Count} phiếu";
    }

    private void Filter_Changed(object sender, EventArgs e)
    {
        if (IsLoaded) _ = LoadReceiptsAsync();
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        FilterAndDisplay();
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        _ = LoadReceiptsAsync();
    }

    private void BtnViewInvoice_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is ReceiptInvoiceRow row)
        {
            OpenInvoice(row);
        }
    }

    private void DgReceipts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (dgReceipts.SelectedItem is ReceiptInvoiceRow row)
        {
            OpenInvoice(row);
        }
    }

    private void OpenInvoice(ReceiptInvoiceRow row)
    {
        var items = row.Items.Select(x => new InvoicePreviewWindow.InvoiceLineItem
        {
            Index = x.Index,
            SKU = x.SKU,
            DisplayName = $"{x.ProductName} ({x.ColorName} - {x.SizeName})",
            LocationCode = x.LocationCode,
            Quantity = x.Quantity,
            UnitPrice = x.UnitPrice
        });

        var invoiceWindow = new InvoicePreviewWindow(
            row.ReceiptCode,
            row.IsImport,
            row.PartnerName,
            row.PartnerPhone,
            row.CreatorFullName,
            row.Notes,
            row.ReceiptDate,
            items);

        invoiceWindow.Owner = Window.GetWindow(this);
        invoiceWindow.ShowDialog();
    }
}

