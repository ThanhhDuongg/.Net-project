using System;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FashionWarehouse.WpfApp.Services;

/// <summary>
/// Bộ tạo đồ họa mã vạch chuẩn Code 128 (ISO/IEC 15417)
/// Hỗ trợ Code 128 Auto (Code 128C cho chuỗi số, Code 128B cho ký tự ASCII)
/// Đảm bảo chuẩn Modulo 103 checksum và Quiet Zone theo tiêu chuẩn quốc tế.
/// </summary>
public static class BarcodeVisualGenerator
{
    // Bảng 107 mẫu chuẩn Code 128 (0 - 106).
    // Mỗi ký tự 0-105 gồm 6 phần tử (vạch-khoảng trắng xen kẽ), tổng độ rộng 11 modules.
    // Ký tự STOP (106) gồm 7 phần tử (vạch-trắng xen kẽ), tổng độ rộng 13 modules.
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

    /// <summary>
    /// Mã hóa chuỗi đầu vào thành chuỗi độ rộng vạch chuẩn Code 128 (Start -> Dữ liệu -> Modulo 103 -> Stop)
    /// </summary>
    public static string EncodeCode128(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) text = "000000";

        var sb = new StringBuilder();
        bool isAllDigits = text.All(char.IsDigit);
        bool useCodeC = isAllDigits && (text.Length % 2 == 0);

        if (useCodeC)
        {
            // Code Set C: nén 2 chữ số thành 1 ký tự mã vạch -> vạch to rõ, máy quét đọc cực nhanh
            int startCode = 105; // Start C
            sb.Append(Code128Patterns[startCode]);
            int checksum = startCode;
            int weight = 1;

            for (int i = 0; i < text.Length; i += 2)
            {
                int val = (text[i] - '0') * 10 + (text[i + 1] - '0');
                sb.Append(Code128Patterns[val]);
                checksum += val * weight;
                weight++;
            }

            int checkDigit = checksum % 103;
            sb.Append(Code128Patterns[checkDigit]);
            sb.Append(Code128Patterns[106]); // Stop pattern
        }
        else
        {
            // Code Set B: ký tự ASCII tiêu chuẩn (32 đến 126)
            int startCode = 104; // Start B
            sb.Append(Code128Patterns[startCode]);
            int checksum = startCode;
            int weight = 1;

            for (int i = 0; i < text.Length; i++)
            {
                int ascii = text[i];
                int val = ascii - 32;
                if (val < 0 || val > 105) val = 0;

                sb.Append(Code128Patterns[val]);
                checksum += val * weight;
                weight++;
            }

            int checkDigit = checksum % 103;
            sb.Append(Code128Patterns[checkDigit]);
            sb.Append(Code128Patterns[106]); // Stop pattern
        }

        return sb.ToString();
    }

    /// <summary>
    /// Tạo BitmapSource sắc nét tuyệt đối (Pixel-Perfect) không bị mờ nhòe do khử răng cưa.
    /// Tối ưu hoàn hảo cho camera điện thoại quét trực tiếp trên màn hình máy tính.
    /// </summary>
    public static BitmapSource CreateBarcodeBitmap(string code, int moduleWidth = 2, int height = 48, int quietModules = 14)
    {
        string pattern = EncodeCode128(code);
        if (string.IsNullOrEmpty(pattern))
        {
            var fallback = new WriteableBitmap(100, height, 96, 96, PixelFormats.Bgra32, null);
            fallback.Freeze();
            return fallback;
        }

        // Tính tổng số module
        int dataModules = 0;
        foreach (char c in pattern) dataModules += (c - '0');

        int totalModules = dataModules + (quietModules * 2);
        int pixelWidth = totalModules * moduleWidth;
        int pixelHeight = Math.Max(24, height);

        int[] pixels = new int[pixelWidth * pixelHeight];
        // Điền toàn bộ nền trắng (0xFFFFFFFF)
        Array.Fill(pixels, unchecked((int)0xFFFFFFFF));

        int currentX = quietModules * moduleWidth;
        bool isBar = true;

        foreach (char c in pattern)
        {
            int barUnits = c - '0';
            int barWidthPx = barUnits * moduleWidth;

            if (isBar)
            {
                for (int y = 0; y < pixelHeight; y++)
                {
                    int rowOffset = y * pixelWidth;
                    for (int x = 0; x < barWidthPx; x++)
                    {
                        pixels[rowOffset + currentX + x] = unchecked((int)0xFF000000); // Đen thuần 100%
                    }
                }
            }

            currentX += barWidthPx;
            isBar = !isBar;
        }

        var bitmap = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixels, pixelWidth * 4, 0);
        bitmap.Freeze();
        return bitmap;
    }

    /// <summary>
    /// Tạo DrawingImage dạng vector cho in ấn tem nhãn hóa đơn
    /// </summary>
    public static DrawingImage CreateBarcodeImage(string code, double width = 230, double height = 45)
    {
        var group = new DrawingGroup();
        using var dc = group.Open();

        // Nền trắng
        dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, width, height));

        string pattern = EncodeCode128(code);
        if (string.IsNullOrEmpty(pattern))
        {
            return new DrawingImage(group);
        }

        int totalUnits = 0;
        foreach (char c in pattern) totalUnits += (c - '0');

        // Ít nhất 12 modules Quiet Zone mỗi bên
        int quietZoneUnits = 14;
        int fullUnits = totalUnits + (quietZoneUnits * 2);

        double unitWidth = width / fullUnits;
        double currentX = quietZoneUnits * unitWidth;

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
}
