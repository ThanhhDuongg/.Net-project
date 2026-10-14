using System.Collections.Generic;
using System.Windows.Controls;

namespace FashionWarehouse.WpfApp.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();

        // Dữ liệu giả lập để test ngay giao diện
        dgAlerts.ItemsSource = new List<object>
        {
            new { SKU = "POLO01-BLK-M", ProductName = "Áo Polo Nam Classic Cotton", ColorName = "Đen", SizeName = "M", LocationCode = "KHO-A-01", QuantityOnHand = 3, SafetyStock = 10 },
            new { SKU = "POLO01-WHT-L", ProductName = "Áo Polo Nam Classic Cotton", ColorName = "Trắng", SizeName = "L", LocationCode = "KHO-A-01", QuantityOnHand = 5, SafetyStock = 15 },
            new { SKU = "SM-OXF-BLU-S", ProductName = "Áo Sơ Mi Oxford Dài Tay", ColorName = "Xanh", SizeName = "S", LocationCode = "KHO-B-01", QuantityOnHand = 2, SafetyStock = 10 }
        };
    }
}
