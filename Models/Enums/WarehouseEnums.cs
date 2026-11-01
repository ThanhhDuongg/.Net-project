namespace NetProject.Models.Enums;

public enum ReceiptType
{
    IMPORT,
    EXPORT
}

public enum ReceiptStatus
{
    DRAFT,
    COMPLETED,
    CANCELLED
}

/// <summary>
/// Loại biến động tồn kho. Mỗi thao tác nhập/xuất sinh một dòng lịch sử bất biến.
/// </summary>
public enum InventoryTransactionType
{
    IMPORT,
    EXPORT,
    ADJUSTMENT
}
