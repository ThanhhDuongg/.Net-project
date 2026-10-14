using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace FashionWarehouse.WpfApp.Views;

public partial class ReceiptView : UserControl
{
    private readonly List<ReceiptLine> _items = new();

    public ReceiptView()
    {
        InitializeComponent();
        AddItem("893600100101");
        AddItem("893600100102");
        RefreshReceipt();
        Loaded += (_, _) => txtBarcode.Focus();
    }

    private void TxtBarcode_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            AddItem(txtBarcode.Text);
            txtBarcode.Clear();
        }
    }

    private void BtnAddBarcode_Click(object sender, RoutedEventArgs e)
    {
        AddItem(txtBarcode.Text);
        txtBarcode.Clear();
        txtBarcode.Focus();
    }

    private void AddItem(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
            return;

        var existing = _items.FirstOrDefault(x => x.Barcode == barcode.Trim());
        if (existing is not null)
        {
            existing.Quantity++;
        }
        else
        {
            int itemNo = _items.Count + 1;
            _items.Add(new ReceiptLine
            {
                Index = itemNo,
                Barcode = barcode.Trim(),
                SKU = $"POLO01-{(itemNo % 2 == 0 ? "BLK" : "WHT")}-{(itemNo % 3 == 0 ? "L" : "M")}",
                DisplayName = "Áo Polo Nam Classic Cotton",
                LocationCode = "KHO-A-01",
                Quantity = 1,
                UnitPrice = 120000
            });
        }

        RefreshReceipt();
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

    private void BtnCommit_Click(object sender, RoutedEventArgs e)
    {
        MessageBox.Show(
            "Đây là giao diện test. Khi kết nối MySQL, nút này sẽ lưu phiếu và cập nhật tồn kho trong Transaction.",
            "Hoàn tất phiếu",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private sealed class ReceiptLine
    {
        public int Index { get; set; }
        public string Barcode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalLineAmount => Quantity * UnitPrice;
    }
}
