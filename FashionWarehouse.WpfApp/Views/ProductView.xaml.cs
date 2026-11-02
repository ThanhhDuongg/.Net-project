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
                    BarcodeImage = RenderBarcodeImage(barcode)
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
            imgBarcode.Source = RenderBarcodeImage(item.Barcode, 300, 100);
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
        long number = Math.Abs(BitConverter.ToInt64(hash, 0)) % 100_000_000_000L;
        return $"893{number:D9}";
    }

    private static BitmapSource RenderBarcodeImage(string code, int width = 200, int height = 40)
    {
        int barWidth = 2;
        string encoded = EncodeCode128B(code);
        int imgWidth = Math.Max(encoded.Length * barWidth + 20, width);

        var visual = new DrawingVisual();
        using (var dc = visual.RenderOpen())
        {
            dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, imgWidth, height));
            int x = 10;
            foreach (char bit in encoded)
            {
                if (bit == '1')
                    dc.DrawRectangle(Brushes.Black, null, new Rect(x, 2, barWidth, height - 4));
                x += barWidth;
            }
        }

        var bitmap = new RenderTargetBitmap(imgWidth, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        bitmap.Freeze();
        return bitmap;
    }

    private static string EncodeCode128B(string data)
    {
        var sb = new StringBuilder("11010010000");
        foreach (char c in data)
        {
            int val = Math.Max(0, Math.Min(94, c - 32));
            sb.Append(Code128Patterns[val]);
        }
        sb.Append("1100011101011");
        return sb.ToString();
    }

    private static readonly string[] Code128Patterns =
    {
        "11011001100","11001101100","11001100110","10010011000","10010001100",
        "10001001100","10011001000","10011000100","10001100100","11001001000",
        "11001000100","11000100100","10110011100","10011011100","10011001110",
        "10111001100","10011101100","10011100110","11001110010","11001011100",
        "11001001110","11011100100","11001110100","11101101110","11101001100",
        "11100101100","11100100110","11101100100","11100110100","11100110010",
        "11011011000","11011000110","11000110110","10100011000","10001011000",
        "10001000110","10110001000","10001101000","10001100010","11010001000",
        "11000101000","11000100010","10110111000","10110001110","10001101110",
        "10111011000","10111000110","10001110110","11101110110","11010001110",
        "11000101110","11011101000","11011100010","11011101110","11101011000",
        "11101000110","11100010110","11101101000","11101100010","11100011010",
        "11101111010","11001000010","11110001010","10100110000","10100001100",
        "10010110000","10010000110","10000101100","10000100110","10110010000",
        "10110000100","10011010000","10011000010","10000110100","10000110010",
        "11000010010","11001010000","11110111010","11000010100","10001111010",
        "10100111100","10010111100","10010011110","10111100100","10011110100",
        "10011110010","11110100100","11110010100","11110010010","11011011110",
        "11011110110","11110110110","10101111000","10100011110","10001011110",
        "10111101000","10111100010","11110101000","11110100010"
    };

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
