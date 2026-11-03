using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using NetProject.DAL;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;
using NetProject.Models.Enums;
using NetProject.Services.Exceptions;

namespace NetProject.DAL;

/// <summary>
/// EF Core implementation của IUnitOfWork. BLL chỉ thấy interface,
/// adapter chịu trách nhiệm mở transaction, commit/rollback và chuyển DbUpdateConcurrencyException.
/// </summary>
public sealed class EfUnitOfWork : IUnitOfWork
{
    private readonly WarehouseDbContext _context;
    private IDbContextTransaction? _transaction;

    public EfUnitOfWork(WarehouseDbContext context)
    {
        _context = context;
        Inventories = new EfInventoryRepository(context);
        InventoryTransactions = new EfInventoryTransactionRepository(context);
        Receipts = new EfReceiptRepository(context);
        Reports = new EfReportRepository(context);
    }

    public IInventoryRepository Inventories { get; }
    public IInventoryTransactionRepository InventoryTransactions { get; }
    public IReceiptRepository Receipts { get; }
    public IReportRepository Reports { get; }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("Unit of Work đã mở transaction.");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            var entry = ex.Entries.FirstOrDefault(x => x.Entity is Inventory);
            var inventory = (Inventory?)entry?.Entity;
            throw new InventoryConcurrencyException(inventory?.InventoryId ?? 0);
        }
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("Chưa mở transaction.");
        }

        await _transaction.CommitAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        await _transaction.RollbackAsync(cancellationToken);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        await _context.DisposeAsync();
    }
}

public sealed class EfInventoryRepository : IInventoryRepository
{
    private readonly WarehouseDbContext _context;

    public EfInventoryRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public Task<Inventory?> FindAsync(object id, CancellationToken cancellationToken = default) =>
        _context.Inventories.FirstOrDefaultAsync(x => x.InventoryId == (int)id, cancellationToken);

    public IQueryable<Inventory> AsNoTracking() => _context.Inventories.AsNoTracking();
    public IQueryable<Inventory> AsTracking() => _context.Inventories;
    public void Add(Inventory inventory) => _context.Inventories.Add(inventory);

    public Task<Inventory?> FindByVariantAndLocationAsync(
        int variantId,
        int locationId,
        bool asNoTracking,
        CancellationToken cancellationToken = default)
    {
        var query = asNoTracking ? AsNoTracking() : AsTracking();
        return query.FirstOrDefaultAsync(x => x.VariantId == variantId && x.LocationId == locationId, cancellationToken);
    }
}

public sealed class EfInventoryTransactionRepository : IInventoryTransactionRepository
{
    private readonly WarehouseDbContext _context;

    public EfInventoryTransactionRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public IQueryable<InventoryTransaction> AsNoTracking() =>
        _context.InventoryTransactions.AsNoTracking();

    public void Add(InventoryTransaction transaction) => _context.InventoryTransactions.Add(transaction);
}

public sealed class EfReceiptRepository : IReceiptRepository
{
    private readonly WarehouseDbContext _context;

    public EfReceiptRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public IQueryable<Receipt> AsNoTracking() => _context.Receipts.AsNoTracking();

    public void Add(Receipt receipt) => _context.Receipts.Add(receipt);

    public void AddDetails(IEnumerable<ReceiptDetail> details) =>
        _context.ReceiptDetails.AddRange(details);
}

/// <summary>
/// Các truy vấn lớn được thi hành ở hạ tầng IQueryable, chỉ materialize ở lớp report/dashboard,
/// giúp giới hạn bộ nhớ khi xuất dữ liệu Excel lớn.
/// </summary>
public sealed class EfReportRepository : IReportRepository
{
    private readonly WarehouseDbContext _context;

    public EfReportRepository(WarehouseDbContext context)
    {
        _context = context;
    }

    public IQueryable<InventoryStatusDto> GetInventoryStatuses() =>
        from inventory in _context.Inventories.AsNoTracking()
        join variant in _context.ProductVariants.AsNoTracking() on inventory.VariantId equals variant.VariantId
        join product in _context.Products.AsNoTracking() on variant.ProductId equals product.ProductId
        join color in _context.Colors.AsNoTracking() on variant.ColorId equals color.ColorId
        join size in _context.Sizes.AsNoTracking() on variant.SizeId equals size.SizeId
        join location in _context.WarehouseLocations.AsNoTracking() on inventory.LocationId equals location.LocationId
        select new InventoryStatusDto
        {
            VariantId = variant.VariantId,
            LocationId = location.LocationId,
            RowVersion = inventory.RowVersion,
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            ProductName = product.ProductName,
            ColorName = color.ColorName,
            SizeName = size.SizeName,
            LocationCode = location.LocationCode,
            QuantityOnHand = inventory.QuantityOnHand,
            SafetyStock = inventory.SafetyStock,
            CostPrice = variant.CostPrice
        };

