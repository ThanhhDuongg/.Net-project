using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FashionWarehouse.WpfApp.Services;

namespace FashionWarehouse.WpfApp.Views;

public partial class InvoicePreviewWindow : Window
{
    public class InvoiceLineItem
    {
        public int Index { get; set; }
        public string SKU { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string LocationCode { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalLineAmount => Quantity * UnitPrice;
    }

    public InvoicePreviewWindow(
        string receiptCode,
        bool isImport,
        string partnerName,
        string? partnerPhone,
        string creatorName,
        string? notes,
        DateTime date,
        IEnumerable<InvoiceLineItem> items)
    {
        InitializeComponent();

        lblHeaderTitle.Text = isImport ? "HÓA ĐƠN / CHỨNG TỪ NHẬP KHO" : "HÓA ĐƠN / CHỨNG TỪ XUẤT KHO";
        txtTitle.Text = isImport ? "PHIẾU NHẬP KHO HÀNG HÓA" : "PHIẾU XUẤT KHO KIÊM HÓA ĐƠN GIAO HÀNG";
        txtTitle.Foreground = isImport
            ? new SolidColorBrush(Color.FromRgb(16, 185, 129)) // Xanh ngọc
            : new SolidColorBrush(Color.FromRgb(225, 29, 72));  // Đỏ hồng

        txtSubTitle.Text = isImport
            ? "(Dùng cho nhập hàng từ Nhà Cung Cấp vào hệ thống kho Fashion WMS)"
            : "(Dùng cho xuất hàng phân phối sỉ/lẻ và đại lý)";

        lblPartnerTitle.Text = isImport ? "Nhà cung cấp: " : "Khách hàng / Đại lý: ";
        lblSignPartner.Text = isImport ? "Người Giao Hàng" : "Người Nhận Hàng";
        txtPaymentType.Text = isImport ? "Nhập kho chính ngạch" : "Xuất bán buôn / Giao đại lý";

        txtCode.Text = receiptCode;
        lblBarcodeText.Text = receiptCode;
        txtPartner.Text = string.IsNullOrWhiteSpace(partnerName) ? (isImport ? "Công Ty May Mặc Việt Tiến" : "Khách mua buôn") : partnerName;
        txtPartnerPhone.Text = string.IsNullOrWhiteSpace(partnerPhone) ? (isImport ? "02838640800" : "0901234567") : partnerPhone;
        txtCreator.Text = creatorName;
        txtSignCreator.Text = creatorName;
        txtNotes.Text = string.IsNullOrWhiteSpace(notes) ? (isImport ? "Nhập kho hoàn tất" : "Xuất kho giao đại lý") : notes;
        txtDate.Text = $"Ngày {date:dd} tháng {date:MM} năm {date:yyyy} (Lúc {date:HH:mm})";

        // Sinh hình barcode mã phiếu
        try
        {
            imgReceiptBarcode.Source = BarcodeVisualGenerator.CreateBarcodeImage(receiptCode, 180, 40);
        }
        catch
        {
        }

        var itemList = items.ToList();
        dgItems.ItemsSource = itemList;

        int totalQty = itemList.Sum(x => x.Quantity);
        decimal totalAmount = itemList.Sum(x => x.TotalLineAmount);

        txtTotalQty.Text = $"{totalQty:N0} Cái";
        txtTotalAmount.Text = $"{totalAmount:N0} đ";
        txtAmountInWords = txtAmountInWords ?? new TextBlock();
        txtAmountInWords.Text = ConvertAmountToVietnameseWords((long)totalAmount);
    }

    private void BtnPrint_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var printDlg = new PrintDialog();
            if (printDlg.ShowDialog() == true)
            {
                // In toàn bộ vùng giấy pnlPrintArea
                printDlg.PrintVisual(pnlPrintArea, $"Phiếu_{txtCode.Text}");
                MessageBox.Show("Lệnh in đã được gửi đến máy in thành công!", "Đã in phiếu", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Lỗi trong quá trình gửi lệnh in:\n{ex.Message}", "Lỗi in ấn", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    // Chuyển đổi số tiền thành chữ tiếng Việt chuẩn kế toán
    private static string ConvertAmountToVietnameseWords(long number)
    {
        if (number == 0) return "Không đồng chẵn.";
        if (number < 0) return "Âm " + ConvertAmountToVietnameseWords(-number);

        string[] units = { "", "nghìn", "triệu", "tỷ", "nghìn tỷ", "triệu tỷ" };
        string[] digits = { "không", "một", "hai", "ba", "bốn", "năm", "sáu", "bảy", "tám", "chín" };

        string result = "";
        int unitIndex = 0;

        while (number > 0)
        {
            int block = (int)(number % 1000);
            number /= 1000;

            if (block > 0)
            {
                string blockText = ReadThreeDigits(block, digits, number > 0);
                result = blockText + " " + units[unitIndex] + " " + result;
            }
            unitIndex++;
        }

        result = result.Trim();
        if (string.IsNullOrWhiteSpace(result)) return "Không đồng chẵn.";

        // Viết hoa chữ cái đầu và thêm đuôi "đồng chẵn."
        result = char.ToUpper(result[0]) + result[1..] + " đồng chẵn.";
        return result.Replace("  ", " ");
    }

    private static string ReadThreeDigits(int number, string[] digits, bool hasHigher)
    {
        int h = number / 100;
        int t = (number % 100) / 10;
        int u = number % 10;
        string res = "";

        if (h > 0 || hasHigher)
        {
            res += digits[h] + " trăm ";
        }

        if (t > 1)
        {
            res += digits[t] + " mươi ";
            if (u == 1) res += "mốt ";
            else if (u == 5) res += "lăm ";
            else if (u > 0) res += digits[u] + " ";
        }
        else if (t == 1)
        {
            res += "mười ";
            if (u == 5) res += "lăm ";
            else if (u > 0) res += digits[u] + " ";
        }
        else if (t == 0 && u > 0)
        {
            if (h > 0 || hasHigher) res += "lẻ ";
            res += digits[u] + " ";
        }

        return res.Trim();
    }
}

