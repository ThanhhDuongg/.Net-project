using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;

namespace NetProject.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<CustomersController> _logger;

    public CustomersController(WarehouseDbContext context, ILogger<CustomersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<CustomerDto>>> GetAll([FromQuery] string? search, [FromQuery] string? type, CancellationToken ct)
    {
        var query = _context.Customers.Where(c => c.IsActive);

        if (!string.IsNullOrWhiteSpace(type) && type != "Tất cả")
        {
            query = query.Where(c => c.CustomerType == type.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(c =>
                c.CustomerCode.ToLower().Contains(term) ||
                c.CustomerName.ToLower().Contains(term) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(term)) ||
                (c.Address != null && c.Address.ToLower().Contains(term)));
        }

        var customers = await query
            .OrderBy(c => c.CustomerId)
            .Select(c => new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                CustomerType = c.CustomerType,
                PhoneNumber = c.PhoneNumber,
                Email = c.Email,
                Address = c.Address,
                DebtAmount = c.DebtAmount,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(customers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerDto>> GetById(int id, CancellationToken ct)
    {
        var c = await _context.Customers.FirstOrDefaultAsync(x => x.CustomerId == id, ct);
        if (c == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}." });

        return Ok(new CustomerDto
        {
            CustomerId = c.CustomerId,
            CustomerCode = c.CustomerCode,
            CustomerName = c.CustomerName,
            CustomerType = c.CustomerType,
            PhoneNumber = c.PhoneNumber,
            Email = c.Email,
            Address = c.Address,
            DebtAmount = c.DebtAmount,
            IsActive = c.IsActive,
            CreatedAt = c.CreatedAt
        });
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create([FromBody] CreateCustomerDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerName))
            return BadRequest(new { message = "Tên khách hàng / đại lý không được để trống." });

        string code = dto.CustomerCode?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
        {
            int count = await _context.Customers.CountAsync(ct) + 1;
            code = $"KH{count:D3}";
        }

        var customer = new Customer
        {
            CustomerCode = code,
            CustomerName = dto.CustomerName.Trim(),
            CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "Đại lý cấp 1" : dto.CustomerType.Trim(),
            PhoneNumber = dto.PhoneNumber?.Trim(),
            Email = dto.Email?.Trim(),
            Address = dto.Address?.Trim(),
            DebtAmount = 0,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã thêm khách hàng mới '{CustomerName}' ({CustomerCode})", customer.CustomerName, customer.CustomerCode);

        return CreatedAtAction(nameof(GetById), new { id = customer.CustomerId }, new CustomerDto
        {
            CustomerId = customer.CustomerId,
            CustomerCode = customer.CustomerCode,
            CustomerName = customer.CustomerName,
            CustomerType = customer.CustomerType,
            PhoneNumber = customer.PhoneNumber,
            Email = customer.Email,
            Address = customer.Address,
            DebtAmount = customer.DebtAmount,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateCustomerDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.CustomerName))
            return BadRequest(new { message = "Tên khách hàng không được để trống." });

        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}." });

        customer.CustomerName = dto.CustomerName.Trim();
        customer.CustomerType = string.IsNullOrWhiteSpace(dto.CustomerType) ? "Đại lý cấp 1" : dto.CustomerType.Trim();
        customer.PhoneNumber = dto.PhoneNumber?.Trim();
        customer.Email = dto.Email?.Trim();
        customer.Address = dto.Address?.Trim();
        customer.DebtAmount = dto.DebtAmount;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã cập nhật khách hàng #{CustomerId} '{CustomerName}'", id, customer.CustomerName);

        return Ok(new { message = "Cập nhật thông tin khách hàng thành công." });
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken ct)
    {
        var customer = await _context.Customers.FindAsync(new object[] { id }, ct);
        if (customer == null)
            return NotFound(new { message = $"Không tìm thấy khách hàng #{id}." });

        customer.IsActive = false;
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã ngưng hoạt động khách hàng #{CustomerId} '{CustomerName}'", id, customer.CustomerName);
        return Ok(new { message = $"Đã ngưng hoạt động khách hàng '{customer.CustomerName}'." });
    }
}

