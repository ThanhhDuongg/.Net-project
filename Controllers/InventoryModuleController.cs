using Microsoft.EntityFrameworkCore;
using NetProject.DAL;
using Microsoft.AspNetCore.Mvc;
using NetProject.DTOs;
using NetProject.Services;
using NetProject.Services.Reporting;
using NetProject.Services.Exceptions;

namespace NetProject.Controllers;

[ApiController]
[Route("api/inventory")]
public sealed class InventoryTransactionsController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IDashboardService _dashboardService;

    public InventoryTransactionsController(IInventoryService inventoryService, IDashboardService dashboardService)
    {
        _inventoryService = inventoryService;
        _dashboardService = dashboardService;
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<InventoryTransactionResultDto>> Process(
        InventoryTransactionRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await _inventoryService.ProcessAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (InventoryConcurrencyException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Dữ liệu tồn kho vừa thay đổi",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InsufficientInventoryException exception)
        {
            return Conflict(new ProblemDetails
            {
                Title = "Không đủ tồn kho để xuất",
                Detail = exception.Message,
                Status = StatusCodes.Status409Conflict
            });
        }
        catch (InventoryNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Title = "Không tìm thấy tồn kho",
                Detail = exception.Message,
                Status = StatusCodes.Status404NotFound
            });
        }
    }

    [HttpGet("statuses")]
    public async Task<ActionResult<IReadOnlyList<InventoryStatusDto>>> GetStatuses(
        [FromQuery] string? search,
        [FromQuery] string? locationCode,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int take = 5000)
    {
        if (take is < 1 or > 5000)
            return BadRequest(new ProblemDetails
            {
                Title = "Giới hạn truy vấn không hợp lệ",
                Detail = "Tham số take phải từ 1 đến 5000.",
                Status = StatusCodes.Status400BadRequest
            });

        var statuses = await _dashboardService.GetInventoryStatusesAsync(search, locationCode, lowStockOnly, take);
        return Ok(statuses);
    }

    [HttpGet("barcode/{barcode}")]
    public async Task<ActionResult<InventoryStatusDto>> FindBarcode(
        string barcode,
        CancellationToken cancellationToken)
    {
        var item = await _inventoryService.FindByBarcodeAsync(barcode, cancellationToken);
        return item is null ? NotFound() : Ok(item);
    }
}

[ApiController]
[Route("api/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> Summary(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        CancellationToken cancellationToken)
    {
        if (from.HasValue && to.HasValue && from > to)
            return BadRequest(new ProblemDetails
            {
                Title = "Khoảng ngày không hợp lệ",
                Detail = "Ngày bắt đầu phải trước hoặc bằng ngày kết thúc.",
                Status = StatusCodes.Status400BadRequest
            });

        return Ok(await _dashboardService.GetSummaryAsync(from, to));
    }

    [HttpGet("low-stock")]
    public Task<IReadOnlyList<InventoryStatusDto>> LowStock([FromQuery] int take = 100) =>
        _dashboardService.GetLowStockAlertsAsync(take);
}

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IExcelReportService _excelReportService;

    public ReportsController(IExcelReportService excelReportService)
    {
        _excelReportService = excelReportService;
    }

    [HttpGet("inventory.xlsx")]
    public async Task<IActionResult> ExportInventory(CancellationToken cancellationToken)
    {
        await using var stream = new MemoryStream();
        await _excelReportService.ExportInventoryReportAsync(stream, cancellationToken);
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"BaoCaoTonKho_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
    }
}

