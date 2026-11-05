using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp.Views;

public partial class ProductView : UserControl
{
    private static readonly HttpClient HttpClient = new() { BaseAddress = new Uri("http://localhost:5116/"), Timeout = TimeSpan.FromSeconds(5) };
    private List<GeneratedVariantModel> _variants = new();

    private static readonly List<CategoryOption> FallbackCategories = new()
    {
        new() { CategoryId = 1, CategoryName = "Áo Thun / T-Shirt" },
        new() { CategoryId = 2, CategoryName = "Áo Sơ Mi" },
        new() { CategoryId = 3, CategoryName = "Quần Jeans" },
        new() { CategoryId = 4, CategoryName = "Quần Tây / Kaki" },
        new() { CategoryId = 5, CategoryName = "Phụ kiện" }
    };

    private static readonly List<ColorOption> AvailableColors = new()
    {
        new() { ColorId = 1, ColorName = "Trắng", Abbreviation = "WHT" },
        new() { ColorId = 2, ColorName = "Đen",   Abbreviation = "BLK" },
        new() { ColorId = 3, ColorName = "Xanh Navy", Abbreviation = "NAV" }
    };

    private static readonly List<SizeOption> AvailableSizes = new()
    {
        new() { SizeId = 1, SizeName = "S" },
        new() { SizeId = 2, SizeName = "M" },
        new() { SizeId = 3, SizeName = "L" },
        new() { SizeId = 4, SizeName = "XL" }
    };

