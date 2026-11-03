using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp.Views;

public partial class BarcodePrintPreviewWindow : Window
{
    public BarcodePrintPreviewWindow(string productName, List<ProductView.GeneratedVariantModel> variants)
    {
        InitializeComponent();
        LoadBarcodeLabels(productName, variants);
    }

    private void LoadBarcodeLabels(string productName, List<ProductView.GeneratedVariantModel> variants)
    {
        txtPrintHeader.Text = $"Mẫu: {productName}";
        txtPrintDate.Text = $"Ngày in: {DateTime.Now:dd/MM/yyyy HH:mm}";
        txtTotalLabels.Text = $"{variants.Count} tem nhãn";

        var labelList = new List<BarcodeLabelModel>();

        foreach (var v in variants)
        {
            var barcodeImage = BarcodeVisualGenerator.CreateBarcodeImage(v.Barcode, 230, 40);

            labelList.Add(new BarcodeLabelModel
            {
                ProductName = productName,
                SKU = v.SKU,
                VariantSummary = $"{v.ColorName} - Size {v.SizeName}",
                Barcode = v.Barcode,
                BarcodeImage = barcodeImage,
                PriceText = $"{v.SalePrice:N0} đ"
            });
        }

        icLabels.ItemsSource = labelList;
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        var printDialog = new PrintDialog();
        if (printDialog.ShowDialog() == true)
        {
            try
            {
                printDialog.PrintVisual(printArea, "Fashion WMS - Danh Sách Tem Mã Vạch Barcode");
                MessageBox.Show("Lệnh in đã được gửi thành công đến máy in!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi in: {ex.Message}", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    public class BarcodeLabelModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string VariantSummary { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public ImageSource? BarcodeImage { get; set; }
        public string PriceText { get; set; } = string.Empty;
    }
}

