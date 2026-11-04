using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using QRCoder;

namespace FashionWarehouse.WpfApp.Views;

public partial class MobileScanQrWindow : Window
{
    private static readonly HttpClient HttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5116/"),
        Timeout = TimeSpan.FromSeconds(3)
    };

    private string _localUrl = "http://10.51.247.73:5116/scan";
    private string _cloudflareUrl = "https://def-impressive-occupied-kelkoo.trycloudflare.com/scan";
    private string _currentActiveUrl = string.Empty;

    public MobileScanQrWindow()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            await LoadNetworkInfoAsync();
            RenderQrCode();
        };
    }

    private async Task LoadNetworkInfoAsync()
    {
        try
        {
            var info = await HttpClient.GetFromJsonAsync<ScannerInfoResponse>("api/system/scanner-info");
            if (info != null)
            {
                if (!string.IsNullOrWhiteSpace(info.LocalScanUrl))
                    _localUrl = info.LocalScanUrl;

                if (!string.IsNullOrWhiteSpace(info.CloudflareScanUrl))
                    _cloudflareUrl = info.CloudflareScanUrl;
            }
        }
        catch { }

        // Fallback đọc file nếu có
        try
        {
            if (File.Exists("tunnel_url.txt"))
            {
                var content = File.ReadAllText("tunnel_url.txt").Trim();
                if (!string.IsNullOrWhiteSpace(content))
                    _cloudflareUrl = content;
            }
        }
        catch { }
    }

    private void RenderQrCode()
    {
        bool isHttps = rbHttps?.IsChecked == true;
        _currentActiveUrl = isHttps ? _cloudflareUrl : _localUrl;

        if (txtUrlDisplay != null)
            txtUrlDisplay.Text = _currentActiveUrl;

        if (lblModeHint != null)
        {
            lblModeHint.Text = isHttps
                ? "Hỗ trợ mọi mạng (4G/5G/Wi-Fi) • Mở camera 100% bảo mật"
                : "Yêu cầu điện thoại kết nối CHUNG Wi-Fi với máy tính này";
            lblModeHint.Foreground = isHttps
                ? System.Windows.Media.Brushes.DarkGreen
                : System.Windows.Media.Brushes.DarkOrange;
        }

        try
        {
            using var qrGenerator = new QRCodeGenerator();
            using var qrCodeData = qrGenerator.CreateQrCode(_currentActiveUrl, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeBytes = qrCode.GetGraphic(15);

            var bitmap = new BitmapImage();
            using (var stream = new MemoryStream(qrCodeBytes))
            {
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = stream;
                bitmap.EndInit();
            }
            bitmap.Freeze();

            imgQrCode.Source = bitmap;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Không thể tạo mã QR: {ex.Message}", "Lỗi QR", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ConnectionMode_Changed(object sender, RoutedEventArgs e)
    {
        if (IsLoaded)
        {
            RenderQrCode();
        }
    }

    private void BtnCopyUrl_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentActiveUrl))
        {
            Clipboard.SetText(_currentActiveUrl);
            MessageBox.Show($"Đã sao chép đường dẫn:\n{_currentActiveUrl}\n\nBạn có thể dán vào trình duyệt di động!", "Đã sao chép", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void BtnOpenBrowser_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_currentActiveUrl))
        {
            try
            {
                Process.Start(new ProcessStartInfo(_currentActiveUrl) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể mở trình duyệt: {ex.Message}", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private class ScannerInfoResponse
    {
        public string? LocalIp { get; set; }
        public string? LocalScanUrl { get; set; }
        public string? CloudflareScanUrl { get; set; }
    }
}