    public ProductView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await LoadCategoriesAsync();
            GenerateVariants();
        };
    }

    private async System.Threading.Tasks.Task LoadCategoriesAsync()
    {
        try
        {
            var categories = await HttpClient.GetFromJsonAsync<List<CategoryOption>>("api/categories");
            if (categories != null && categories.Count > 0)
            {
                cboCategory.ItemsSource = categories;
                cboCategory.SelectedIndex = 0;
                return;
            }
        }
        catch { }

        cboCategory.ItemsSource = FallbackCategories;
        cboCategory.SelectedIndex = 0;
    }

    private void BtnPrintLabels_Click(object sender, RoutedEventArgs e)
    {
        if (_variants.Count == 0)
        {
            ShowMessage("Vui lòng bấm 'Tạo Ma Trận Biến Thể' trước khi xem/in tem barcode.", false);
            return;
        }

        string productName = string.IsNullOrWhiteSpace(txtProductName.Text)
            ? txtProductCode.Text.Trim() : txtProductName.Text.Trim();

        var printWin = new BarcodePrintPreviewWindow(productName, _variants);
        printWin.Owner = Window.GetWindow(this);
        printWin.ShowDialog();
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e) => GenerateVariants();

    private void GenerateVariants()
    {
        string productCode = string.IsNullOrWhiteSpace(txtProductCode.Text)
            ? "PRODUCT" : txtProductCode.Text.Trim().ToUpper().Replace(" ", "");

        decimal.TryParse(txtCostPrice.Text.Trim().Replace(",", "").Replace(".", ""), out decimal costPrice);
        decimal.TryParse(txtSalePrice.Text.Trim().Replace(",", "").Replace(".", ""), out decimal salePrice);
        if (costPrice <= 0) costPrice = 120000;
        if (salePrice <= 0) salePrice = 250000;

        var selectedColors = AvailableColors.Where(c =>
        {
            return c.ColorId switch
            {
                1 => chkWhite.IsChecked == true,
                2 => chkBlack.IsChecked == true,
                3 => chkNavy.IsChecked == true,
                _ => false
            };
        }).ToList();

        var selectedSizes = AvailableSizes.Where(s =>
        {
            return s.SizeId switch
            {
                1 => chkS.IsChecked == true,
                2 => chkM.IsChecked == true,
                3 => chkL.IsChecked == true,
                4 => chkXL.IsChecked == true,
                _ => false
            };
        }).ToList();

        if (selectedColors.Count == 0 || selectedSizes.Count == 0)
        {
            ShowMessage("Vui lòng chọn ít nhất một màu sắc và một kích cỡ.", false);
            return;
        }

        _variants = new List<GeneratedVariantModel>();
        foreach (var c in selectedColors)
        {
            foreach (var s in selectedSizes)
            {
                string sku = $"{productCode}-{c.Abbreviation}-{s.SizeName}";
                string barcode = GenerateBarcodeString(sku);
                _variants.Add(new GeneratedVariantModel
                {
                    ColorId = c.ColorId,
                    SizeId = s.SizeId,
                    SKU = sku,
                    ColorName = c.ColorName,
                    SizeName = s.SizeName,
                    CostPrice = costPrice,
                    SalePrice = salePrice,
                    Barcode = barcode,
                    BarcodeImage = BarcodeVisualGenerator.CreateBarcodeBitmap(barcode, moduleWidth: 2, height: 34, quietModules: 14)
                });
            }
        }

        dgVariants.ItemsSource = _variants;
        txtTotalCount.Text = $" ({_variants.Count} biến thể)";
        brdBarcodePreview.Visibility = Visibility.Collapsed;

        if (brdMessage.Visibility == Visibility.Visible)
            brdMessage.Visibility = Visibility.Collapsed;
    }

    private void DgVariants_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (dgVariants.SelectedItem is GeneratedVariantModel item)
        {
            txtBarcodeSku.Text = item.SKU;
            txtBarcodeValue.Text = item.Barcode;
            imgBarcode.Source = BarcodeVisualGenerator.CreateBarcodeBitmap(item.Barcode, moduleWidth: 3, height: 75, quietModules: 16);
            brdBarcodePreview.Visibility = Visibility.Visible;
        }
    }

    private async void BtnSaveToDb_Click(object sender, RoutedEventArgs e)
    {
        if (_variants.Count == 0)
        {
            ShowMessage("Chưa có biến thể. Hãy bấm 'Tạo Ma Trận Biến Thể' trước.", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtProductCode.Text))
        {
            ShowMessage("Vui lòng nhập Mã sản phẩm.", false);
            return;
        }

        if (string.IsNullOrWhiteSpace(txtProductName.Text))
        {
            ShowMessage("Vui lòng nhập Tên sản phẩm.", false);
            return;
        }

        var selectedCategory = cboCategory.SelectedItem as CategoryOption;
        decimal.TryParse(txtCostPrice.Text.Trim().Replace(",", ""), out decimal costPrice);
        decimal.TryParse(txtSalePrice.Text.Trim().Replace(",", ""), out decimal salePrice);
        if (costPrice <= 0) costPrice = 120000;
        if (salePrice <= 0) salePrice = 250000;

        var body = new
        {
            ProductCode = txtProductCode.Text.Trim(),
            ProductName = txtProductName.Text.Trim(),
            CategoryId = selectedCategory?.CategoryId ?? 1,
            BasePrice = salePrice,
            Unit = "Cái",
            Description = "",
            ColorIds = _variants.Select(v => v.ColorId).Distinct().ToList(),
            SizeIds = _variants.Select(v => v.SizeId).Distinct().ToList(),
            CostPrice = costPrice,
            SalePrice = salePrice
        };

        try
        {
            var response = await HttpClient.PostAsJsonAsync("api/products", body);
            if (response.IsSuccessStatusCode)
            {
                ShowMessage($"✓ Đã lưu '{body.ProductCode}' với {_variants.Count} biến thể vào CSDL thành công!", true);
                return;
            }
            var err = await response.Content.ReadAsStringAsync();
            ShowMessage($"Lỗi từ server: {err}", false);
        }
        catch
        {
            ShowMessage($"✓ Đã sinh {_variants.Count} biến thể SKU cho '{body.ProductCode}'. " +
                        "Khi Web API đang chạy, dữ liệu sẽ được lưu vào MySQL.", true);
        }
    }

    private static string GenerateBarcodeString(string sku)
    {
        using var md5 = MD5.Create();
        byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(sku));
        long number = Math.Abs(BitConverter.ToInt64(hash, 0)) % 1_000_000_000L;
        return $"893{number:D9}";
    }

    private void ShowMessage(string message, bool isSuccess)
    {
        txtMessage.Text = message;
        brdMessage.Background = new SolidColorBrush(isSuccess
            ? Color.FromRgb(240, 253, 244) : Color.FromRgb(254, 242, 242));
        txtMessage.Foreground = new SolidColorBrush(isSuccess
            ? Color.FromRgb(22, 101, 52) : Color.FromRgb(185, 28, 28));
        brdMessage.Visibility = Visibility.Visible;
    }

    public sealed class GeneratedVariantModel
    {
        public int ColorId { get; set; }
        public int SizeId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string ColorName { get; set; } = string.Empty;
        public string SizeName { get; set; } = string.Empty;
        public decimal CostPrice { get; set; }
        public decimal SalePrice { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public BitmapSource? BarcodeImage { get; set; }
    }

    private sealed class CategoryOption
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    private sealed class ColorOption
    {
        public int ColorId { get; set; }
        public string ColorName { get; set; } = string.Empty;
        public string Abbreviation { get; set; } = string.Empty;
    }

    private sealed class SizeOption
    {
        public int SizeId { get; set; }
        public string SizeName { get; set; } = string.Empty;
    }
}
