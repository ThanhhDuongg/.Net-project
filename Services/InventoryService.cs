using NetProject.DAL;
using NetProject.DTOs;
using NetProject.Models.Entities;
using NetProject.Models.Enums;
using NetProject.Services.Exceptions;

namespace NetProject.Services;

public interface IInventoryService
{
    Task<InventoryTransactionResultDto> ProcessAsync(
        InventoryTransactionRequestDto request,
        CancellationToken cancellationToken = default);

    Task<InventoryStatusDto?> FindByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// BLL nhập/xuất kho. Toàn bộ Receipt, ReceiptDetail, Inventory và InventoryTransaction
/// được lưu bằng IUnitOfWork trong cùng transaction. Không gọi DbContext trực tiếp.
/// </summary>
public sealed class InventoryService : IInventoryService
{
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<InventoryTransactionResultDto> ProcessAsync(
        InventoryTransactionRequestDto request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);
        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var pending = new List<PendingInventoryChange>();
        try
        {
            var isImport = request.TransactionType == ReceiptType.IMPORT;
            var receipt = CreateReceipt(request);

            // Gom nhóm cùng Variant + Location để một inventory chỉ cập nhật một lần.
            foreach (var group in request.Items.GroupBy(x => new { x.VariantId, x.LocationId }))
            {
                var expectedVersions = group
                    .Where(x => x.ExpectedRowVersion.HasValue)
                    .Select(x => x.ExpectedRowVersion!.Value)
                    .Distinct()
                    .ToList();

                if (expectedVersions.Count > 1)
                {
                    throw new InventoryConcurrencyException(0);
                }

                var inventory = await _unitOfWork.Inventories.FindByVariantAndLocationAsync(
                    group.Key.VariantId,
                    group.Key.LocationId,
                    asNoTracking: false,
                    cancellationToken);

                if (inventory is null)
                {
                    if (isImport)
                    {
                        inventory = new Inventory
                        {
                            VariantId = group.Key.VariantId,
                            LocationId = group.Key.LocationId,
                            QuantityOnHand = 0,
                            SafetyStock = 10,
                            RowVersion = DateTime.UtcNow
                        };
                        _unitOfWork.Inventories.Add(inventory);
                    }
                    else
                    {
                        throw new InventoryNotFoundException(group.Key.VariantId, group.Key.LocationId);
                    }
                }

                if (expectedVersions.Count == 1 && inventory.RowVersion != expectedVersions[0])
                {
                    throw new InventoryConcurrencyException(inventory.InventoryId);
                }

                var quantity = group.Sum(x => x.Quantity);
                var before = inventory.QuantityOnHand;
                var after = isImport ? checked(before + quantity) : checked(before - quantity);

                if (!isImport && after < 0)
                {
                    var reference = inventory.Variant?.SKU ?? $"VariantId={inventory.VariantId}";
                    throw new InsufficientInventoryException(reference, before, quantity);
                }

                pending.Add(new PendingInventoryChange(inventory, quantity, before, after));
            }

            // Thao tác mutate chỉ xảy ra sau khi tất cả điều kiện nghiệp vụ được xác nhận.
            foreach (var change in pending)
            {
                change.Inventory.QuantityOnHand = change.After;
                if (isImport)
                {
                    change.Inventory.LastImportDate = DateTime.UtcNow;
                }

                _unitOfWork.InventoryTransactions.Add(new InventoryTransaction
                {
                    InventoryId = change.Inventory.InventoryId,
                    VariantId = change.Inventory.VariantId,
                    LocationId = change.Inventory.LocationId,
                    Receipt = receipt,
                    TransactionType = isImport
                        ? InventoryTransactionType.IMPORT
                        : InventoryTransactionType.EXPORT,
                    QuantityChange = isImport ? change.Quantity : -change.Quantity,
                    QuantityBefore = change.Before,
                    QuantityAfter = change.After,
                    ReferenceNo = request.TransactionCode,
                    Notes = request.Notes,
                    OccurredAt = DateTime.UtcNow,
                    PerformedByUserId = request.UserId
                });
            }

            _unitOfWork.Receipts.Add(receipt);
            _unitOfWork.Receipts.AddDetails(receipt.Details);

            // Provider sẽ phát hiện UPDATE không khớp RowVersion và ném concurrency exception riêng;
            // adapter EF chuyển ngoại lệ đó về InventoryConcurrencyException ở tầng DAL.
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            return new InventoryTransactionResultDto
            {
                ReceiptId = receipt.ReceiptId,
                ReceiptCode = receipt.ReceiptCode,
                CompletedAt = DateTime.UtcNow,
                Balances = pending.Select(x => new InventoryBalanceDto
                {
                    InventoryId = x.Inventory.InventoryId,
                    VariantId = x.Inventory.VariantId,
                    LocationId = x.Inventory.LocationId,
                    QuantityOnHand = x.After,
                    RowVersion = x.Inventory.RowVersion
                }).ToArray()
            };
        }
        catch
        {
            foreach (var change in pending)
            {
                change.Inventory.QuantityOnHand = change.Before;
            }
            await _unitOfWork.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<InventoryStatusDto?> FindByBarcodeAsync(
        string barcode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return null;
        }

        var trimmed = barcode.Trim();
        return await Task.FromResult(_unitOfWork.Reports.GetInventoryStatuses()
            .FirstOrDefault(x => x.Barcode == trimmed || x.SKU == trimmed));
    }

    private static Receipt CreateReceipt(InventoryTransactionRequestDto request)
    {
        return new Receipt
        {
            ReceiptCode = request.TransactionCode.Trim(),
            ReceiptType = request.TransactionType,
            ReceiptDate = DateTime.UtcNow,
            SupplierId = request.SupplierId,
            CustomerName = request.CustomerName?.Trim(),
            Notes = request.Notes?.Trim(),
            UserId = request.UserId,
            Status = ReceiptStatus.COMPLETED,
            TotalAmount = request.Items.Sum(x => x.Quantity * x.UnitPrice),
            Details = request.Items.Select(item => new ReceiptDetail
            {
                VariantId = item.VariantId,
                LocationId = item.LocationId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice
            }).ToList()
        };
    }

    private static void ValidateRequest(InventoryTransactionRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.TransactionCode))
        {
            throw new ArgumentException("Mã giao dịch là bắt buộc.", nameof(request));
        }

        if (request.UserId <= 0)
        {
            throw new ArgumentException("Người thực hiện không hợp lệ.", nameof(request));
        }

        if (request.Items.Count == 0)
        {
            throw new ArgumentException("Phiếu phải có ít nhất một dòng hàng.", nameof(request));
        }

        if (request.Items.Any(x => x.VariantId <= 0 || x.LocationId <= 0 || x.Quantity <= 0 || x.UnitPrice < 0))
        {
            throw new ArgumentException("Dòng hàng chứa dữ liệu không hợp lệ.", nameof(request));
        }
    }

    private sealed record PendingInventoryChange(Inventory Inventory, int Quantity, int Before, int After);
}
