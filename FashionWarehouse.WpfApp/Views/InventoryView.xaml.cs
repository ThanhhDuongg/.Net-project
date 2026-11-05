using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;

namespace FashionWarehouse.WpfApp.Views;

public partial class InventoryView : UserControl
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/") };
    private List<InventoryStatusRow> _inventory = new();
    private bool _isLoading;

    public InventoryView()
    {
        InitializeComponent();
        Loaded += (_, _) => LoadInventoryAsync();
    }

    private async void LoadInventoryAsync()
    {
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            var rawList = await HttpClient.GetFromJsonAsync<List<InventoryApiDto>>("api/inventory/statuses?take=5000");
            _inventory = (rawList ?? new List<InventoryApiDto>()).Select(x => new InventoryStatusRow
            {
                LocationId = x.LocationId,
                VariantId = x.VariantId,
                SKU = x.SKU,
                Barcode = x.Barcode,
                ProductName = x.ProductName,
                ColorName = x.ColorName,
                SizeName = x.SizeName,
                LocationCode = x.LocationCode,
                QuantityOnHand = x.QuantityOnHand,
                SafetyStock = x.SafetyStock,
                CostPrice = x.CostPrice
            }).ToList();

            var selectedLocation = cboLocation.SelectedItem as string;
            var locations = _inventory.Select(item => item.LocationCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(loc => loc)
                .ToList();
            locations.Insert(0, "Tất cả vị trí kệ");
            cboLocation.ItemsSource = locations;
            cboLocation.SelectedItem = selectedLocation is not null && locations.Contains(selectedLocation)
                ? selectedLocation
                : locations[0];

            BuildShelfCards();
            ApplyFilters();
        }
        catch (Exception ex)
        {
            _inventory.Clear();
            dgInventory.ItemsSource = null;
            txtRows.Text = "0";
            txtQuantity.Text = "0";
            txtLowStock.Text = "0";
            txtLocationCount.Text = "0";
            MessageBox.Show($"Không thể tải dữ liệu tồn kho từ Web API.\n\nChi tiết: {ex.Message}",
                "Lỗi tải tồn kho", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private void BuildShelfCards()
    {
        var shelfGroups = _inventory
            .GroupBy(item => item.LocationCode)
            .OrderBy(group => group.Key)
            .Select(group => new ShelfCardModel
            {
                LocationCode = group.Key,
                LocationName = GetLocationDisplayName(group.Key),
                Description = GetLocationDescription(group.Key),
                ShelfType = "Khu Vực Lưu Trữ",
                SkuCount = group.Select(x => x.VariantId).Distinct().Count(),
                TotalQuantity = group.Sum(x => x.QuantityOnHand)
            })
            .ToList();

        icShelfCards.ItemsSource = shelfGroups;
        txtLocationCount.Text = shelfGroups.Count.ToString("N0");
    }

    private static string GetLocationDisplayName(string code) => code switch
    {
        "KHO-A-01" => "Kệ A - Tầng 1",
        "KHO-B-01" => "Kệ B - Tầng 1",
        "KHO-C-02" => "Kệ C - Tầng 2",
        _ => $"Khu Vực {code}"
    };

    private static string GetLocationDescription(string code) => code switch
    {
        "KHO-A-01" => "Khu vực phân loại và lưu trữ các mẫu áo phông, áo Polo thời trang.",
        "KHO-B-01" => "Khu vực lưu trữ các dòng áo sơ mi dài tay, sơ mi oxford công sở.",
        "KHO-C-02" => "Khu vực giá treo và xếp quần jeans, quần kaki cao cấp.",
        _ => "Vị trí lưu trữ chuyên biệt theo danh mục sản phẩm."
    };

    private void ApplyFilters()
    {
        if (dgInventory is null) return;

        string search = txtSearch?.Text?.Trim() ?? "";
        string location = cboLocation?.SelectedItem as string ?? "Tất cả vị trí kệ";
        int statusFilterIndex = cboStockStatus?.SelectedIndex ?? 0;

        var filtered = _inventory.Where(item =>
        {
            // 1. Location Filter
            if (location != "Tất cả vị trí kệ" && !item.LocationCode.Equals(location, StringComparison.OrdinalIgnoreCase))
                return false;

            // 2. Status Filter
            // Index 0: Tất cả, 1: Cần nhập (<= SafetyStock), 2: Hết hàng (= 0), 3: An toàn (> SafetyStock)
            if (statusFilterIndex == 1 && item.QuantityOnHand > item.SafetyStock)
                return false;
            if (statusFilterIndex == 2 && item.QuantityOnHand > 0)
                return false;
            if (statusFilterIndex == 3 && item.QuantityOnHand <= item.SafetyStock)
                return false;

            // 3. Search Filter
            if (!string.IsNullOrEmpty(search))
            {
                bool matches = item.SKU.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               item.Barcode.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               item.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               item.ColorName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                               item.LocationCode.Contains(search, StringComparison.OrdinalIgnoreCase);
                if (!matches) return false;
            }

            return true;
        }).ToList();

        dgInventory.ItemsSource = filtered;

        // Cập nhật thẻ chỉ số KPI
        txtRows.Text = filtered.Count.ToString("N0");
        txtQuantity.Text = filtered.Sum(item => item.QuantityOnHand).ToString("N0");
        txtLowStock.Text = filtered.Count(item => item.IsLowStock).ToString("N0");
    }

    private void Filters_Changed(object sender, RoutedEventArgs e)
    {
        if (!_isLoading) ApplyFilters();
    }

    private void Filters_Changed(object sender, SelectionChangedEventArgs e)
    {
        if (!_isLoading) ApplyFilters();
    }

    private void Filters_Changed(object sender, TextChangedEventArgs e)
    {
        if (!_isLoading) ApplyFilters();
    }

    private void ViewMode_Changed(object sender, RoutedEventArgs e)
    {
        if (pnlListView is null || pnlShelfView is null) return;

        if (rbListView.IsChecked == true)
        {
            pnlListView.Visibility = Visibility.Visible;
            pnlShelfView.Visibility = Visibility.Collapsed;
        }
        else
        {
            pnlListView.Visibility = Visibility.Collapsed;
            pnlShelfView.Visibility = Visibility.Visible;
        }
    }

    private void BtnFilterByLocation_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is string locationCode)
        {
            rbListView.IsChecked = true;
            if (cboLocation.Items.Contains(locationCode))
            {
                cboLocation.SelectedItem = locationCode;
            }
        }
    }

    private void BtnViewBarcode_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is InventoryStatusRow row)
        {
            var win = new BarcodeItemDetailWindow(row);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }
    }

    private void BtnRefresh_Click(object sender, RoutedEventArgs e) => LoadInventoryAsync();

    private async void BtnExportExcel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            Title = "Lưu danh sách kiểm kê kho",
            FileName = $"KiemKeTonKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx",
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
                MessageBox.Show("Máy chủ không xuất được file Excel.", "Lỗi xuất báo cáo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var bytes = await response.Content.ReadAsByteArrayAsync();
            await File.WriteAllBytesAsync(dialog.FileName, bytes);
            MessageBox.Show($"Đã xuất danh sách kiểm kê thành công tại:\n{dialog.FileName}",
                "Xuất file thành công", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể xuất file: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    public sealed class InventoryApiDto
    {
        public int LocationId { get; set; }
        public int VariantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
        public decimal CostPrice { get; set; }
    }

    public sealed class InventoryStatusRow
    {
        public int LocationId { get; set; }
        public int VariantId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int QuantityOnHand { get; set; }
        public int SafetyStock { get; set; }
        public decimal CostPrice { get; set; }
        public decimal TotalValue => QuantityOnHand * CostPrice;

        public bool IsLowStock { get => QuantityOnHand <= SafetyStock; set { } }

        public string StatusText => QuantityOnHand == 0
            ? "❌ Hết hàng"
            : (QuantityOnHand <= SafetyStock ? "⚠️ Cần nhập" : "✅ Còn hàng");

        public string StatusBgColor => QuantityOnHand == 0
            ? "#FEE2E2"
            : (QuantityOnHand <= SafetyStock ? "#FEF3C7" : "#DCFCE7");

        public string StatusFgColor => QuantityOnHand == 0
            ? "#B91C1C"
            : (QuantityOnHand <= SafetyStock ? "#B45309" : "#15803D");
    }

    public sealed class ShelfCardModel
    {
        public string LocationCode { get; set; } = string.Empty;
        public string LocationName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ShelfType { get; set; } = string.Empty;
        public int SkuCount { get; set; }
        public int TotalQuantity { get; set; }
    }
}