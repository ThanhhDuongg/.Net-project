using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Models.Entities;
using NetProject.Services;

namespace NetProject.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<UsersController> _logger;

    public UsersController(WarehouseDbContext context, ILogger<UsersController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<List<UserListItemDto>>> GetAll(CancellationToken ct)
    {
        var users = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.Receipts)
            .OrderBy(u => u.UserId)
            .Select(u => new UserListItemDto
            {
                UserId = u.UserId,
                Username = u.Username,
                FullName = u.FullName,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                IsActive = u.IsActive,
                RoleId = u.RoleId,
                RoleName = u.Role != null ? u.Role.RoleName : "Chưa phân quyền",
                CreatedAt = u.CreatedAt,
                TotalReceiptsCreated = u.Receipts.Count
            })
            .ToListAsync(ct);

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserListItemDto>> GetById(int id, CancellationToken ct)
    {
        var u = await _context.Users
            .Include(x => x.Role)
            .Include(x => x.Receipts)
            .FirstOrDefaultAsync(x => x.UserId == id, ct);

        if (u == null)
            return NotFound(new { message = $"Không tìm thấy người dùng có ID {id}." });

        return Ok(new UserListItemDto
        {
            UserId = u.UserId,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            IsActive = u.IsActive,
            RoleId = u.RoleId,
            RoleName = u.Role != null ? u.Role.RoleName : "Chưa phân quyền",
            CreatedAt = u.CreatedAt,
            TotalReceiptsCreated = u.Receipts.Count
        });
    }

    [HttpPost]
    public async Task<ActionResult<UserListItemDto>> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        if (dto == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        var username = dto.Username?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username) || username.Length < 3)
            return BadRequest(new { message = "Tên đăng nhập phải có ít nhất 3 ký tự." });

        if (username.Contains(' '))
            return BadRequest(new { message = "Tên đăng nhập không được chứa khoảng trắng." });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Họ và tên không được để trống." });

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            return BadRequest(new { message = "Mật khẩu khởi tạo phải có ít nhất 6 ký tự." });

        var exists = await _context.Users.AnyAsync(u => u.Username == username, ct);
        if (exists)
            return Conflict(new { message = $"Tên đăng nhập '{username}' đã tồn tại trong hệ thống." });

        var role = await _context.Roles.FindAsync(new object[] { dto.RoleId }, ct);
        if (role == null)
            return BadRequest(new { message = "Vai trò (Role) không hợp lệ." });

        var user = new User
        {
            Username = username,
            PasswordHash = SecurityHelper.HashPassword(dto.Password),
            FullName = dto.FullName.Trim(),
            Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim(),
            RoleId = dto.RoleId,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã tạo mới tài khoản '{Username}' với quyền '{RoleName}'", user.Username, role.RoleName);

        return CreatedAtAction(nameof(GetById), new { id = user.UserId }, new UserListItemDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            IsActive = user.IsActive,
            RoleId = user.RoleId,
            RoleName = role.RoleName,
            CreatedAt = user.CreatedAt,
            TotalReceiptsCreated = 0
        });
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        if (dto == null)
            return BadRequest(new { message = "Dữ liệu không hợp lệ." });

        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id, ct);
        if (user == null)
            return NotFound(new { message = $"Không tìm thấy người dùng có ID {id}." });

        if (string.IsNullOrWhiteSpace(dto.FullName))
            return BadRequest(new { message = "Họ và tên không được để trống." });

        var role = await _context.Roles.FindAsync(new object[] { dto.RoleId }, ct);
        if (role == null)
            return BadRequest(new { message = "Vai trò (Role) không hợp lệ." });

        // Bảo vệ tài khoản Admin duy nhất: nếu hạ quyền hoặc khóa tài khoản của Admin
        if (user.Role?.RoleName == "Admin" && (role.RoleName != "Admin" || !dto.IsActive))
        {
            var activeAdminCount = await _context.Users
                .Include(u => u.Role)
                .CountAsync(u => u.IsActive && u.Role.RoleName == "Admin", ct);

            if (activeAdminCount <= 1)
            {
                return BadRequest(new { message = "Không thể khóa hoặc hạ quyền Quản trị viên (Admin) duy nhất đang hoạt động trong hệ thống." });
            }
        }

        user.FullName = dto.FullName.Trim();
        user.Email = string.IsNullOrWhiteSpace(dto.Email) ? null : dto.Email.Trim();
        user.PhoneNumber = string.IsNullOrWhiteSpace(dto.PhoneNumber) ? null : dto.PhoneNumber.Trim();
        user.RoleId = dto.RoleId;
        user.IsActive = dto.IsActive;

        await _context.SaveChangesAsync(ct);
        _logger.LogInformation("Đã cập nhật thông tin tài khoản '{Username}'", user.Username);

        return Ok(new { message = "Cập nhật thông tin nhân viên thành công." });
    }

    [HttpPatch("{id:int}/toggle-status")]
    public async Task<ActionResult> ToggleStatus(int id, CancellationToken ct)
    {
        var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserId == id, ct);
        if (user == null)
            return NotFound(new { message = $"Không tìm thấy người dùng có ID {id}." });

        // Nếu chuẩn bị khóa tài khoản Admin
        if (user.IsActive && user.Role?.RoleName == "Admin")
        {
            var activeAdminCount = await _context.Users
                .Include(u => u.Role)
                .CountAsync(u => u.IsActive && u.Role.RoleName == "Admin", ct);

            if (activeAdminCount <= 1)
            {
                return BadRequest(new { message = "Không thể khóa Quản trị viên (Admin) duy nhất đang hoạt động trong hệ thống." });
            }
        }

        user.IsActive = !user.IsActive;
        await _context.SaveChangesAsync(ct);

        var statusText = user.IsActive ? "kích hoạt" : "khóa";
        _logger.LogInformation("Tài khoản '{Username}' đã được {StatusText}.", user.Username, statusText);

        return Ok(new
        {
            message = $"Tài khoản '{user.Username}' đã được {statusText} thành công.",
            isActive = user.IsActive
        });
    }

    [HttpPost("{id:int}/reset-password")]
    public async Task<ActionResult> ResetPassword(int id, [FromBody] ResetPasswordDto dto, CancellationToken ct)
    {
        if (dto == null || string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            return BadRequest(new { message = "Mật khẩu mới phải có ít nhất 6 ký tự." });

        var user = await _context.Users.FindAsync(new object[] { id }, ct);
        if (user == null)
            return NotFound(new { message = $"Không tìm thấy người dùng có ID {id}." });

        user.PasswordHash = SecurityHelper.HashPassword(dto.NewPassword);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Đã đặt lại mật khẩu cho tài khoản '{Username}'", user.Username);

        return Ok(new { message = $"Đặt lại mật khẩu cho tài khoản '{user.Username}' thành công." });
    }
}

