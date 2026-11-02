using NetProject.DAL;
using NetProject.DTOs;

namespace NetProject.Services;

/// <summary>
/// Dashboard dùng dữ liệu tổng hợp ở SQL level. Không duyệt entity đầy đủ trong bộ nhớ.
/// </summary>
public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from = null, DateOnly? to = null);
    Task<IReadOnlyList<InventoryStatusDto>> GetLowStockAlertsAsync(int take = 100);
    Task<IReadOnlyList<InventoryStatusDto>> GetInventoryStatusesAsync(
        string? search = null,
        string? locationCode = null,
        bool lowStockOnly = false,
        int take = 5000);
}

public sealed class DashboardService : IDashboardService
{
    private readonly IUnitOfWork _unitOfWork;

    public DashboardService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task<DashboardSummaryDto> GetSummaryAsync(DateOnly? from = null, DateOnly? to = null)
    {
        var start = from ?? DateOnly.FromDateTime(DateTime.Today.AddDays(-13));
        var end = to ?? DateOnly.FromDateTime(DateTime.Today);
        var statuses = _unitOfWork.Reports.GetInventoryStatuses().ToList();

        var totalSkus = statuses.Select(x => x.VariantId).Distinct().Count();
        var totalQuantity = statuses.Sum(x => x.QuantityOnHand);
        var lowStockSkuCount = statuses
            .Where(x => x.QuantityOnHand <= x.SafetyStock)
            .Select(x => x.VariantId)
            .Distinct()
            .Count();

        var inventoryValue = statuses.Sum(x => (decimal)x.QuantityOnHand * x.CostPrice);

        var movements = _unitOfWork.Reports.GetDailyMovements(start, end).ToList();
        return Task.FromResult(new DashboardSummaryDto
        {
            TotalSkus = totalSkus,
            TotalQuantity = totalQuantity,
            LowStockSkuCount = lowStockSkuCount,
            InventoryValue = inventoryValue,
            DailyMovements = movements
        });
    }

    public Task<IReadOnlyList<InventoryStatusDto>> GetLowStockAlertsAsync(int take = 100)
    {
        var alerts = _unitOfWork.Reports.GetInventoryStatuses()
            .ToList()
            .Where(x => x.QuantityOnHand <= x.SafetyStock)
            .OrderBy(x => x.QuantityOnHand)
            .ThenBy(x => x.SKU)
            .Take(take)
            .ToList();

        return Task.FromResult((IReadOnlyList<InventoryStatusDto>)alerts);
    }

    public Task<IReadOnlyList<InventoryStatusDto>> GetInventoryStatusesAsync(
        string? search = null,
        string? locationCode = null,
        bool lowStockOnly = false,
        int take = 5000)
    {
        if (take is < 1 or > 5000)
            return Task.FromException<IReadOnlyList<InventoryStatusDto>>(
                new ArgumentOutOfRangeException(nameof(take), "Số dòng phải từ 1 đến 5000."));

        var query = _unitOfWork.Reports.GetInventoryStatuses();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(item => item.SKU.Contains(term) ||
                item.Barcode.Contains(term) || item.ProductName.Contains(term) || item.ColorName.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(locationCode))
        {
            var location = locationCode.Trim();
            query = query.Where(item => item.LocationCode == location);
        }

        if (lowStockOnly)
            query = query.Where(item => item.QuantityOnHand <= item.SafetyStock);

        var results = query.OrderBy(item => item.LocationCode)
            .ThenBy(item => item.SKU)
            .Take(take)
            .ToList();
        return Task.FromResult((IReadOnlyList<InventoryStatusDto>)results);
    }
}
