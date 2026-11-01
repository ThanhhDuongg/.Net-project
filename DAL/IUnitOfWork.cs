using NetProject.DTOs;
using NetProject.Models.Entities;

namespace NetProject.DAL;

/// <summary>
/// Repository thuần query, không SaveChanges. Mọi thao tác ghi phải thông qua Unit of Work
/// để tồn kho và nhật ký được commit trong cùng một transaction.
/// </summary>
public interface IReadRepository<TEntity>
{
    Task<TEntity?> FindAsync(object id, CancellationToken cancellationToken = default);
    IQueryable<TEntity> AsNoTracking();
}

public interface IInventoryRepository : IReadRepository<Inventory>
{
    IQueryable<Inventory> AsTracking();

    Task<Inventory?> FindByVariantAndLocationAsync(
        int variantId,
        int locationId,
        bool asNoTracking,
        CancellationToken cancellationToken = default);
}

public interface IInventoryTransactionRepository
{
    IQueryable<InventoryTransaction> AsNoTracking();
    void Add(InventoryTransaction transaction);
}

public interface IReceiptRepository
{
    IQueryable<Receipt> AsNoTracking();
    void Add(Receipt receipt);
    void AddDetails(IEnumerable<ReceiptDetail> details);
}

public interface IReportRepository
{
    IQueryable<InventoryStatusDto> GetInventoryStatuses();
    IQueryable<InventoryReportRowDto> GetInventoryReportRows();
    IQueryable<DashboardChartPointDto> GetDailyMovements(DateOnly from, DateOnly to);

    /// <summary>
    /// Truy vấn lịch sử giao dịch kho, lọc theo khoảng ngày nếu có.
    /// Kết quả sắp xếp giảm dần theo thời gian xảy ra.
    /// </summary>
    IQueryable<TransactionHistoryDto> GetTransactionHistory(DateOnly? from, DateOnly? to);
}

/// <summary>
/// Điều phối transaction ở tầng BLL. Interface giúp service không phụ thuộc trực tiếp DbContext
/// và Moq có thể làm giả UoW trong unit test.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IInventoryRepository Inventories { get; }
    IInventoryTransactionRepository InventoryTransactions { get; }
    IReceiptRepository Receipts { get; }
    IReportRepository Reports { get; }

    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task CommitAsync(CancellationToken cancellationToken = default);
    Task RollbackAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Cài đặt tổng quát dùng cho kiểm thử hoặc khi DbContext chưa được đưa vào hệ thống.
/// Không để BLL gọi trực tiếp DbContext.
/// </summary>
public sealed class InMemoryUnitOfWork : IUnitOfWork
{
    public IInventoryRepository Inventories { get; }
    public IInventoryTransactionRepository InventoryTransactions { get; }
    public IReceiptRepository Receipts { get; }
    public IReportRepository Reports { get; }

    public bool HasBegun { get; private set; }
    public bool Committed { get; private set; }
    public bool RolledBack { get; private set; }

    public InMemoryUnitOfWork(
        IInventoryRepository? inventories = null,
        IInventoryTransactionRepository? inventoryTransactions = null,
        IReceiptRepository? receipts = null,
        IReportRepository? reports = null)
    {
        Inventories = inventories ?? new EmptyInventoryRepository();
        InventoryTransactions = inventoryTransactions ?? new EmptyInventoryTransactionRepository();
        Receipts = receipts ?? new EmptyReceiptRepository();
        Reports = reports ?? new EmptyReportRepository();
    }

    public Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        HasBegun = true;
        return Task.CompletedTask;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        Committed = true;
        HasBegun = false;
        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        RolledBack = true;
        HasBegun = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private sealed class EmptyInventoryRepository : IInventoryRepository
    {
        public Task<Inventory?> FindAsync(object id, CancellationToken cancellationToken = default) => Task.FromResult<Inventory?>(null);
        public IQueryable<Inventory> AsNoTracking() => Array.Empty<Inventory>().AsQueryable();
        public IQueryable<Inventory> AsTracking() => Array.Empty<Inventory>().AsQueryable();
        public Task<Inventory?> FindByVariantAndLocationAsync(int variantId, int locationId, bool asNoTracking, CancellationToken cancellationToken = default) => Task.FromResult<Inventory?>(null);
    }

    private sealed class EmptyInventoryTransactionRepository : IInventoryTransactionRepository
    {
        public IQueryable<InventoryTransaction> AsNoTracking() => Array.Empty<InventoryTransaction>().AsQueryable();
        public void Add(InventoryTransaction transaction) { }
    }

    private sealed class EmptyReceiptRepository : IReceiptRepository
    {
        public IQueryable<Receipt> AsNoTracking() => Array.Empty<Receipt>().AsQueryable();
        public void Add(Receipt receipt) { }
        public void AddDetails(IEnumerable<ReceiptDetail> details) { }
    }

    private sealed class EmptyReportRepository : IReportRepository
    {
        public IQueryable<InventoryStatusDto> GetInventoryStatuses() => Array.Empty<InventoryStatusDto>().AsQueryable();
        public IQueryable<InventoryReportRowDto> GetInventoryReportRows() => Array.Empty<InventoryReportRowDto>().AsQueryable();
        public IQueryable<DashboardChartPointDto> GetDailyMovements(DateOnly from, DateOnly to) => Array.Empty<DashboardChartPointDto>().AsQueryable();
        public IQueryable<TransactionHistoryDto> GetTransactionHistory(DateOnly? from, DateOnly? to) => Array.Empty<TransactionHistoryDto>().AsQueryable();
    }
}

