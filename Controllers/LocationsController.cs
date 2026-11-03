using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;
using NetProject.Models.Enums;

namespace NetProject.Controllers;

[ApiController]
[Route("api/locations")]
public class LocationsController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<LocationsController> _logger;

    public LocationsController(WarehouseDbContext context, ILogger<LocationsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<WarehouseLocationDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var query = _context.WarehouseLocations
            .Include(l => l.Inventories)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(l =>
                l.LocationCode.ToLower().Contains(term) ||
                l.LocationName.ToLower().Contains(term) ||
                (l.Description != null && l.Description.ToLower().Contains(term)));
        }

        var locations = await query
            .OrderBy(l => l.LocationCode)
            .Select(l => new WarehouseLocationDto
            {
                LocationId = l.LocationId,
                LocationCode = l.LocationCode,
                LocationName = l.LocationName,
                Description = l.Description,
                SkuCount = l.Inventories.Count(i => i.QuantityOnHand > 0),
                TotalQuantity = l.Inventories.Sum(i => i.QuantityOnHand)
            })
            .ToListAsync(ct);

        return Ok(locations);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WarehouseLocationDto>> GetById(int id, CancellationToken ct)
    {
        var l = await _context.WarehouseLocations
            .Include(x => x.Inventories)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.LocationId == id, ct);

        if (l == null)
            return NotFound(new { message = $"Không tìm thấy vị trí kho với ID = {id}" });

        return Ok(new WarehouseLocationDto
        {
            LocationId = l.LocationId,
            LocationCode = l.LocationCode,
            LocationName = l.LocationName,
            Description = l.Description,
            SkuCount = l.Inventories.Count(i => i.QuantityOnHand > 0),
            TotalQuantity = l.Inventories.Sum(i => i.QuantityOnHand)
        });
    }

    [HttpGet("{id:int}/inventory")]
    public async Task<ActionResult<List<LocationInventoryItemDto>>> GetLocationInventory(int id, CancellationToken ct)
    {
        var items = await _context.Inventories
            .Include(i => i.Variant)
                .ThenInclude(v => v.Product)
            .Include(i => i.Variant)
                .ThenInclude(v => v.Color)
            .Include(i => i.Variant)
                .ThenInclude(v => v.Size)
            .Where(i => i.LocationId == id && i.QuantityOnHand > 0)
            .OrderBy(i => i.Variant.SKU)
            .Select(i => new LocationInventoryItemDto
            {
                VariantId = i.VariantId,
                SKU = i.Variant.SKU,
                Barcode = i.Variant.Barcode,
                ProductName = i.Variant.Product.ProductName,
                ColorName = i.Variant.Color.ColorName,
                SizeName = i.Variant.Size.SizeName,
                QuantityOnHand = i.QuantityOnHand,
                SafetyStock = i.SafetyStock,
                CostPrice = i.Variant.CostPrice
            })
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult<WarehouseLocationDto>> Create([FromBody] CreateWarehouseLocationDto dto, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(dto.LocationName))
            return BadRequest(new { message = "Tên kho / vị trí kệ không được để trống." });

        string code = dto.LocationCode?.Trim().ToUpper() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            var count = await _context.WarehouseLocations.CountAsync(ct);
            code = $"KHO-{(char)('A' + (count % 26))}-{count + 1:D2}";
        }

        var exists = await _context.WarehouseLocations.AnyAsync(l => l.LocationCode == code, ct);
        if (exists)
            return Conflict(new { message = $"Mã vị trí kho '{code}' đã tồn tại trong hệ thống." });

        var location = new WarehouseLocation
        {
            LocationCode = code,
            LocationName = dto.LocationName.Trim(),
            Description = dto.Description?.Trim()
        };

        _context.WarehouseLocations.Add(location);
        await _context.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = location.LocationId }, new WarehouseLocationDto
        {
            LocationId = location.LocationId,
            LocationCode = location.LocationCode,
            LocationName = location.LocationName,
            Description = location.Description,
            SkuCount = 0,
            TotalQuantity = 0
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<WarehouseLocationDto>> Update(int id, [FromBody] UpdateWarehouseLocationDto dto, CancellationToken ct)
    {
        var location = await _context.WarehouseLocations
            .Include(l => l.Inventories)
            .FirstOrDefaultAsync(l => l.LocationId == id, ct);

        if (location == null)
            return NotFound(new { message = $"Không tìm thấy vị trí kho với ID = {id}" });

        if (string.IsNullOrWhiteSpace(dto.LocationName))
            return BadRequest(new { message = "Tên vị trí kho không được để trống." });

        location.LocationName = dto.LocationName.Trim();
        location.Description = dto.Description?.Trim();

        await _context.SaveChangesAsync(ct);

        return Ok(new WarehouseLocationDto
        {
            LocationId = location.LocationId,
            LocationCode = location.LocationCode,
            LocationName = location.LocationName,
            Description = location.Description,
            SkuCount = location.Inventories.Count(i => i.QuantityOnHand > 0),
            TotalQuantity = location.Inventories.Sum(i => i.QuantityOnHand)
        });
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var location = await _context.WarehouseLocations
            .Include(l => l.Inventories)
            .FirstOrDefaultAsync(l => l.LocationId == id, ct);

        if (location == null)
            return NotFound(new { message = $"Không tìm thấy vị trí kho với ID = {id}" });

        var totalQty = location.Inventories.Sum(i => i.QuantityOnHand);
        if (totalQty > 0)
            return BadRequest(new { message = $"Không thể xóa vị trí '{location.LocationCode}' vì đang tồn trữ {totalQty} sản phẩm. Hãy điều chuyển hàng sang vị trí khác trước khi xóa." });

        // Remove 0-qty inventory records
        _context.Inventories.RemoveRange(location.Inventories);
        _context.WarehouseLocations.Remove(location);
        await _context.SaveChangesAsync(ct);

        return Ok(new { message = $"Đã xóa vị trí kho '{location.LocationCode}' thành công." });
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferStockDto dto, CancellationToken ct)
    {
        if (dto.Quantity <= 0)
            return BadRequest(new { message = "Số lượng điều chuyển phải lớn hơn 0." });

        if (dto.FromLocationId == dto.ToLocationId)
            return BadRequest(new { message = "Vị trí kho nguồn và đích không được trùng nhau." });

        var fromLoc = await _context.WarehouseLocations.FindAsync(new object[] { dto.FromLocationId }, ct);
        var toLoc = await _context.WarehouseLocations.FindAsync(new object[] { dto.ToLocationId }, ct);

        if (fromLoc == null || toLoc == null)
            return BadRequest(new { message = "Vị trí kho nguồn hoặc đích không tồn tại." });

        var fromInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == dto.VariantId && i.LocationId == dto.FromLocationId, ct);

        if (fromInv == null || fromInv.QuantityOnHand < dto.Quantity)
        {
            var avail = fromInv?.QuantityOnHand ?? 0;
            return BadRequest(new { message = $"Vị trí '{fromLoc.LocationCode}' không đủ hàng tồn để chuyển. Tồn hiện tại: {avail}, cần chuyển: {dto.Quantity}." });
        }

        var toInv = await _context.Inventories
            .FirstOrDefaultAsync(i => i.VariantId == dto.VariantId && i.LocationId == dto.ToLocationId, ct);

        if (toInv == null)
        {
            toInv = new Inventory
            {
                VariantId = dto.VariantId,
                LocationId = dto.ToLocationId,
                QuantityOnHand = 0,
                SafetyStock = fromInv.SafetyStock,
                RowVersion = DateTime.UtcNow
            };
            _context.Inventories.Add(toInv);
            await _context.SaveChangesAsync(ct);
        }

        int fromBefore = fromInv.QuantityOnHand;
        fromInv.QuantityOnHand -= dto.Quantity;
        int fromAfter = fromInv.QuantityOnHand;

        int toBefore = toInv.QuantityOnHand;
        toInv.QuantityOnHand += dto.Quantity;
        int toAfter = toInv.QuantityOnHand;

        string transferRef = $"DC-{DateTime.UtcNow:yyyyMMddHHmmss}";

        var txnOut = new InventoryTransaction
        {
            InventoryId = fromInv.InventoryId,
            VariantId = dto.VariantId,
            LocationId = dto.FromLocationId,
            TransactionType = InventoryTransactionType.TRANSFER,
            QuantityChange = -dto.Quantity,
            QuantityBefore = fromBefore,
            QuantityAfter = fromAfter,
            ReferenceNo = transferRef,
            Notes = $"Điều chuyển sang [{toLoc.LocationCode}]: {dto.Notes}".Trim(),
            OccurredAt = DateTime.UtcNow,
            PerformedByUserId = dto.UserId > 0 ? dto.UserId : 1
        };

        var txnIn = new InventoryTransaction
        {
            InventoryId = toInv.InventoryId,
            VariantId = dto.VariantId,
            LocationId = dto.ToLocationId,
            TransactionType = InventoryTransactionType.TRANSFER,
            QuantityChange = dto.Quantity,
            QuantityBefore = toBefore,
            QuantityAfter = toAfter,
            ReferenceNo = transferRef,
            Notes = $"Nhận điều chuyển từ [{fromLoc.LocationCode}]: {dto.Notes}".Trim(),
            OccurredAt = DateTime.UtcNow,
            PerformedByUserId = dto.UserId > 0 ? dto.UserId : 1
        };

        _context.InventoryTransactions.Add(txnOut);
        _context.InventoryTransactions.Add(txnIn);

        await _context.SaveChangesAsync(ct);

        return Ok(new
        {
            message = $"Điều chuyển thành công {dto.Quantity} sản phẩm từ [{fromLoc.LocationCode}] sang [{toLoc.LocationCode}]. Mã giao dịch: {transferRef}",
            referenceNo = transferRef
        });
    }
}