[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly WarehouseDbContext _context;

    public RolesController(WarehouseDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<List<RoleItemDto>>> GetAll(CancellationToken ct)
    {
        var roles = await _context.Roles
            .Include(r => r.Users)
            .OrderBy(r => r.RoleId)
            .Select(r => new RoleItemDto
            {
                RoleId = r.RoleId,
                RoleName = r.RoleName,
                Description = r.Description,
                UserCount = r.Users.Count
            })
            .ToListAsync(ct);

        return Ok(roles);
    }

    [HttpGet("permissions")]
    public ActionResult<List<RolePermissionMatrixDto>> GetPermissionMatrix()
    {
        var matrix = new List<RolePermissionMatrixDto>
        {
            new() { ModuleName = "Tổng quan (Dashboard)", FeatureName = "Xem tổng quan tồn kho & biểu đồ nhập xuất", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Theo dõi số lượng hàng hóa, cảnh báo hết hàng" },
            new() { ModuleName = "Tổng quan (Dashboard)", FeatureName = "Xem tổng giá trị vốn tồn kho", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Số liệu tài chính giá vốn hàng trong kho" },
            new() { ModuleName = "Quản lý Sản phẩm & SKU", FeatureName = "Xem danh mục sản phẩm, biến thể & mã Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Tra cứu thông tin hàng hóa, size, màu sắc" },
            new() { ModuleName = "Quản lý Sản phẩm & SKU", FeatureName = "Tạo mới sản phẩm & sinh ma trận biến thể", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Khai báo sản phẩm mới vào danh mục kho" },
            new() { ModuleName = "Quản lý Danh mục", FeatureName = "Thêm, sửa, xóa danh mục sản phẩm", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Cấu trúc cây danh mục hàng hóa (Áo sơ mi, Quần Jean,...)" },
            new() { ModuleName = "Nhập / Xuất Kho", FeatureName = "Tạo phiếu nhập kho (NCC) & quét Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Nhận hàng từ nhà cung cấp vào kho" },
            new() { ModuleName = "Nhập / Xuất Kho", FeatureName = "Tạo phiếu xuất kho & quét Barcode", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xuất hàng bán sỉ/lẻ hoặc chuyển kho nội bộ" },
            new() { ModuleName = "Tồn kho & Vị trí kệ", FeatureName = "Tra cứu tồn kho thực tế theo kệ hàng", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xem vị trí kệ lưu trữ (Khu A, B, C, D) và số lượng" },
            new() { ModuleName = "Tồn kho & Vị trí kệ", FeatureName = "Xuất bảng kiểm kê tồn kho ra Excel", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Hỗ trợ công tác kiểm kê định kỳ" },
            new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Xem sổ thẻ kho chi tiết theo ngày", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Theo dõi biến động xuất nhập tồn từng SKU" },
            new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Dự báo nhập hàng bổ sung tự động", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Gợi ý số lượng đặt hàng dựa trên ngưỡng an toàn" },
            new() { ModuleName = "Báo cáo & Thống kê", FeatureName = "Xuất báo cáo tổng hợp tài chính kho ra Excel", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Báo cáo doanh số và biến động giá trị tài sản kho" },
            new() { ModuleName = "Nhân viên & Phân quyền", FeatureName = "Xem danh sách nhân sự kho", AdminAllowed = true, WarehouseKeeperAllowed = true, Description = "Xem danh sách và thông tin liên hệ các thành viên" },
            new() { ModuleName = "Nhân viên & Phân quyền", FeatureName = "Tạo tài khoản, phân quyền & khóa tài khoản", AdminAllowed = true, WarehouseKeeperAllowed = false, Description = "Quản lý tài khoản, đặt lại mật khẩu và cấp quyền truy cập" },
        };

        return Ok(matrix);
    }
}
