using NetProject.Models.Enums;

namespace NetProject.DTOs;

public class LoginRequestDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RoleName { get; set; } = string.Empty;
}

public class CreateProductMatrixDto
{
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public decimal BasePrice { get; set; }
    public string Unit { get; set; } = "Cái";
    public string? Description { get; set; }
    public List<int> ColorIds { get; set; } = new();
    public List<int> SizeIds { get; set; } = new();
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
}

public class GeneratedVariantDto
{
    public int ProductId { get; set; }
    public int ColorId { get; set; }
    public string ColorName { get; set; } = string.Empty;
    public int SizeId { get; set; }
    public string SizeName { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public decimal CostPrice { get; set; }
    public decimal SalePrice { get; set; }
}

public class ReceiptItemDto
{
    public int VariantId { get; set; }
    public int LocationId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}

public class CreateReceiptDto
{
    public string ReceiptCode { get; set; } = string.Empty;
    public ReceiptType ReceiptType { get; set; }
    public int? SupplierId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public int UserId { get; set; }
    public List<ReceiptItemDto> Items { get; set; } = new();
}

public class InventoryStatusDto
{
    public int LocationId { get; set; }
    public DateTime RowVersion { get; set; }
    public int VariantId { get; set; }
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int SafetyStock { get; set; }
    public decimal CostPrice { get; set; }
    public bool IsLowStock => QuantityOnHand <= SafetyStock;
}

/// <summary>Yêu cầu một giao dịch nhập/xuất kho từ màn quét mã vạch hoặc API.</summary>
public class InventoryTransactionRequestDto
{
    public string TransactionCode { get; set; } = string.Empty;
    public ReceiptType TransactionType { get; set; }
    public int UserId { get; set; }
    public int? SupplierId { get; set; }
    public string? CustomerName { get; set; }
    public string? Notes { get; set; }
    public List<InventoryTransactionItemDto> Items { get; set; } = new();
}

public class InventoryTransactionItemDto
{
    public int VariantId { get; set; }
    public int LocationId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Token RowVersion do client đọc cùng dữ liệu tồn kho. Khi khác token hiện tại, thao tác bị từ chối.
    /// </summary>
    public DateTime? ExpectedRowVersion { get; set; }
}

public class InventoryTransactionResultDto
{
    public int ReceiptId { get; set; }
    public string ReceiptCode { get; set; } = string.Empty;
    public DateTime CompletedAt { get; set; }
    public IReadOnlyList<InventoryBalanceDto> Balances { get; set; } = Array.Empty<InventoryBalanceDto>();
}

public class InventoryBalanceDto
{
    public int InventoryId { get; set; }
    public int VariantId { get; set; }
    public int LocationId { get; set; }
    public int QuantityOnHand { get; set; }
    public DateTime RowVersion { get; set; }
}

public class DashboardSummaryDto
{
    public int TotalSkus { get; set; }
    public int TotalQuantity { get; set; }
    public int LowStockSkuCount { get; set; }
    public decimal InventoryValue { get; set; }
    public IReadOnlyList<DashboardChartPointDto> DailyMovements { get; set; } = Array.Empty<DashboardChartPointDto>();
}

public class DashboardChartPointDto
{
    public DateTime Date { get; set; }
    public int Imports { get; set; }
    public int Exports { get; set; }
}

public class InventoryReportRowDto
{
    public string SKU { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int QuantityOnHand { get; set; }
    public int SafetyStock { get; set; }
    public decimal CostPrice { get; set; }
    public decimal InventoryValue => QuantityOnHand * CostPrice;
}

/// <summary>Dòng lịch sử giao dịch nhập/xuất/điều chỉnh tồn kho.</summary>
public class TransactionHistoryDto
{
    public long TransactionId { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string ReferenceNo { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string ColorName { get; set; } = string.Empty;
    public string SizeName { get; set; } = string.Empty;
    public string LocationCode { get; set; } = string.Empty;
    public int QuantityChange { get; set; }
    public int QuantityBefore { get; set; }
    public int QuantityAfter { get; set; }
    public DateTime OccurredAt { get; set; }
    public string? Notes { get; set; }
}

public class ProductListItemDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal BasePrice { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int VariantCount { get; set; }
}

public class ProductDetailDto
{
    public int ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public List<GeneratedVariantDto> Variants { get; set; } = new();
}

public class CategoryDto
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int ProductCount { get; set; }
}

public class CreateCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UserListItemDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public bool IsActive { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int TotalReceiptsCreated { get; set; }
}

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; } = true;
}

public class UpdateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public int RoleId { get; set; }
    public bool IsActive { get; set; }
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = string.Empty;
}

public class RoleItemDto
{
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int UserCount { get; set; }
}

public class RolePermissionMatrixDto
{
    public string ModuleName { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public bool AdminAllowed { get; set; }
    public bool WarehouseKeeperAllowed { get; set; }
    public string Description { get; set; } = string.Empty;
}
