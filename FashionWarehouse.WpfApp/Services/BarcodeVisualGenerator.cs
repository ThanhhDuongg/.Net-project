using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Media;

namespace FashionWarehouse.WpfApp.Services;

/// <summary>
/// Bộ tạo đồ họa mã vạch Code 128 chuẩn cho tem nhãn hàng hóa
/// </summary>
public static class BarcodeVisualGenerator
{
    private static readonly string[] Code128Patterns = new[]
    {
        "212222", "222122", "222221", "121223", "121322", "131222", "122213", "122312", "132212", "221213", // 0-9
        "221312", "231212", "112232", "122132", "122231", "113222", "123122", "123221", "223211", "221132", // 10-19
        "221231", "213212", "223112", "312131", "311222", "321122", "321221", "312212", "322112", "322211", // 20-29
        "212123", "212321", "232121", "111323", "131123", "131321", "112313", "132113", "132311", "211313", // 30-39
        "231113", "231311", "112133", "112331", "132131", "113123", "113321", "133121", "313121", "211331", // 40-49
        "231131", "213113", "213311", "213131", "311123", "311321", "331121", "312113", "312311", "332111", // 50-59
        "314111", "221411", "431111", "111224", "111422", "121124", "121421", "141122", "141221", "112214", // 60-69
        "112412", "122114", "122411", "142112", "142211", "241211", "221114", "413111", "241112", "134111", // 70-79
        "111242", "121142", "121241", "114212", "124112", "124211", "411212", "421112", "421211", "212141", // 80-89
        "214121", "412121", "111143", "111341", "131141", "114113", "114311", "411113", "411311", "113141", // 90-99
        "114131", "311141", "411131", "211412", "211214", "211232", "2331112"                                 // 100-106 (Stop)
    };

    public static DrawingImage CreateBarcodeImage(string code, double width = 190, double height = 45)
    {
        var group = new DrawingGroup();
        using var dc = group.Open();

        // Nền trắng
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        string pattern = EncodeCode128B(code);
        if (string.IsNullOrEmpty(pattern))
        {
            return new DrawingImage(group);
        }

        // Tính độ rộng của mỗi vạch
        int totalUnits = 0;
        foreach (char c in pattern) totalUnits += (c - '0');

        double unitWidth = width / (totalUnits + 10);
        double startX = (width - (totalUnits * unitWidth)) / 2;
        double currentX = startX;

        bool isBar = true;
        foreach (char c in pattern)
        {
            int barUnits = c - '0';
            double barWidth = barUnits * unitWidth;

            if (isBar)
            {
                dc.DrawRectangle(Brushes.Black, null, new Rect(currentX, 2, barWidth, height - 4));
            }

            currentX += barWidth;
            isBar = !isBar;
        }

        return new DrawingImage(group);
    }

    private static string EncodeCode128B(string text)
    {
        if (string.IsNullOrEmpty(text)) text = "000000";

        var sb = new StringBuilder();
        int startCode = 104; // Start B
        sb.Append(Code128Patterns[startCode]);

        int checksum = startCode;
        for (int i = 0; i < text.Length; i++)
        {
            int ascii = text[i];
            int codeValue = ascii - 32;
            if (codeValue < 0 || codeValue > 105) codeValue = 0;

            sb.Append(Code128Patterns[codeValue]);
            checksum += codeValue * (i + 1);
        }

        int checkDigit = checksum % 103;
        sb.Append(Code128Patterns[checkDigit]);
        sb.Append(Code128Patterns[106]); // Stop pattern

        return sb.ToString();
    }
}

