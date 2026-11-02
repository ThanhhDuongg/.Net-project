using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FashionWarehouse.WpfApp.Views;

public partial class ReportsView : UserControl
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/") };
    private List<TransactionHistoryRow> _allTransactions = new();

    public ReportsView()
    {
        InitializeComponent();
        dpTo.SelectedDate = DateTime.Today;
        dpFrom.SelectedDate = DateTime.Today.AddDays(-13);
        Loaded += (_, _) => _ = LoadAllDataAsync();
    }

    private async System.Threading.Tasks.Task LoadAllDataAsync()
    {
        await LoadReportAsync();
        await LoadTransactionsAsync();
        await LoadRestockForecastAsync();
    }

    private async System.Threading.Tasks.Task LoadReportAsync()
    {
        if (dpFrom.SelectedDate is not DateTime from || dpTo.SelectedDate is not DateTime to)
        {
            txtStatus.Text = "Vui lòng chọn đầy đủ ngày bắt đầu và ngày kết thúc.";
            return;
        }

        if (from.Date > to.Date)
        {
            txtStatus.Text = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.";
            return;
        }

        try
        {
            string fromValue = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string toValue = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var summary = await HttpClient.GetFromJsonAsync<DashboardSummaryDto>(
                $"api/dashboard/summary?from={fromValue}&to={toValue}");

            if (summary is null)
            {
                txtStatus.Text = "Máy chủ không trả dữ liệu thống kê.";
                return;
            }

            txtInventoryValue.Text = summary.InventoryValue.ToString("N0") + " đ";

            int totalImports = summary.DailyMovements.Sum(x => x.Imports);
            int totalExports = summary.DailyMovements.Sum(x => x.Exports);
            int netFlow = totalImports - totalExports;

            txtTotalImports.Text = totalImports.ToString("N0");
            txtTotalExports.Text = totalExports.ToString("N0");
            txtNetFlow.Text = (netFlow >= 0 ? "+" : "") + netFlow.ToString("N0");
            txtNetFlow.Foreground = netFlow >= 0
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(16, 185, 129))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(220, 38, 38));

            var movementRows = summary.DailyMovements.Select(m => new MovementRowModel
            {
                Date = m.Date,
                Imports = m.Imports,
                Exports = m.Exports,
                NetChange = m.Imports - m.Exports,
                Note = (m.Imports > 0 && m.Exports > 0)
                    ? "Nhập và xuất diễn ra trong ngày"
                    : (m.Imports > 0 ? "Chỉ phát sinh nhập kho" : (m.Exports > 0 ? "Chỉ phát sinh xuất kho" : "Không biến động"))
            }).ToList();

            dgMovements.ItemsSource = movementRows;
            txtStatus.Text = $"Dữ liệu thống kê luân chuyển kho từ {from:dd/MM/yyyy} đến {to:dd/MM/yyyy} ({movementRows.Count} ngày ghi nhận).";
        }
        catch (Exception ex)
        {
            txtStatus.Text = $"Không thể tải thống kê từ máy chủ: {ex.Message}";
        }
    }

    private async System.Threading.Tasks.Task LoadTransactionsAsync()
    {
        if (dpFrom.SelectedDate is not DateTime from || dpTo.SelectedDate is not DateTime to)
            return;

        try
        {
            string fromValue = from.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            string toValue = to.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

            var rawTxns = await HttpClient.GetFromJsonAsync<List<TransactionDto>>(
                $"api/reports/transactions?from={fromValue}&to={toValue}");

            _allTransactions = (rawTxns ?? new List<TransactionDto>()).Select(t => new TransactionHistoryRow
            {
                ReferenceNo = string.IsNullOrEmpty(t.ReferenceNo) ? $"TXN-{t.TransactionId:D6}" : t.ReferenceNo,
                OccurredAt = t.OccurredAt,
                TransactionType = t.TransactionType,
                SKU = t.SKU,
                ProductName = t.ProductName,
                LocationCode = t.LocationCode,
                QuantityChange = t.QuantityChange,
                QuantityBefore = t.QuantityBefore,
                QuantityAfter = t.QuantityAfter,
                Notes = t.Notes ?? ""
            }).ToList();

            ApplyTransactionFilter();
        }
        catch
        {
            _allTransactions.Clear();
            dgTransactions.ItemsSource = null;
            txtLogCount.Text = "Chưa có giao dịch";
        }
    }

    private void ApplyTransactionFilter()
    {
        string term = txtSearchLog?.Text?.Trim() ?? "";
        var filtered = _allTransactions.Where(t =>
            string.IsNullOrEmpty(term) ||
            t.ReferenceNo.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.SKU.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.ProductName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.LocationCode.Contains(term, StringComparison.OrdinalIgnoreCase) ||
            t.Notes.Contains(term, StringComparison.OrdinalIgnoreCase)
        ).ToList();

        dgTransactions.ItemsSource = filtered;
        txtLogCount.Text = $"{filtered.Count} giao dịch";
    }

    private async System.Threading.Tasks.Task LoadRestockForecastAsync()
    {
        try
        {
            var rawList = await HttpClient.GetFromJsonAsync<List<InventoryRawDto>>("api/inventory/statuses?lowStockOnly=true");
            var rows = (rawList ?? new List<InventoryRawDto>()).Select(x =>
            {
                int suggested = Math.Max(1, x.SafetyStock * 2 - x.QuantityOnHand);
                return new RestockForecastRow
                {
                    SKU = x.SKU,
                    ProductName = x.ProductName,
                    VariantDetail = $"{x.ColorName} - Size {x.SizeName}",
                    LocationCode = x.LocationCode,
                    QuantityOnHand = x.QuantityOnHand,
                    SafetyStock = x.SafetyStock,
                    SuggestedOrderQty = suggested,
                    CostPrice = x.CostPrice,
                    EstimatedCost = suggested * x.CostPrice
                };
            }).ToList();

            dgRestock.ItemsSource = rows;
            decimal totalCost = rows.Sum(r => r.EstimatedCost);
            txtEstimatedCost.Text = totalCost.ToString("N0") + " đ";
        }
        catch
        {
            dgRestock.ItemsSource = null;
            txtEstimatedCost.Text = "0 đ";
        }
    }

    private void TxtSearchLog_TextChanged(object sender, TextChangedEventArgs e) => ApplyTransactionFilter();

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e) => await LoadAllDataAsync();

    private async void BtnRefreshTransactions_Click(object sender, RoutedEventArgs e) => await LoadTransactionsAsync();

    private void BtnPresetToday_Click(object sender, RoutedEventArgs e)
    {
        dpFrom.SelectedDate = DateTime.Today;
        dpTo.SelectedDate = DateTime.Today;
        _ = LoadAllDataAsync();
    }

    private void BtnPreset7Days_Click(object sender, RoutedEventArgs e)
    {
        dpFrom.SelectedDate = DateTime.Today.AddDays(-6);
        dpTo.SelectedDate = DateTime.Today;
        _ = LoadAllDataAsync();
    }

    private void BtnPreset14Days_Click(object sender, RoutedEventArgs e)
    {
        dpFrom.SelectedDate = DateTime.Today.AddDays(-13);
        dpTo.SelectedDate = DateTime.Today;
        _ = LoadAllDataAsync();
    }

    private void BtnPreset30Days_Click(object sender, RoutedEventArgs e)
    {
        dpFrom.SelectedDate = DateTime.Today.AddDays(-29);
        dpTo.SelectedDate = DateTime.Today;
        _ = LoadAllDataAsync();
    }

    private async void BtnExport_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu báo cáo thống kê kho",
            FileName = $"BaoCaoKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
            DefaultExt = ".xlsx",
            Filter = "Excel Workbook (*.xlsx)|*.xlsx",
            AddExtension = true,
            OverwritePrompt = true
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            using var response = await HttpClient.GetAsync("api/reports/inventory.xlsx");
            if (!response.IsSuccessStatusCode)
            {
                var details = await response.Content.ReadAsStringAsync();
                MessageBox.Show($"Máy chủ không xuất được báo cáo (HTTP {(int)response.StatusCode}).\n{details}",
                    "Xuất báo cáo thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            MessageBox.Show($"Đã xuất báo cáo ClosedXML thành công (Sheet Tồn kho & Sheet Lịch sử giao dịch):\n{dialog.FileName}",
                "Xuất báo cáo thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tải hoặc lưu báo cáo: {ex.Message}",
                "Xuất báo cáo thất bại", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public sealed class DashboardSummaryDto
    {
        public int TotalSkus { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockSkuCount { get; set; }
        public decimal InventoryValue { get; set; }
        public List<DashboardChartPointDto> DailyMovements { get; set; } = new();
    }

    public sealed class DashboardChartPointDto
    {
        public DateTime Date { get; set; }
        public int Imports { get; set; }
        public int Exports { get; set; }
    }

    public sealed class MovementRowModel
    {
        public DateTime Date { get; set; }
        public int Imports { get; set; }
        public int Exports { get; set; }
        public int NetChange { get; set; }
        public string Note { get; set; } = string.Empty;
    }

    public sealed class TransactionDto
    {
        public long TransactionId { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string ReferenceNo { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public DateTime OccurredAt { get; set; }
        public string? Notes { get; set; }
    }

    public sealed class TransactionHistoryRow
    {
        public string ReferenceNo { get; set; } = string.Empty;
        public DateTime OccurredAt { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityChange { get; set; }
        public int QuantityBefore { get; set; }
        public int QuantityAfter { get; set; }
        public string Notes { get; set; } = string.Empty;

        public string TypeDisplay => TransactionType == "IMPORT" ? "NHẬP KHO" : (TransactionType == "EXPORT" ? "XUẤT KHO" : TransactionType);
        public string TypeBgColor => TransactionType == "IMPORT" ? "#DCFCE7" : "#FEE2E2";
        public string TypeFgColor => TransactionType == "IMPORT" ? "#15803D" : "#B91C1C";

        public string QuantityDisplay => (QuantityChange > 0 ? "+" : "") + QuantityChange.ToString("N0");
        public string QuantityColor => QuantityChange > 0 ? "#15803D" : "#B91C1C";
    }

    public sealed class InventoryRawDto
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
        public decimal CostPrice { get; set; }
    }

    public sealed class RestockForecastRow
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string VariantDetail { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
        public int SuggestedOrderQty { get; set; }
        public decimal CostPrice { get; set; }
        public decimal EstimatedCost { get; set; }
    }
}