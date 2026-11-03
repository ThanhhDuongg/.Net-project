using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;

namespace NetProject.Controllers;

[ApiController]
[Route("api/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<SuppliersController> _logger;

    public SuppliersController(WarehouseDbContext context, ILogger<SuppliersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<SupplierDto>>> GetAll([FromQuery] string? search, CancellationToken ct)
    {
        var query = _context.Suppliers
            .Include(s => s.Receipts)
            .Where(s => s.IsActive);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(s =>
                s.SupplierName.ToLower().Contains(term) ||
                (s.ContactName != null && s.ContactName.ToLower().Contains(term)) ||
                (s.PhoneNumber != null && s.PhoneNumber.Contains(term)) ||
                (s.Address != null && s.Address.ToLower().Contains(term)));
        }

        var suppliers = await query
            .OrderBy(s => s.SupplierId)
            .Select(s => new SupplierDto
            {
                SupplierId = s.SupplierId,
                SupplierName = s.SupplierName,
                ContactName = s.ContactName,
                PhoneNumber = s.PhoneNumber,
                Email = s.Email,
                Address = s.Address,
                IsActive = s.IsActive,
                TotalReceipts = s.Receipts.Count
            })
            .ToListAsync(ct);

        return Ok(suppliers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<SupplierDto>> GetById(int id, CancellationToken ct)
    {
        var s = await _context.Suppliers
            .Include(x => x.Receipts)
            .FirstOrDefaultAsync(x => x.SupplierId == id, ct);

        if (s == null)
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp #{id}." });

        return Ok(new SupplierDto
        {
            SupplierId = s.SupplierId,
            SupplierName = s.SupplierName,
            ContactName = s.ContactName,
            PhoneNumber = s.PhoneNumber,
            Email = s.Email,
            Address = s.Address,
            IsActive = s.IsActive,
            TotalReceipts = s.Receipts.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<SupplierDto>> Create([FromBody] CreateOrUpdateSupplierDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SupplierName))
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });

        var supplier = new Supplier
        {
            SupplierName = dto.SupplierName.Trim(),
            ContactName = dto.ContactName?.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            IsActive = true
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã thêm nhà cung cấp mới '{SupplierName}' (ID: {SupplierId})", supplier.SupplierName, supplier.SupplierId);

        return CreatedAtAction(nameof(GetById), new { id = supplier.SupplierId }, new SupplierDto
        {
            SupplierId = supplier.SupplierId,
            SupplierName = supplier.SupplierName,
            ContactName = supplier.ContactName,
            PhoneNumber = supplier.PhoneNumber,
            Email = supplier.Email,
            Address = supplier.Address,
            IsActive = supplier.IsActive,
            TotalReceipts = 0
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] CreateOrUpdateSupplierDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.SupplierName))
            return BadRequest(new { message = "Tên nhà cung cấp không được để trống." });

        var supplier = await _context.Suppliers.FindAsync(new object[] { id }, ct);
        if (supplier == null)
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp #{id}." });

        supplier.SupplierName = dto.SupplierName.Trim();
        supplier.ContactName = dto.ContactName?.Trim();
        supplier.PhoneNumber = dto.PhoneNumber?.Trim();
        supplier.Email = dto.Email?.Trim();
        supplier.Address = dto.Address?.Trim();

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã cập nhật thông tin nhà cung cấp #{SupplierId} '{SupplierName}'", id, supplier.SupplierName);

        return Ok(new { message = "Cập nhật thông tin nhà cung cấp thành công." });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var supplier = await _context.Suppliers
            .Include(s => s.Receipts)
            .FirstOrDefaultAsync(s => s.SupplierId == id, ct);

        if (supplier == null)
            return NotFound(new { message = $"Không tìm thấy nhà cung cấp #{id}." });

        // Xóa mềm
        supplier.IsActive = false;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã vô hiệu hóa nhà cung cấp #{SupplierId} '{SupplierName}'", id, supplier.SupplierName);
        return Ok(new { message = $"Đã ngưng hoạt động nhà cung cấp '{supplier.SupplierName}'." });
    }
}

