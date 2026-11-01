using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetProject.Data;
using NetProject.DTOs;
using NetProject.Services;

namespace NetProject.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly WarehouseDbContext _context;
    private readonly ILogger<AuthController> _logger;

    public AuthController(WarehouseDbContext context, ILogger<AuthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginRequestDto request, CancellationToken cancellationToken)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new { message = "Vui lòng nhập tên đăng nhập và mật khẩu." });
        }

        var username = request.Username.Trim();
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == username, cancellationToken);

        if (user == null)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' không tồn tại.", username);
            return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản '{Username}' đã bị khóa.", username);
            return StatusCode(StatusCodes.Status403Forbidden, new { message = "Tài khoản của bạn đã bị khóa hoặc ngừng hoạt động." });
        }

        _logger.LogInformation("Kiểm tra đăng nhập cho '{Username}', storedHash: '{StoredHash}'", user.Username, user.PasswordHash);
        bool isPasswordValid = SecurityHelper.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Đăng nhập thất bại: Sai mật khẩu cho tài khoản '{Username}'.", username);
            return Unauthorized(new { message = "Tài khoản hoặc mật khẩu không chính xác." });
        }

        _logger.LogInformation("Người dùng '{Username}' ({FullName}) đăng nhập thành công.", user.Username, user.FullName);

        return Ok(new LoginResponseDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FullName = user.FullName,
            RoleName = user.Role?.RoleName ?? "Nhân viên"
        });
    }
}
