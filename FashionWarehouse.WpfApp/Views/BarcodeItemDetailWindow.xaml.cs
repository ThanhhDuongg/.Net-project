using System.Windows;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp.Views;

public partial class BarcodeItemDetailWindow : Window
{
    private readonly InventoryView.InventoryStatusRow _item;

    public BarcodeItemDetailWindow(InventoryView.InventoryStatusRow item)
    {
        InitializeComponent();
        _item = item;
        LoadData();
    }

    private void LoadData()
    {
        txtProductName.Text = _item.ProductName;
        txtVariantSummary.Text = $"Màu: {_item.ColorName} — Size: {_item.SizeName}";
        txtSku.Text = $"SKU: {_item.SKU}";
        txtStock.Text = $"Tồn: {_item.QuantityOnHand:N0} cái";
        txtLocation.Text = $"Kệ: {_item.LocationCode}";

        txtBarcodeValue.Text = _item.Barcode;
        imgBarcode.Source = BarcodeVisualGenerator.CreateBarcodeBitmap(_item.Barcode, moduleWidth: 3, height: 85, quietModules: 16);
    }

    private void BtnOpenImport_Click(object sender, RoutedEventArgs e)
    {
        if (Owner is MainWindow main)
        {
            main.ShowImport();
        }
        Close();
    }

    private void BtnOpenExport_Click(object sender, RoutedEventArgs e)
    {
        if (Owner is MainWindow main)
        {
            main.ShowExport();
        }
        Close();
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}

