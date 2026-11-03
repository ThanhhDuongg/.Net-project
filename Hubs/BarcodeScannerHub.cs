using Microsoft.AspNetCore.SignalR;

namespace NetProject.Hubs;

public class BarcodeScannerHub : Hub
{
    /// <summary>
    /// Được gọi từ trang Web trên điện thoại di động khi camera đọc được mã vạch
    /// </summary>
    public async Task SendScannedBarcode(string barcode)
    {
        if (!string.IsNullOrWhiteSpace(barcode))
        {
            // Bắn tín hiệu đến tất cả ứng dụng WPF Desktop đang lắng nghe
            await Clients.All.SendAsync("ReceiveScannedBarcode", barcode.Trim());
        }
    }
}