    public IQueryable<InventoryReportRowDto> GetInventoryReportRows() =>
        from inventory in _context.Inventories.AsNoTracking()
        join variant in _context.ProductVariants.AsNoTracking() on inventory.VariantId equals variant.VariantId
        join product in _context.Products.AsNoTracking() on variant.ProductId equals product.ProductId
        join color in _context.Colors.AsNoTracking() on variant.ColorId equals color.ColorId
        join size in _context.Sizes.AsNoTracking() on variant.SizeId equals size.SizeId
        join location in _context.WarehouseLocations.AsNoTracking() on inventory.LocationId equals location.LocationId
        where inventory.QuantityOnHand > 0
        orderby location.LocationCode, product.ProductName, variant.SKU
        select new InventoryReportRowDto
        {
            SKU = variant.SKU,
            Barcode = variant.Barcode,
            ProductName = product.ProductName,
            ColorName = color.ColorName,
            SizeName = size.SizeName,
            LocationCode = location.LocationCode,
            QuantityOnHand = inventory.QuantityOnHand,
            SafetyStock = inventory.SafetyStock,
            CostPrice = variant.CostPrice
        };

    public IQueryable<DashboardChartPointDto> GetDailyMovements(DateOnly from, DateOnly to)
    {
        var start = from.ToDateTime(TimeOnly.MinValue);
        var endExclusive = to.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var transactions = _context.InventoryTransactions.AsNoTracking()
            .Where(txn => txn.OccurredAt >= start && txn.OccurredAt < endExclusive)
            .Select(txn => new
            {
                txn.OccurredAt,
                txn.TransactionType,
                txn.QuantityChange
            })
            .ToList();

        var dailyMovements = transactions
            .GroupBy(txn => txn.OccurredAt.Date)
            .OrderBy(daily => daily.Key)
            .Select(daily => new DashboardChartPointDto
            {
                Date = daily.Key,
                Imports = daily.Where(x => x.TransactionType == InventoryTransactionType.IMPORT)
                    .Sum(x => x.QuantityChange),
                Exports = daily.Where(x => x.TransactionType == InventoryTransactionType.EXPORT)
                    .Sum(x => -x.QuantityChange)
            })
            .ToList();

        return dailyMovements.AsQueryable();
    }

    /// <summary>
    /// Truy vấn lịch sử giao dịch kho. Join InventoryTransactions với ProductVariants, Products,
    /// Colors, Sizes, WarehouseLocations. Lọc theo khoảng ngày nếu có, sắp xếp giảm dần thời gian.
    /// </summary>
    public IQueryable<TransactionHistoryDto> GetTransactionHistory(DateOnly? from, DateOnly? to)
    {
        var query = from txn in _context.InventoryTransactions.AsNoTracking()
            join variant in _context.ProductVariants.AsNoTracking() on txn.VariantId equals variant.VariantId
            join product in _context.Products.AsNoTracking() on variant.ProductId equals product.ProductId
            join color in _context.Colors.AsNoTracking() on variant.ColorId equals color.ColorId
            join size in _context.Sizes.AsNoTracking() on variant.SizeId equals size.SizeId
            join location in _context.WarehouseLocations.AsNoTracking() on txn.LocationId equals location.LocationId
            select new { txn, variant, product, color, size, location };

        // Lọc theo ngày bắt đầu nếu có
        if (from.HasValue)
        {
            var start = from.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.txn.OccurredAt >= start);
        }

        // Lọc theo ngày kết thúc nếu có (lấy hết ngày cuối)
        if (to.HasValue)
        {
            var endExclusive = to.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.txn.OccurredAt < endExclusive);
        }

        return query
            .OrderByDescending(x => x.txn.OccurredAt)
            .Select(x => new TransactionHistoryDto
            {
                TransactionId = x.txn.InventoryTransactionId,
                TransactionType = x.txn.TransactionType.ToString(),
                ReferenceNo = x.txn.ReferenceNo,
                SKU = x.variant.SKU,
                ProductName = x.product.ProductName,
                ColorName = x.color.ColorName,
                SizeName = x.size.SizeName,
                LocationCode = x.location.LocationCode,
                QuantityChange = x.txn.QuantityChange,
                QuantityBefore = x.txn.QuantityBefore,
                QuantityAfter = x.txn.QuantityAfter,
                OccurredAt = x.txn.OccurredAt,
                Notes = x.txn.Notes
            });
    }
}



