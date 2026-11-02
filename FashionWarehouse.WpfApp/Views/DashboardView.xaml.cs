using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;

namespace FashionWarehouse.WpfApp.Views;

public partial class DashboardView : UserControl
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/") };

    public DashboardView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadDashboardDataAsync();
    }

    private async void LoadDashboardDataAsync()
    {
        try
        {
            var summary = await HttpClient.GetFromJsonAsync<DashboardSummaryDto>("api/dashboard/summary");
            var alerts = await HttpClient.GetFromJsonAsync<List<InventoryAlertDto>>("api/dashboard/low-stock?take=100");

            if (summary != null)
            {
                txtSkuCount.Text = summary.TotalSkus.ToString("N0");
                txtInventoryQuantity.Text = summary.TotalQuantity.ToString("N0");
                txtLowStockCount.Text = $"{summary.LowStockSkuCount:N0} SKU";
                txtInventoryValue.Text = summary.InventoryValue >= 1_000_000_000
                    ? $"{summary.InventoryValue / 1_000_000_000m:N2} Tỷ"
                    : $"{summary.InventoryValue / 1_000_000m:N0} Triệu";

                RenderChart(summary.DailyMovements);
            }

            dgAlerts.ItemsSource = alerts ?? new List<InventoryAlertDto>();
        }
        catch (Exception ex)
        {
            txtSkuCount.Text = "—";
            txtInventoryQuantity.Text = "—";
            txtLowStockCount.Text = "—";
            txtInventoryValue.Text = "—";
            icMovementChart.ItemsSource = Array.Empty<ChartColumnViewModel>();
            dgAlerts.ItemsSource = Array.Empty<InventoryAlertDto>();
            MessageBox.Show($"Không thể tải số liệu dashboard từ API: {ex.Message}",
                "Không có dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void RenderChart(IReadOnlyList<DailyMovementDto>? movements)
    {
        if (movements == null || movements.Count == 0)
        {
            icMovementChart.ItemsSource = Array.Empty<ChartColumnViewModel>();
            return;
        }

        var maxVal = Math.Max(1, movements.Select(x => Math.Max(x.Imports, x.Exports)).DefaultIfEmpty(1).Max());
        const double chartMaxHeight = 90.0;

        icMovementChart.ItemsSource = movements.Select(x => new ChartColumnViewModel
        {
            DateLabel = x.Date.ToString("dd/MM"),
            ImportHeight = Math.Max(4, (x.Imports / (double)maxVal) * chartMaxHeight),
            ExportHeight = Math.Max(4, (x.Exports / (double)maxVal) * chartMaxHeight),
            ImportTooltip = $"Nhập: {x.Imports} cái",
            ExportTooltip = $"Xuất: {x.Exports} cái"
        }).ToList();
    }

    private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var response = await HttpClient.GetAsync("api/reports/inventory.xlsx");
            if (!response.IsSuccessStatusCode)
            {
                MessageBox.Show("Không thể tải file báo cáo từ máy chủ.", "Lỗi tải file", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            var downloads = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
            Directory.CreateDirectory(downloads);
            var filePath = Path.Combine(downloads, $"BaoCaoTonKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
            await File.WriteAllBytesAsync(filePath, bytes);

            var openResult = MessageBox.Show(
                $"Đã xuất báo cáo ClosedXML thành công (gồm 2 Sheets: Tồn kho & Lịch sử giao dịch):\n{filePath}\n\nBạn có muốn mở file ngay không?",
                "Xuất Excel thành công",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (openResult == MessageBoxResult.Yes)
            {
                Process.Start(new ProcessStartInfo(filePath) { UseShellExecute = true });
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Lưu ý: Chức năng xuất Excel gọi trực tiếp endpoint 'api/reports/inventory.xlsx' từ backend ClosedXML.\nĐảm bảo Web API đang chạy: {ex.Message}",
                "Thông báo kết nối API",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }

    private sealed class DashboardSummaryDto
    {
        public int TotalSkus { get; set; }
        public int TotalQuantity { get; set; }
        public int LowStockSkuCount { get; set; }
        public decimal InventoryValue { get; set; }
        public List<DailyMovementDto> DailyMovements { get; set; } = new();
    }

    private sealed class DailyMovementDto
    {
        public DateTime Date { get; set; }
        public int Imports { get; set; }
        public int Exports { get; set; }
    }

    private sealed class InventoryAlertDto
    {
        public string SKU { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
    }

    private sealed class ChartColumnViewModel
    {
        public string DateLabel { get; set; } = string.Empty;
        public double ImportHeight { get; set; }
        public double ExportHeight { get; set; }
        public string ImportTooltip { get; set; } = string.Empty;
        public string ExportTooltip { get; set; } = string.Empty;
    }
}
