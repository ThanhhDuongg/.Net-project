using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace FashionWarehouse.WpfApp.Views;

public partial class ProductView : UserControl
{
    public ProductView()
    {
        InitializeComponent();
        GenerateVariants();
    }

    private void BtnGenerate_Click(object sender, RoutedEventArgs e) => GenerateVariants();

    private void GenerateVariants()
    {
        var colors = new List<(int Id, string Name, bool IsSelected)>
        {
            (1, "Trắng", chkWhite.IsChecked == true),
            (2, "Đen", chkBlack.IsChecked == true),
            (3, "Xanh Navy", chkNavy.IsChecked == true)
        };

        var sizes = new List<(int Id, string Name, bool IsSelected)>
        {
            (1, "S", chkS.IsChecked == true),
            (2, "M", chkM.IsChecked == true),
            (3, "L", chkL.IsChecked == true),
            (4, "XL", chkXL.IsChecked == true)
        };

        string productCode = string.IsNullOrWhiteSpace(txtProductCode.Text) ? "PRODUCT" : txtProductCode.Text.Trim();
        var result = new List<object>();

        foreach (var c in colors.Where(c => c.IsSelected))
        {
            foreach (var s in sizes.Where(s => s.IsSelected))
            {
                string sku = $"{productCode}-{c.Name[..3].ToUpper()}-{s.Name}";
                result.Add(new
                {
                    SKU = sku,
                    ColorName = c.Name,
                    SizeName = s.Name,
                    SalePrice = 250000,
                    Barcode = $"893{Math.Abs(sku.GetHashCode()) % 1000000000:D9}"
                });
            }
        }

        dgVariants.ItemsSource = result;
        txtTotalCount.Text = $" ({result.Count} biến thể)";
    }
}
