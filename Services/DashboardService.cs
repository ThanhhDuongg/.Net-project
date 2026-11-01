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
        var statuses = _unitOfWork.Reports.GetInventoryStatuses();

        var totalSkus = statuses.Select(x => x.VariantId).Distinct().Count();
        var totalQuantity = statuses.Sum(x => (int?)x.QuantityOnHand) ?? 0;
        var lowStockSkuCount = statuses
            .Where(x => x.QuantityOnHand <= x.SafetyStock)
            .Select(x => x.VariantId)
            .Distinct()
            .Count();

        var inventoryValue = _unitOfWork.Inventories.AsNoTracking()
            .Select(x => (decimal?)(x.QuantityOnHand * x.Variant.CostPrice))
            .Sum() ?? 0m;

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
            .Where(x => x.QuantityOnHand <= x.SafetyStock)
            .OrderBy(x => x.QuantityOnHand)
            .ThenBy(x => x.SKU)
            .Take(take)
            .ToList();

        return Task.FromResult((IReadOnlyList<InventoryStatusDto>)alerts);
    }
}
