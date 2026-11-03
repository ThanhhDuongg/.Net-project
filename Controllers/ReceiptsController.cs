using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Enums;

namespace NetProject.Controllers;

[ApiController]
[Route("api/receipts")]
public class ReceiptsController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public ReceiptsController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<ReceiptInvoiceDto>>> GetAll(
        [FromQuery] string? type,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        CancellationToken ct = default)
    {
        var query = _context.Receipts
            .Include(r => r.Supplier)
            .Include(r => r.User)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Product)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Color)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Size)
            .Include(r => r.Details)
                .ThenInclude(d => d.Location)
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<ReceiptType>(type, true, out var rType))
        {
            query = query.Where(r => r.ReceiptType == rType);
        }

        if (from.HasValue)
        {
            query = query.Where(r => r.ReceiptDate >= from.Value.Date);
        }

        if (to.HasValue)
        {
            var endExclusive = to.Value.Date.AddDays(1);
            query = query.Where(r => r.ReceiptDate < endExclusive);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r =>
                r.ReceiptCode.ToLower().Contains(term) ||
                (r.CustomerName != null && r.CustomerName.ToLower().Contains(term)) ||
                (r.Supplier != null && r.Supplier.SupplierName.ToLower().Contains(term)) ||
                (r.Notes != null && r.Notes.ToLower().Contains(term)));
        }

        var receipts = await query
            .OrderByDescending(r => r.ReceiptDate)
            .ThenByDescending(r => r.ReceiptId)
            .Take(200)
            .ToListAsync(ct);

        var result = receipts.Select(r => MapToInvoiceDto(r)).ToList();
        return Ok(result);
    }

    [HttpGet("{id:int}/invoice")]
    public async Task<ActionResult<ReceiptInvoiceDto>> GetInvoiceById(int id, CancellationToken ct)
    {
        var r = await _context.Receipts
            .Include(r => r.Supplier)
            .Include(r => r.User)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Product)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Color)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Size)
            .Include(r => r.Details)
                .ThenInclude(d => d.Location)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReceiptId == id, ct);

        if (r == null)
            return NotFound(new { message = $"Không tìm thấy chứng từ/hóa đơn #{id}." });

        return Ok(MapToInvoiceDto(r));
    }

    [HttpGet("code/{code}/invoice")]
    public async Task<ActionResult<ReceiptInvoiceDto>> GetInvoiceByCode(string code, CancellationToken ct)
    {
        var r = await _context.Receipts
            .Include(r => r.Supplier)
            .Include(r => r.User)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Product)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Color)
            .Include(r => r.Details)
                .ThenInclude(d => d.Variant)
                    .ThenInclude(v => v.Size)
            .Include(r => r.Details)
                .ThenInclude(d => d.Location)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ReceiptCode == code.Trim(), ct);

        if (r == null)
            return NotFound(new { message = $"Không tìm thấy chứng từ/hóa đơn mã '{code}'." });

        return Ok(MapToInvoiceDto(r));
    }

    private static ReceiptInvoiceDto MapToInvoiceDto(Models.Entities.Receipt r)
    {
        bool isImport = r.ReceiptType == ReceiptType.IMPORT;
        string partnerName = isImport
            ? (r.Supplier?.SupplierName ?? "Nhà Cung Cấp")
            : (r.CustomerName ?? "Khách Đại Lý / Bán Buôn");

        string? partnerPhone = isImport ? r.Supplier?.PhoneNumber : null;
        string? partnerAddress = isImport ? r.Supplier?.Address : null;

        int idx = 1;
        var items = r.Details.Select(d => new ReceiptInvoiceItemDto
        {
            Index = idx++,
            VariantId = d.VariantId,
            SKU = d.Variant?.SKU ?? $"VAR-{d.VariantId}",
            Barcode = d.Variant?.Barcode ?? "",
            ProductName = d.Variant?.Product?.ProductName ?? "Sản phẩm",
            ColorName = d.Variant?.Color?.ColorName ?? "",
            SizeName = d.Variant?.Size?.SizeName ?? "",
            LocationCode = d.Location?.LocationCode ?? "KHO",
            Quantity = d.Quantity,
            UnitPrice = d.UnitPrice
        }).ToList();

        return new ReceiptInvoiceDto
        {
            ReceiptId = r.ReceiptId,
            ReceiptCode = r.ReceiptCode,
            ReceiptType = r.ReceiptType.ToString(),
            ReceiptDate = r.ReceiptDate,
            SupplierId = r.SupplierId,
            PartnerName = partnerName,
            PartnerPhone = partnerPhone,
            PartnerAddress = partnerAddress,
            UserId = r.UserId,
            CreatorFullName = r.User?.FullName ?? "Thủ kho",
            TotalAmount = r.TotalAmount,
            Notes = r.Notes,
            TotalQuantity = items.Sum(i => i.Quantity),
            Items = items
        };
    }
}

