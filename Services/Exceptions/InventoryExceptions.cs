namespace NetProject.Services.Exceptions;

public sealed class InventoryNotFoundException : InvalidOperationException
{
    public InventoryNotFoundException(int variantId, int locationId)
        : base($"Không tìm thấy tồn kho cho VariantId={variantId}, LocationId={locationId}.") { }
}

public sealed class InsufficientInventoryException : InvalidOperationException
{
    public InsufficientInventoryException(string reference, int available, int requested)
        : base($"Tồn kho không đủ cho {reference}. Hiện có {available}, yêu cầu xuất {requested}.") { }
}

/// <summary>Được ném ra khi RowVersion do client gửi đã cũ so với dữ liệu hiện tại.</summary>
public sealed class InventoryConcurrencyException : InvalidOperationException
{
    public InventoryConcurrencyException(int inventoryId)
        : base($"Tồn kho #{inventoryId} vừa được người khác thay đổi. Vui lòng tải lại tồn kho và thử lại.") { }
}
